using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using EZLogger.Appenders;
using EZLogger.Utils;

namespace EZLogger
{
    /// <summary>
    /// EZ Logger 主管理器
    /// </summary>
    public sealed class EZLoggerManager : ILogger, IDisposable
    {
        #region 单例实现
        private static volatile EZLoggerManager _instance;
        private static readonly object _instanceLock = new object();

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static EZLoggerManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new EZLoggerManager();
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region 字段和属性
        private readonly List<ILogAppender> _appenders = new List<ILogAppender>();
        private readonly object _appendersLock = new object();
        private readonly ThreadSafeQueue<LogMessage> _logQueue;
        private readonly Thread _writeThread;
        private volatile bool _isRunning;
        private volatile bool _isDisposed;

        private LoggerConfiguration _configuration;
        private readonly Dictionary<LogLevel, ConditionalLogger> _conditionalLoggers = new Dictionary<LogLevel, ConditionalLogger>();

        // 系统日志监控
        private bool _systemLogMonitorEnabled;
        private bool _serverReportingEnabled;

        // 服务器上报相关
        private readonly Queue<string> _errorQueue = new Queue<string>();
        private readonly object _errorQueueLock = new object();
        private Thread _serverReportThread;
        private volatile bool _isServerReportRunning;
        private readonly Dictionary<string, object> _reportExtraData = new Dictionary<string, object>();
        private string _serverUrl = string.Empty;

        /// <summary>日志记录器名称</summary>
        public string Name => "EZLogger";

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>当前启用的日志级别</summary>
        public LogLevel EnabledLevels
        {
            get => _enabledLevels;
            set
            {
                if (_enabledLevels != value)
                {
                    _enabledLevels = value;
                    OnLevelsChanged?.Invoke(value);
                }
            }
        }
        private LogLevel _enabledLevels = LogLevel.All;

        /// <summary>日志级别变化事件</summary>
        public static event System.Action<LogLevel> OnLevelsChanged;

        /// <summary>当前配置</summary>
        public LoggerConfiguration Configuration
        {
            get => _configuration;
            set
            {
                _configuration = value ?? LoggerConfiguration.CreateDefault();
                EnabledLevels = _configuration.GlobalEnabledLevels;
            }
        }

        #region 便捷级别控制方法
        /// <summary>启用指定级别</summary>
        public void EnableLevel(LogLevel level) => EnabledLevels |= level;

        /// <summary>禁用指定级别</summary>
        public void DisableLevel(LogLevel level) => EnabledLevels &= ~level;

        /// <summary>切换指定级别的开关状态</summary>
        public void ToggleLevel(LogLevel level)
        {
            if (EnabledLevels.Contains(level))
                DisableLevel(level);
            else
                EnableLevel(level);
        }

        /// <summary>设置为仅错误级别</summary>
        public void SetErrorOnly() => EnabledLevels = LogLevel.ErrorAndAbove;

        /// <summary>设置为警告及以上级别</summary>
        public void SetWarningAndAbove() => EnabledLevels = LogLevel.WarningAndAbove;

        /// <summary>设置为错误及以上级别</summary>
        public void SetErrorAndAbove() => EnabledLevels = LogLevel.ErrorAndAbove;

        /// <summary>启用所有级别</summary>
        public void EnableAll() => EnabledLevels = LogLevel.All;

        /// <summary>禁用所有级别</summary>
        public void DisableAll() => EnabledLevels = LogLevel.None;
        #endregion

        /// <summary>是否处于性能模式</summary>
        public bool IsPerformanceMode => _configuration?.PerformanceMode ?? false;
        #endregion

        #region 构造函数和初始化
        private EZLoggerManager()
        {
            _configuration = LoggerConfiguration.CreateDefault();
            _logQueue = new ThreadSafeQueue<LogMessage>(_configuration.MaxQueueSize);

            // 启动写入线程
            if (_configuration.EnableAsyncWrite)
            {
                _isRunning = true;
                _writeThread = new Thread(WriteThreadProc)
                {
                    Name = "EZLogger-Writer",
                    IsBackground = true
                };
                _writeThread.Start();
            }

            // 添加默认的Unity输出器
            AddDefaultAppenders();

            // 初始化条件日志记录器
            InitializeConditionalLoggers();

            // 初始化系统日志监控
            InitializeSystemLogMonitor();

            // 初始化设备信息
            InitializeDeviceInfo();
        }

        private void AddDefaultAppenders()
        {
            // 添加Unity控制台输出器
            var unityAppender = new UnityAppender();
            unityAppender.Initialize(_configuration.UnityConsole);
            AddAppender(unityAppender);
        }

        private void InitializeConditionalLoggers()
        {
            // 为每个日志级别创建条件日志记录器
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                if (level != LogLevel.None && level != LogLevel.All && level != LogLevel.ErrorAndWarning)
                {
                    _conditionalLoggers[level] = new ConditionalLogger(level, this);
                }
            }
        }

        private void InitializeSystemLogMonitor()
        {
            // 注册系统日志监控事件
            SystemLogMonitor.Instance.OnSystemLogReceived += OnSystemLogReceived;
        }

        private void InitializeDeviceInfo()
        {
            try
            {
                // 收集设备信息，参考原始代码
                SetReportExtraData("platform", UnityEngine.Application.platform.ToString());
                SetReportExtraData("version", UnityEngine.Application.version);
                SetReportExtraData("bundleIdentifier", UnityEngine.Application.identifier);
                SetReportExtraData("productName", UnityEngine.Application.productName);
                SetReportExtraData("deviceModel", UnityEngine.SystemInfo.deviceModel);
                SetReportExtraData("operatingSystem", UnityEngine.SystemInfo.operatingSystem);
                SetReportExtraData("graphicsDeviceName", UnityEngine.SystemInfo.graphicsDeviceName);
                SetReportExtraData("systemMemorySize", UnityEngine.SystemInfo.systemMemorySize);
                SetReportExtraData("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        private void OnSystemLogReceived(string condition, string stackTrace, LogLevel logLevel)
        {
            // 构建系统错误消息
            var message = string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}";

            // 记录系统日志
            Log(logLevel, "System", message);

            // 如果启用服务器上报且是错误或异常，则上报
            if (_serverReportingEnabled && (logLevel == LogLevel.Error || logLevel == LogLevel.Exception))
            {
                ReportToServer(message, logLevel);
            }
        }

        /// <summary>
        /// 获取指定级别的条件日志记录器
        /// </summary>
        public ConditionalLogger GetConditionalLogger(LogLevel level)
        {
            return _conditionalLoggers.TryGetValue(level, out var logger) ? logger : null;
        }
        #endregion

        #region 输出器管理
        /// <summary>
        /// 添加日志输出器
        /// </summary>
        public void AddAppender(ILogAppender appender)
        {
            if (appender == null || _isDisposed)
                return;

            lock (_appendersLock)
            {
                _appenders.Add(appender);
            }
        }

        /// <summary>
        /// 移除日志输出器
        /// </summary>
        public bool RemoveAppender(ILogAppender appender)
        {
            if (appender == null || _isDisposed)
                return false;

            lock (_appendersLock)
            {
                return _appenders.Remove(appender);
            }
        }

        /// <summary>
        /// 移除指定名称的输出器
        /// </summary>
        public bool RemoveAppender(string name)
        {
            if (string.IsNullOrEmpty(name) || _isDisposed)
                return false;

            lock (_appendersLock)
            {
                for (int i = _appenders.Count - 1; i >= 0; i--)
                {
                    if (_appenders[i].Name == name)
                    {
                        _appenders.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 清空所有输出器
        /// </summary>
        public void ClearAppenders()
        {
            lock (_appendersLock)
            {
                foreach (var appender in _appenders)
                {
                    try
                    {
                        appender?.Dispose();
                    }
                    catch
                    {
                        // 忽略释放错误
                    }
                }
                _appenders.Clear();
            }
        }

        /// <summary>
        /// 获取所有输出器的副本
        /// </summary>
        public ILogAppender[] GetAppenders()
        {
            lock (_appendersLock)
            {
                return _appenders.ToArray();
            }
        }
        #endregion

        #region ILogger实现
        /// <summary>
        /// 检查指定级别是否启用
        /// </summary>
        public bool IsLevelEnabled(LogLevel level)
        {
            if (!IsEnabled || _isDisposed)
                return false;

            // 性能模式下，如果级别未启用则直接返回false
            if (IsPerformanceMode && !EnabledLevels.Contains(level))
                return false;

            return EnabledLevels.Contains(level);
        }

        /// <summary>
        /// 记录日志消息
        /// </summary>
        public void Log(LogMessage message)
        {
            if (!IsLevelEnabled(message.Level))
                return;

            // 分别处理同步和异步输出器
            WriteToSyncAppenders(message);

            if (_configuration.EnableAsyncWrite && _isRunning)
            {
                // 异步写入到支持异步的输出器
                if (!_logQueue.Enqueue(message))
                {
                    // 队列已满，强制入队（会丢弃最旧的消息）
                    _logQueue.ForceEnqueue(message);
                }
            }
            else
            {
                // 同步写入到支持异步的输出器
                WriteToAsyncAppenders(message);
            }
        }

        /// <summary>
        /// 记录日志（简化版本）
        /// </summary>
        public void Log(LogLevel level, string tag, string message)
        {
            if (!IsLevelEnabled(level))
                return;

            var logMessage = new LogMessage(level, tag, message, null, GetCurrentFrameCount());
            Log(logMessage);
        }

        #region 便捷日志方法
        public void LogLog(string tag, string message) => Log(LogLevel.Log, tag, message);
        public void LogWarning(string tag, string message) => Log(LogLevel.Warning, tag, message);
        public void LogAssert(string tag, string message) => Log(LogLevel.Assert, tag, message);

        /// <summary>
        /// 记录错误日志（带防重复机制）
        /// </summary>
        public void LogError(string tag, string message)
        {
            // 设置防重复标志，避免调用Unity Debug.LogError时重复记录
            SystemLogMonitor.Instance.SetPreventDuplicate(LogLevel.Error, true);
            try
            {
                Log(LogLevel.Error, tag, message);
            }
            finally
            {
                SystemLogMonitor.Instance.SetPreventDuplicate(LogLevel.Error, false);
            }
        }

        /// <summary>
        /// 记录异常日志（带防重复机制）
        /// </summary>
        public void LogException(string tag, string message)
        {
            // 设置防重复标志，避免调用Unity Debug.LogException时重复记录
            SystemLogMonitor.Instance.SetPreventDuplicate(LogLevel.Exception, true);
            try
            {
                Log(LogLevel.Exception, tag, message);
            }
            finally
            {
                SystemLogMonitor.Instance.SetPreventDuplicate(LogLevel.Exception, false);
            }
        }
        #endregion
        #endregion

        #region 写入线程处理
        private void WriteThreadProc()
        {
            while (_isRunning)
            {
                try
                {
                    // 批量处理日志消息
                    var messages = _logQueue.DequeueBatch(10);
                    if (messages.Count > 0)
                    {
                        foreach (var message in messages)
                        {
                            WriteToAppenders(message);
                        }
                    }
                    else
                    {
                        // 没有消息时短暂休眠
                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    // 记录写入线程错误，但不能使用自己的日志系统
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 写入到同步输出器（如Unity控制台）
        /// 这些输出器需要立即执行，以保证与Unity原生API的顺序一致
        /// </summary>
        private void WriteToSyncAppenders(LogMessage message)
        {
            ILogAppender[] appenders;
            lock (_appendersLock)
            {
                if (_appenders.Count == 0)
                    return;
                appenders = _appenders.Where(a => !a.SupportsAsyncWrite).ToArray();
            }

            foreach (var appender in appenders)
            {
                try
                {
                    if (appender.IsEnabled && appender.SupportedLevels.Contains(message.Level))
                    {
                        appender.WriteLog(message);
                    }
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 写入到异步输出器（如文件）
        /// 这些输出器可以在后台线程处理，避免IO阻塞
        /// </summary>
        private void WriteToAsyncAppenders(LogMessage message)
        {
            ILogAppender[] appenders;
            lock (_appendersLock)
            {
                if (_appenders.Count == 0)
                    return;
                appenders = _appenders.Where(a => a.SupportsAsyncWrite).ToArray();
            }

            foreach (var appender in appenders)
            {
                try
                {
                    if (appender.IsEnabled && appender.SupportedLevels.Contains(message.Level))
                    {
                        appender.WriteLog(message);
                    }
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 兼容旧版本的WriteToAppenders方法（用于后台线程）
        /// </summary>
        private void WriteToAppenders(LogMessage message)
        {
            // 在后台线程中，只处理异步输出器
            WriteToAsyncAppenders(message);
        }
        #endregion

        #region 工具方法
        private int GetCurrentFrameCount()
        {
            return UnityEngine.Time.frameCount;
        }

        private void HandleInternalError(Exception ex)
        {
            UnityEngine.Debug.LogError($"[EZLogger] Internal error: {ex.Message}");
        }
        #endregion

        #region 系统日志监控控制
        /// <summary>
        /// 启用系统日志监控
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void EnableSystemLogMonitor(bool enabled = true)
        {
            _systemLogMonitorEnabled = enabled;
            if (enabled)
            {
                SystemLogMonitor.Instance.StartMonitoring();
            }
            else
            {
                SystemLogMonitor.Instance.StopMonitoring();
            }
        }

        /// <summary>
        /// 启用错误日志服务器上报
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void EnableServerReporting(bool enabled = true)
        {
            _serverReportingEnabled = enabled;
        }

        /// <summary>
        /// 系统日志监控是否启用
        /// </summary>
        public bool IsSystemLogMonitorEnabled => _systemLogMonitorEnabled;

        /// <summary>
        /// 服务器上报是否启用
        /// </summary>
        public bool IsServerReportingEnabled => _serverReportingEnabled;

                /// <summary>
        /// 上报错误到服务器（参考原始PushError逻辑）
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="logLevel">日志级别</param>
        private void ReportToServer(string message, LogLevel logLevel)
        {
            // 如果没有配置服务器地址，跳过上报（不重复记录日志）
            if (string.IsNullOrEmpty(_serverUrl))
            {
                return;
            }
            
            // 启动服务器上报线程（如果还没启动）
            if (_serverReportThread == null && !string.IsNullOrEmpty(_serverUrl))
            {
                _isServerReportRunning = true;
                _serverReportThread = new Thread(ProcessServerReportQueue)
                {
                    Name = "EZLogger-ServerReport",
                    IsBackground = true
                };
                _serverReportThread.Start();
            }
            
            // 格式化错误消息，添加帧数和系统标识
            var formattedMessage = $"[FRAME:{GetCurrentFrameCount()}][SystemError]{message}";
            
            // 将消息加入队列等待上报
            lock (_errorQueueLock)
            {
                _errorQueue.Enqueue(formattedMessage);
            }
        }

        /// <summary>
        /// 处理服务器上报队列的后台线程方法（参考原始ProcessLogQueue）
        /// </summary>
        private void ProcessServerReportQueue()
        {
            while (_isServerReportRunning && !_isDisposed)
            {
                string errorMessage = null;

                lock (_errorQueueLock)
                {
                    if (_errorQueue.Count > 0)
                    {
                        errorMessage = _errorQueue.Dequeue();
                    }
                }

                if (errorMessage != null)
                {
                    try
                    {
                        SendErrorToServer(errorMessage);
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(ex);
                    }
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }

        /// <summary>
        /// 发送错误到服务器（参考原始SendErrorLogServer方法）
        /// </summary>
        private void SendErrorToServer(string errorMessage)
        {
            try
            {
                // 构建JSON数据
                var jsonData = BuildErrorReportJson(errorMessage);

                // 发送HTTP请求
                PostWebRequest(_serverUrl, jsonData);
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        /// <summary>
        /// 构建错误上报的JSON数据
        /// </summary>
        private string BuildErrorReportJson(string errorMessage)
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{");

            // 添加扩展数据
            jsonBuilder.Append("\"extData\":");
            jsonBuilder.Append(GetReportExtraDataJson());
            jsonBuilder.Append(",");

            // 添加错误消息
            jsonBuilder.Append("\"msg\":\"");
            jsonBuilder.Append(EscapeJsonString(errorMessage));
            jsonBuilder.Append("\"");

            jsonBuilder.Append("}");
            return jsonBuilder.ToString();
        }

        /// <summary>
        /// 获取扩展数据的JSON字符串
        /// </summary>
        private string GetReportExtraDataJson()
        {
            var jsonBuilder = new StringBuilder();
            jsonBuilder.Append("{");

            bool first = true;
            foreach (var kvp in _reportExtraData)
            {
                if (!first)
                    jsonBuilder.Append(",");

                jsonBuilder.Append("\"");
                jsonBuilder.Append(kvp.Key);
                jsonBuilder.Append("\":\"");
                jsonBuilder.Append(EscapeJsonString(kvp.Value?.ToString() ?? ""));
                jsonBuilder.Append("\"");

                first = false;
            }

            jsonBuilder.Append("}");
            return jsonBuilder.ToString();
        }

        /// <summary>
        /// JSON字符串转义
        /// </summary>
        private string EscapeJsonString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "";

            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        /// <summary>
        /// HTTP POST请求（参考原始PostWebRequest方法）
        /// </summary>
        private string PostWebRequest(string postUrl, string paramData)
        {
            string result = string.Empty;
            try
            {
                byte[] byteArray = CompressString(paramData);
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(postUrl));
                webReq.Method = "POST";
                webReq.ContentType = "application/x-www-form-urlencoded";
                webReq.Timeout = 3000;
                webReq.ContentLength = byteArray.Length;

                using (Stream newStream = webReq.GetRequestStream())
                {
                    newStream.Write(byteArray, 0, byteArray.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)webReq.GetResponse())
                using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.Default))
                {
                    result = sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }

            return result;
        }

        /// <summary>
        /// 压缩字符串（参考原始CompressString方法）
        /// </summary>
        private byte[] CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gzip.Write(buffer, 0, buffer.Length);
                }
                return memoryStream.ToArray();
            }
        }

        /// <summary>
        /// 设置扩展上报数据
        /// </summary>
        public void SetReportExtraData(string key, object data)
        {
            if (string.IsNullOrEmpty(key))
                return;

            _reportExtraData[key] = data;
        }

        /// <summary>
        /// 设置服务器上报URL
        /// </summary>
        public void SetServerReportUrl(string url)
        {
            _serverUrl = url ?? string.Empty;
        }

        /// <summary>
        /// 获取服务器上报URL
        /// </summary>
        public string GetServerReportUrl() => _serverUrl;
        #endregion

        #region 刷新和释放
        /// <summary>
        /// 刷新所有输出器
        /// </summary>
        public void Flush()
        {
            if (_isDisposed)
                return;

            // 等待队列处理完毕
            int waitCount = 0;
            while (!_logQueue.IsEmpty && waitCount < 100)
            {
                Thread.Sleep(10);
                waitCount++;
            }

            // 刷新所有输出器
            lock (_appendersLock)
            {
                foreach (var appender in _appenders)
                {
                    try
                    {
                        appender?.Flush();
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(ex);
                    }
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            // 停止写入线程
            _isRunning = false;
            _writeThread?.Join(1000);

            // 停止服务器上报线程
            _isServerReportRunning = false;
            _serverReportThread?.Join(1000);

            // 停止系统日志监控
            if (_systemLogMonitorEnabled)
            {
                SystemLogMonitor.Instance.Release();
            }

            // 刷新并释放所有输出器
            Flush();
            ClearAppenders();

            // 清空队列
            _logQueue?.Clear();
        }
        #endregion
    }

    /// <summary>
    /// 静态便捷访问类 - 提供零开销的条件日志记录
    /// </summary>
    public static class EZLog
    {
        /// <summary>获取Logger实例</summary>
        public static ILogger Logger => EZLoggerManager.Instance;

        /// <summary>是否启用指定级别</summary>
        public static bool IsLevelEnabled(LogLevel level) => Logger.IsLevelEnabled(level);

        #region 零开销条件日志记录器
        // 缓存条件日志记录器实例，避免重复创建
        private static ConditionalLogger _cachedL;  // Log
        private static ConditionalLogger _cachedW;  // Warning
        private static ConditionalLogger _cachedA;  // Assert
        private static ConditionalLogger _cachedE;  // Error
        private static ConditionalLogger _cachedX;  // Exception

        // 缓存上次检查的级别状态，用于检测变化
        private static LogLevel _lastCheckedLevels = LogLevel.None;

        /// <summary>
        /// Log级别条件日志记录器 - 零开销设计 (对应Unity LogType.Log)
        /// 当级别被禁用时返回null，避免任何性能开销
        /// 使用方式: EZLog.Log?.Log("tag", "message")
        /// </summary>
        public static ConditionalLogger Log
        {
            get
            {
                var currentLevels = EZLoggerManager.Instance.EnabledLevels;
                if (currentLevels != _lastCheckedLevels)
                {
                    RefreshCachedLoggers(currentLevels);
                }
                return _cachedL;
            }
        }

        /// <summary>
        /// Warning级别条件日志记录器 - 零开销设计 (对应Unity LogType.Warning)
        /// 使用方式: EZLog.Warning?.Log("tag", "message")
        /// </summary>
        public static ConditionalLogger Warning
        {
            get
            {
                var currentLevels = EZLoggerManager.Instance.EnabledLevels;
                if (currentLevels != _lastCheckedLevels)
                {
                    RefreshCachedLoggers(currentLevels);
                }
                return _cachedW;
            }
        }

        /// <summary>
        /// Assert级别条件日志记录器 - 零开销设计 (对应Unity LogType.Assert)
        /// 使用方式: EZLog.Assert?.Log("tag", "message")
        /// </summary>
        public static ConditionalLogger Assert
        {
            get
            {
                var currentLevels = EZLoggerManager.Instance.EnabledLevels;
                if (currentLevels != _lastCheckedLevels)
                {
                    RefreshCachedLoggers(currentLevels);
                }
                return _cachedA;
            }
        }

        /// <summary>
        /// Error级别条件日志记录器 - 零开销设计 (对应Unity LogType.Error)
        /// 使用方式: EZLog.Error?.Log("tag", "message")
        /// </summary>
        public static ConditionalLogger Error
        {
            get
            {
                var currentLevels = EZLoggerManager.Instance.EnabledLevels;
                if (currentLevels != _lastCheckedLevels)
                {
                    RefreshCachedLoggers(currentLevels);
                }
                return _cachedE;
            }
        }

        /// <summary>
        /// Exception级别条件日志记录器 - 零开销设计 (对应Unity LogType.Exception)
        /// 使用方式: EZLog.Exception?.Log("tag", "message")
        /// </summary>
        public static ConditionalLogger Exception
        {
            get
            {
                var currentLevels = EZLoggerManager.Instance.EnabledLevels;
                if (currentLevels != _lastCheckedLevels)
                {
                    RefreshCachedLoggers(currentLevels);
                }
                return _cachedX;
            }
        }

        /// <summary>
        /// 刷新缓存的条件日志记录器
        /// 只有在级别发生变化时才会调用，避免重复创建对象
        /// </summary>
        private static void RefreshCachedLoggers(LogLevel currentLevels)
        {
            var manager = EZLoggerManager.Instance;

            _cachedL = currentLevels.Contains(LogLevel.Log) ? manager.GetConditionalLogger(LogLevel.Log) : null;
            _cachedW = currentLevels.Contains(LogLevel.Warning) ? manager.GetConditionalLogger(LogLevel.Warning) : null;
            _cachedA = currentLevels.Contains(LogLevel.Assert) ? manager.GetConditionalLogger(LogLevel.Assert) : null;
            _cachedE = currentLevels.Contains(LogLevel.Error) ? manager.GetConditionalLogger(LogLevel.Error) : null;
            _cachedX = currentLevels.Contains(LogLevel.Exception) ? manager.GetConditionalLogger(LogLevel.Exception) : null;

            _lastCheckedLevels = currentLevels;
        }
        #endregion

        #region 传统便捷方法（保持向下兼容）
        // 新的方法，与Unity LogType对齐
        public static void LogLog(string tag, string message) => Logger.LogLog(tag, message);
        public static void LogWarning(string tag, string message) => Logger.LogWarning(tag, message);
        public static void LogAssert(string tag, string message) => Logger.LogAssert(tag, message);
        public static void LogError(string tag, string message) => Logger.LogError(tag, message);
        public static void LogException(string tag, string message) => Logger.LogException(tag, message);

        // 使用对象作为标签
        public static void LogLog(object tag, string message) => Logger.LogLog(tag?.ToString(), message);
        public static void LogWarning(object tag, string message) => Logger.LogWarning(tag?.ToString(), message);
        public static void LogAssert(object tag, string message) => Logger.LogAssert(tag?.ToString(), message);
        public static void LogError(object tag, string message) => Logger.LogError(tag?.ToString(), message);
        public static void LogException(object tag, string message) => Logger.LogException(tag?.ToString(), message);

        // 格式化方法
        public static void LogLogFormat(string tag, string format, params object[] args) => Logger.LogLogFormat(tag, format, args);
        public static void LogWarningFormat(string tag, string format, params object[] args) => Logger.LogWarningFormat(tag, format, args);
        public static void LogAssertFormat(string tag, string format, params object[] args) => Logger.LogAssertFormat(tag, format, args);
        public static void LogErrorFormat(string tag, string format, params object[] args) => Logger.LogErrorFormat(tag, format, args);
        public static void LogExceptionFormat(string tag, string format, params object[] args) => Logger.LogExceptionFormat(tag, format, args);

        // 异常记录
        public static void LogException(string tag, System.Exception ex) => Logger.LogException(tag, ex);
        #endregion

        #region 静态级别控制方法
        /// <summary>启用指定级别</summary>
        public static void EnableLevel(LogLevel level) => EZLoggerManager.Instance.EnableLevel(level);

        /// <summary>禁用指定级别</summary>
        public static void DisableLevel(LogLevel level) => EZLoggerManager.Instance.DisableLevel(level);

        /// <summary>切换指定级别的开关状态</summary>
        public static void ToggleLevel(LogLevel level) => EZLoggerManager.Instance.ToggleLevel(level);

        /// <summary>设置为仅错误级别</summary>
        public static void SetErrorOnly() => EZLoggerManager.Instance.SetErrorOnly();

        /// <summary>设置为警告及以上级别</summary>
        public static void SetWarningAndAbove() => EZLoggerManager.Instance.SetWarningAndAbove();

        /// <summary>设置为错误及以上级别</summary>
        public static void SetErrorAndAbove() => EZLoggerManager.Instance.SetErrorAndAbove();

        /// <summary>启用所有级别</summary>
        public static void EnableAll() => EZLoggerManager.Instance.EnableAll();

        /// <summary>禁用所有级别</summary>
        public static void DisableAll() => EZLoggerManager.Instance.DisableAll();

        /// <summary>获取当前启用的级别</summary>
        public static LogLevel GetEnabledLevels() => EZLoggerManager.Instance.EnabledLevels;

        /// <summary>设置启用的级别</summary>
        public static void SetEnabledLevels(LogLevel levels) => EZLoggerManager.Instance.EnabledLevels = levels;
        #endregion

        #region 系统监控控制方法
        /// <summary>启用系统日志监控</summary>
        public static void EnableSystemLogMonitor(bool enabled = true) => EZLoggerManager.Instance.EnableSystemLogMonitor(enabled);

        /// <summary>启用错误日志服务器上报</summary>
        public static void EnableServerReporting(bool enabled = true) => EZLoggerManager.Instance.EnableServerReporting(enabled);

        /// <summary>系统日志监控是否启用</summary>
        public static bool IsSystemLogMonitorEnabled => EZLoggerManager.Instance.IsSystemLogMonitorEnabled;

        /// <summary>服务器上报是否启用</summary>
        public static bool IsServerReportingEnabled => EZLoggerManager.Instance.IsServerReportingEnabled;

        /// <summary>设置服务器上报URL</summary>
        public static void SetServerReportUrl(string url) => EZLoggerManager.Instance.SetServerReportUrl(url);

        /// <summary>获取服务器上报URL</summary>
        public static string GetServerReportUrl() => EZLoggerManager.Instance.GetServerReportUrl();

        /// <summary>设置扩展上报数据</summary>
        public static void SetReportExtraData(string key, object data) => EZLoggerManager.Instance.SetReportExtraData(key, data);
        #endregion
    }
}
