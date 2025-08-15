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
using UnityEngine;

namespace EZLogger
{
    /// <summary>
    /// EZ Logger 主管理器
    /// </summary>
    public sealed class EZLoggerManager : ILogger, IDisposable
    {
        #region 单例实现
        private static volatile EZLoggerManager? _instance;
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
        private volatile bool _isDisposed;
        private volatile bool _isInitializing;

        private LoggerConfiguration _configuration;
        private readonly Dictionary<LogLevel, ConditionalLogger> _conditionalLoggers = new Dictionary<LogLevel, ConditionalLogger>();

        // 系统日志监控
        private bool _systemLogMonitorEnabled;
        private bool _serverReportingEnabled;

        // 服务器上报相关
        private readonly Queue<string> _errorQueue = new Queue<string>();
        private readonly object _errorQueueLock = new object();
        private Thread? _serverReportThread;
        private volatile bool _isServerReportRunning;
        private readonly Dictionary<string, object> _reportExtraData = new Dictionary<string, object>();
        private string _serverUrl = string.Empty;

        // WebGL支持相关
        private EZLoggerUpdateDriver? _updateDriver;
        private GameObject? _updateDriverObject;

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
        public static event System.Action<LogLevel>? OnLevelsChanged;

        /// <summary>当前配置</summary>
        public LoggerConfiguration Configuration
        {
            get => _configuration;
            set
            {
                _configuration = value ?? LoggerConfiguration.CreateDefault();
                EnabledLevels = _configuration.GlobalEnabledLevels;

                // 运行时重新配置输出器（初始化时跳过，避免递归调用）
                if (!_isInitializing)
                {
                    RefreshAppenders();
                }
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

        #endregion

        #region 构造函数和初始化
        private EZLoggerManager()
        {
            // 标记正在初始化，防止递归调用
            _isInitializing = true;

            // 从运行时配置加载器加载配置
            _configuration = RuntimeSettingsLoader.LoadConfiguration();

            // 初始化条件日志记录器
            InitializeConditionalLoggers();

            // 初始化系统日志监控
            InitializeSystemLogMonitor();

            // 初始化设备信息
            InitializeDeviceInfo();

            // WebGL平台需要创建Update驱动器
            if (PlatformCapabilities.RequiresUpdateDriven)
            {
                InitializeUpdateDriver();
            }

            // 添加默认的Unity输出器（在所有其他初始化完成后）
            AddDefaultAppenders();

            // 注册Unity应用退出事件
#if UNITY_2018_1_OR_NEWER
            UnityEngine.Application.quitting += OnApplicationQuitting;
#endif

            // 初始化完成
            _isInitializing = false;
        }

#if UNITY_2018_1_OR_NEWER
        /// <summary>
        /// Unity应用退出时的处理
        /// </summary>
        private void OnApplicationQuitting()
        {
            // 在应用退出时主动释放资源
            try
            {
                Dispose();
            }
            catch (ThreadAbortException)
            {
                // 忽略线程终止异常
            }
        }
#endif

        /// <summary>
        /// 添加默认输出器（仅在初始化时调用）
        /// </summary>
        private void AddDefaultAppenders()
        {
            // 初始化时直接创建输出器，不通过RefreshAppenders避免递归
            try
            {
                if (_configuration.UnityConsole.Enabled)
                {
                    var unityAppender = new UnityAppender();
                    unityAppender.Initialize(_configuration.UnityConsole);
                    AddAppender(unityAppender);
                }

                if (_configuration.FileOutput.Enabled)
                {
                    var fileAppender = new FileAppender(_configuration.Timezone);
                    fileAppender.Initialize(_configuration.FileOutput, _configuration.Timezone);
                    AddAppender(fileAppender);
                }
            }
            catch (Exception ex)
            {
                HandleInternalError(new Exception($"Failed to initialize default appenders: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// 刷新输出器配置 - 支持运行时动态启用/禁用
        /// </summary>
        private void RefreshAppenders()
        {
            if (_configuration == null || _isDisposed || _isInitializing)
                return;

            // 管理Unity控制台输出器
            ManageUnityAppender();

            // 管理文件输出器
            ManageFileAppender();
        }

        /// <summary>
        /// 管理Unity控制台输出器的启用/禁用
        /// </summary>
        private void ManageUnityAppender()
        {
            const string UNITY_APPENDER_NAME = "Unity Console";
            var existingAppender = GetAppenderByName(UNITY_APPENDER_NAME);

            if (_configuration.UnityConsole.Enabled)
            {
                if (existingAppender == null)
                {
                    // 需要创建新的Unity输出器
                    try
                    {
                        var unityAppender = new UnityAppender();
                        unityAppender.Initialize(_configuration.UnityConsole);
                        AddAppender(unityAppender);
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(new Exception($"Failed to initialize Unity appender: {ex.Message}", ex));
                    }
                }
                else
                {
                    // 已存在，重新配置
                    try
                    {
                        existingAppender.Initialize(_configuration.UnityConsole);
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(new Exception($"Failed to reconfigure Unity appender: {ex.Message}", ex));
                    }
                }
            }
            else
            {
                // 需要移除Unity输出器
                if (existingAppender != null)
                {
                    RemoveAppender(existingAppender);
                    try
                    {
                        existingAppender.Dispose();
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(new Exception($"Failed to dispose Unity appender: {ex.Message}", ex));
                    }
                }
            }
        }

        /// <summary>
        /// 管理文件输出器的启用/禁用
        /// </summary>
        private void ManageFileAppender()
        {
            const string FILE_APPENDER_NAME = "FileAppender";
            var existingAppender = GetAppenderByName(FILE_APPENDER_NAME);

            if (_configuration.FileOutput.Enabled)
            {
                if (existingAppender == null)
                {
                    // 需要创建新的文件输出器
                    try
                    {
                        var fileAppender = new FileAppender(_configuration.Timezone);
                        fileAppender.Initialize(_configuration.FileOutput, _configuration.Timezone);
                        AddAppender(fileAppender);
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(new Exception($"Failed to initialize File appender: {ex.Message}", ex));
                    }
                }
                else
                {
                    // 已存在，重新配置
                    try
                    {
                        existingAppender.Initialize(_configuration.FileOutput);
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(new Exception($"Failed to reconfigure File appender: {ex.Message}", ex));
                    }
                }
            }
            else
            {
                // 需要移除文件输出器
                if (existingAppender != null)
                {
                    RemoveAppender(existingAppender);
                    try
                    {
                        existingAppender.Dispose();
                    }
                    catch (Exception ex)
                    {
                        HandleInternalError(new Exception($"Failed to dispose File appender: {ex.Message}", ex));
                    }
                }
            }
        }

        /// <summary>
        /// 根据名称获取输出器
        /// </summary>
        private ILogAppender GetAppenderByName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            lock (_appendersLock)
            {
                return _appenders.FirstOrDefault(a => a.Name == name);
            }
        }

        /// <summary>
        /// 日志级别到Logger类型的映射配置
        /// 便于扩展和维护不同级别的特殊处理逻辑
        /// </summary>
        private static readonly Dictionary<LogLevel, System.Func<LogLevel, ILogger, ConditionalLogger>> LoggerTypeMapping =
            new Dictionary<LogLevel, System.Func<LogLevel, ILogger, ConditionalLogger>>
            {
                // 基础级别使用ConditionalLogger
                { LogLevel.Log, (level, logger) => new ConditionalLogger(level, logger) },
                { LogLevel.Warning, (level, logger) => new ConditionalLogger(level, logger) },
                { LogLevel.Assert, (level, logger) => new ConditionalLogger(level, logger) },
                
                // 关键级别使用CriticalConditionalLogger（包含防重复和服务器上报）
                { LogLevel.Error, (level, logger) => new CriticalConditionalLogger(level, logger) },
                { LogLevel.Exception, (level, logger) => new CriticalConditionalLogger(level, logger) },
                
                // 可以在这里轻松添加新的特殊Logger类型
                // 例如：{ LogLevel.Performance, (level, logger) => new PerformanceConditionalLogger(level, logger) },
            };

        /// <summary>
        /// 注册自定义的Logger类型工厂方法
        /// 允许在运行时为特定级别注册特殊的Logger实现
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="factory">Logger工厂方法</param>
        /// <example>
        /// // 注册一个性能专用的Logger
        /// EZLoggerManager.RegisterLoggerType(LogLevel.Log, 
        ///     (level, logger) => new PerformanceConditionalLogger(level, logger));
        /// </example>
        public static void RegisterLoggerType(LogLevel level, System.Func<LogLevel, ILogger, ConditionalLogger> factory)
        {
            if (factory == null)
                throw new System.ArgumentNullException(nameof(factory));

            lock (_instanceLock)
            {
                LoggerTypeMapping[level] = factory;

                // 如果实例已经创建，需要重新初始化该级别的Logger
                if (_instance != null)
                {
                    _instance.ReinitializeLogger(level);
                }
            }
        }

        /// <summary>
        /// 获取指定级别当前使用的Logger类型名称
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <returns>Logger类型名称</returns>
        public static string GetLoggerTypeName(LogLevel level)
        {
            if (LoggerTypeMapping.TryGetValue(level, out var factory))
            {
                // 创建一个临时实例来获取类型信息
                var tempLogger = factory(level, null);
                return tempLogger?.GetType().Name ?? "Unknown";
            }
            return "ConditionalLogger"; // 默认类型
        }

        /// <summary>
        /// 重新初始化指定级别的Logger（用于运行时类型变更）
        /// </summary>
        private void ReinitializeLogger(LogLevel level)
        {
            if (LoggerTypeMapping.TryGetValue(level, out var factory))
            {
                _conditionalLoggers[level] = factory(level, this);
            }
            else
            {
                _conditionalLoggers[level] = new ConditionalLogger(level, this);
            }
        }

        private void InitializeConditionalLoggers()
        {
            // 使用映射配置创建条件日志记录器
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                // 跳过复合级别和无效级别
                if (level == LogLevel.None || level == LogLevel.All || level == LogLevel.ErrorAndWarning)
                    continue;

                // 从映射中获取对应的工厂方法
                if (LoggerTypeMapping.TryGetValue(level, out var factory))
                {
                    _conditionalLoggers[level] = factory(level, this);
                }
                else
                {
                    // 如果没有特殊配置，使用默认的ConditionalLogger
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
                SetReportExtraData("timestamp", _configuration.Timezone.FormatTime());
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        private void OnSystemLogReceived(string condition, string stackTrace, LogLevel logLevel)
        {
            // 🎯 系统错误处理：区分系统堆栈和手动调用堆栈
            LogMessage logMessage;

            if (!string.IsNullOrEmpty(stackTrace))
            {
                // 系统提供了堆栈跟踪，直接使用系统堆栈
                logMessage = LogMessage.CreateWithSystemStackTrace(logLevel, "System", condition, stackTrace);
            }
            else
            {
                // 系统没有提供堆栈跟踪，使用普通日志消息
                logMessage = new LogMessage(logLevel, "System", condition, _configuration, _configuration?.Timezone);
            }

            // 记录系统日志
            Log(logMessage);

            // 如果启用服务器上报且是错误或异常，则上报
            if (_serverReportingEnabled && StackTraceHelper.IsErrorLevel(logLevel))
            {
                // 构建完整的消息（包含堆栈跟踪）
                var fullMessage = string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}";
                ReportToServer(fullMessage, logLevel, "System");
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

                // 如果需要Update驱动，注册到驱动器
                if (appender.RequiresUpdate && _updateDriver != null)
                {
                    _updateDriver.RegisterAppender(appender);
                }
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
                bool removed = _appenders.Remove(appender);

                if (removed && appender.RequiresUpdate && _updateDriver != null)
                {
                    _updateDriver.UnregisterAppender(appender);
                }

                return removed;
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

            // 如果级别未启用则直接返回false
            if (!EnabledLevels.Contains(level))
                return false;

            return EnabledLevels.Contains(level);
        }

        /// <summary>
        /// 记录日志消息 - 优化后的简化设计
        /// 直接分发给所有输出器，让它们自己决定同步/异步处理
        /// </summary>
        public void Log(LogMessage message)
        {
            if (!IsLevelEnabled(message.Level))
                return;

            // 直接写入所有输出器，让它们自己管理异步
            WriteToAllAppenders(message);
        }

        /// <summary>
        /// 记录日志（简化版本）- 支持智能堆栈跟踪
        /// </summary>
        public void Log(LogLevel level, string tag, string message)
        {
            if (!IsLevelEnabled(level))
                return;

            // 使用新的智能堆栈跟踪构造函数
            var logMessage = new LogMessage(level, tag, message, _configuration, _configuration?.Timezone);
            Log(logMessage);
        }

        // 注意：传统便捷方法已移除，专注于零开销设计
        // 推荐使用：EZLog.Error?.Log("tag", "message") 等零开销API
        #endregion

        #region 输出器写入处理 - 优化后的简化设计

        /// <summary>
        /// 写入到所有输出器 - 优化后的统一入口
        /// 每个输出器自己决定是否异步处理
        /// </summary>
        private void WriteToAllAppenders(LogMessage message)
        {
            ILogAppender[] appenders;
            lock (_appendersLock)
            {
                if (_appenders.Count == 0)
                    return;
                appenders = _appenders.ToArray();
            }

            foreach (var appender in appenders)
            {
                try
                {
                    if (appender.IsEnabled && appender.SupportedLevels.Contains(message.Level))
                    {
                        // 让输出器自己决定同步/异步处理
                        appender.WriteLog(message);
                    }
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
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

        /// <summary>
        /// 初始化Update驱动器（仅WebGL平台）
        /// </summary>
        private void InitializeUpdateDriver()
        {
            try
            {
                _updateDriverObject = new GameObject("EZLogger_UpdateDriver")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                _updateDriver = _updateDriverObject.AddComponent<EZLoggerUpdateDriver>();
                _updateDriver.Initialize();

                // 确保在场景切换时不被销毁
                UnityEngine.Object.DontDestroyOnLoad(_updateDriverObject);

                Debug.Log("[EZLogger] Update驱动器已初始化");
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        /// <summary>
        /// 获取Update驱动器（用于调试和监控）
        /// </summary>
        internal EZLoggerUpdateDriver? GetUpdateDriver()
        {
            return _updateDriver;
        }

        /// <summary>
        /// 获取WebGL状态信息
        /// </summary>
        public string GetWebGLStatus()
        {
            if (_updateDriver != null)
            {
                return $"WebGL状态: 注册输出器={_updateDriver.RegisteredAppendersCount}";
            }
            return "WebGL驱动器未启用";
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
        /// 上报错误到服务器（统一入口）
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="logLevel">日志级别</param>
        /// <param name="tag">日志标签（用于区分来源：System=系统抓取，其他=自己API）</param>
        internal void ReportToServer(string message, LogLevel logLevel, string tag = "System")
        {
            // 如果没有配置服务器地址，跳过上报
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

            // 格式化错误消息，添加帧数和标签
            var formattedMessage = $"[FRAME:{GetCurrentFrameCount()}][{tag}]{message}";

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
                    catch (ThreadAbortException)
                    {
                        // 线程被主动终止，正常行为
                        break;
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

        /// <summary>
        /// 手动刷新输出器配置 - 当用户在运行时修改配置后调用
        /// </summary>
        public void RefreshConfiguration()
        {
            RefreshAppenders();
        }

        /// <summary>
        /// 打开日志文件夹 - 跨平台支持，包括WebGL平台的特殊处理
        /// </summary>
        /// <param name="openMode">WebGL平台的打开模式：download=下载文件，list=显示文件列表</param>
        public void OpenLogFolder(string openMode = "list")
        {
            var folderPath = _configuration.GetLogFolderPath();
            Debug.Log($"[EZLogger] 尝试打开日志文件夹: {folderPath}");

            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                // WebGL平台：使用特殊的文件访问方式
                HandleWebGLLogFolder(folderPath, openMode);
#elif UNITY_EDITOR_WIN
                // Windows编辑器：使用explorer命令
                folderPath = folderPath.Replace('/', '\\'); // 统一使用反斜杠
                System.Diagnostics.Process.Start("explorer.exe", $"\"{folderPath}\"");
                Debug.Log($"[EZLogger] 在Windows资源管理器中打开: {folderPath}");
#elif UNITY_EDITOR_OSX
                // macOS编辑器：使用open命令
                System.Diagnostics.Process.Start("open", $"\"{folderPath}\"");
                Debug.Log($"[EZLogger] 在macOS Finder中打开: {folderPath}");
#elif UNITY_EDITOR_LINUX
                // Linux编辑器：使用xdg-open命令
                System.Diagnostics.Process.Start("xdg-open", $"\"{folderPath}\"");
                Debug.Log($"[EZLogger] 在Linux文件管理器中打开: {folderPath}");
#else
                // 其他平台运行时处理
                HandleRuntimeLogFolder(folderPath);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EZLogger] 打开日志文件夹失败: {ex.Message}");

                // 在WebGL平台提供备用方案
#if UNITY_WEBGL && !UNITY_EDITOR
                Debug.Log("[EZLogger] 尝试使用备用方案...");
                try
                {
                    WebGLFileSyncUtil.ShowLogFilesList(folderPath);
                }
                catch (Exception fallbackEx)
                {
                    Debug.LogError($"[EZLogger] 备用方案也失败了: {fallbackEx.Message}");
                }
#endif
            }
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        /// <summary>
        /// 处理WebGL平台的日志文件夹访问
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        /// <param name="openMode">打开模式</param>
        private void HandleWebGLLogFolder(string folderPath, string openMode)
        {
            Debug.Log($"[EZLogger] WebGL平台日志文件夹访问 - 模式: {openMode}");
            Debug.Log($"[EZLogger] {WebGLFileSyncUtil.GetWebGLStatus()}");
            
            // 显示安全提示信息
            Debug.Log($"[EZLogger] {WebGLFileSyncUtil.GetWebGLSecurityInfo()}");
            
            switch (openMode.ToLower())
            {
                case "download":
                    // 下载模式：将日志文件夹打包下载
                    Debug.Log("[EZLogger] 启动下载模式 - 将日志文件夹打包为ZIP下载");
                    WebGLFileSyncUtil.DownloadLogFolder(folderPath);
                    break;
                    
                case "list":
                default:
                    // 列表模式：在新标签页显示文件列表（默认模式）
                    Debug.Log("[EZLogger] 启动列表模式 - 在新标签页显示日志文件列表");
                    WebGLFileSyncUtil.ShowLogFilesList(folderPath);
                    break;
            }
        }
#endif

        /// <summary>
        /// 处理运行时平台的日志文件夹访问
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        private void HandleRuntimeLogFolder(string folderPath)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    // Windows运行时
                    folderPath = folderPath.Replace('/', '\\');
                    System.Diagnostics.Process.Start("explorer.exe", $"\"{folderPath}\"");
                    Debug.Log($"[EZLogger] Windows运行时打开: {folderPath}");
                    break;

                case RuntimePlatform.OSXPlayer:
                    // macOS运行时
                    System.Diagnostics.Process.Start("open", $"\"{folderPath}\"");
                    Debug.Log($"[EZLogger] macOS运行时打开: {folderPath}");
                    break;

                case RuntimePlatform.LinuxPlayer:
                    // Linux运行时
                    System.Diagnostics.Process.Start("xdg-open", $"\"{folderPath}\"");
                    Debug.Log($"[EZLogger] Linux运行时打开: {folderPath}");
                    break;

                case RuntimePlatform.Android:
                    // Android平台
                    Debug.LogWarning("[EZLogger] Android平台无法直接打开文件夹，日志路径: " + folderPath);
                    Debug.Log("[EZLogger] 建议使用adb命令或设备文件管理器查看日志文件");
                    break;

                case RuntimePlatform.IPhonePlayer:
                    // iOS平台
                    Debug.LogWarning("[EZLogger] iOS平台无法直接打开文件夹，日志路径: " + folderPath);
                    Debug.Log("[EZLogger] 日志文件位于应用沙盒中，可通过iTunes或Xcode查看");
                    break;

                default:
                    // 通用方案
                    try
                    {
                        System.Diagnostics.Process.Start(folderPath);
                        Debug.Log($"[EZLogger] 使用通用方法打开: {folderPath}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[EZLogger] 当前平台({Application.platform})不支持直接打开文件夹: {ex.Message}");
                        Debug.Log($"[EZLogger] 日志文件路径: {folderPath}");
                    }
                    break;
            }
        }

        /// <summary>
        /// WebGL平台专用：下载日志文件夹
        /// </summary>
        public void DownloadLogFolder()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var folderPath = _configuration.GetLogFolderPath();
            Debug.Log("[EZLogger] 开始下载日志文件夹...");
            WebGLFileSyncUtil.DownloadLogFolder(folderPath);
#else
            Debug.LogWarning("[EZLogger] DownloadLogFolder仅在WebGL平台可用");
#endif
        }

        /// <summary>
        /// WebGL平台专用：显示日志文件列表
        /// </summary>
        public void ShowLogFilesList()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var folderPath = _configuration.GetLogFolderPath();
            Debug.Log("[EZLogger] 显示日志文件列表...");
            WebGLFileSyncUtil.ShowLogFilesList(folderPath);
#else
            Debug.LogWarning("[EZLogger] ShowLogFilesList仅在WebGL平台可用");
#endif
        }

        /// <summary>
        /// 获取平台特定的日志访问说明
        /// </summary>
        /// <returns>说明文本</returns>
        public string GetLogAccessInfo()
        {
            switch (Application.platform)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                case RuntimePlatform.WebGLPlayer:
                    return WebGLFileSyncUtil.GetWebGLSecurityInfo();
#endif
                case RuntimePlatform.Android:
                    return "Android平台:\n• 日志存储在应用私有目录\n• 需要root权限或adb工具访问\n• 路径: " + _configuration.GetLogFolderPath();

                case RuntimePlatform.IPhonePlayer:
                    return "iOS平台:\n• 日志存储在应用沙盒中\n• 可通过iTunes文件共享或Xcode访问\n• 路径: " + _configuration.GetLogFolderPath();

                default:
                    return "桌面平台:\n• 可直接通过文件管理器访问\n• 路径: " + _configuration.GetLogFolderPath();
            }
        }
        #endregion

        #region 刷新和释放
        /// <summary>
        /// 刷新所有输出器 - 优化后直接刷新
        /// </summary>
        public void Flush()
        {
            if (_isDisposed)
                return;

            // 直接刷新所有输出器，让它们自己处理内部队列
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

            // 不再需要停止主写入线程（已移除）

            // 停止服务器上报线程
            _isServerReportRunning = false;
            if (_serverReportThread != null && _serverReportThread.IsAlive)
            {
                if (!_serverReportThread.Join(1000))
                {
                    // 如果线程在1秒内没有正常结束，强制终止
                    try
                    {
                        _serverReportThread.Abort();
                    }
                    catch (ThreadAbortException)
                    {
                        // 忽略线程终止异常
                    }
                }
            }

            // 停止系统日志监控
            if (_systemLogMonitorEnabled)
            {
                SystemLogMonitor.Instance.Release();
            }

            // 清理Update驱动器
            if (_updateDriverObject != null)
            {
                try
                {
                    if (UnityEngine.Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(_updateDriverObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(_updateDriverObject);
                    }
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
                finally
                {
                    _updateDriverObject = null;
                    _updateDriver = null;
                }
            }

            // 刷新并释放所有输出器
            Flush();
            ClearAppenders();
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
        private static ConditionalLogger? _cachedL;  // Log
        private static ConditionalLogger? _cachedW;  // Warning
        private static ConditionalLogger? _cachedA;  // Assert
        private static ConditionalLogger? _cachedE;  // Error
        private static ConditionalLogger? _cachedX;  // Exception

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

        // 注意：传统便捷方法已移除，框架专注于零开销设计
        // 
        // 推荐使用模式：
        // ✅ EZLog.Log?.Log("tag", "message")      - 零开销，对应Unity LogType.Log
        // ✅ EZLog.Warning?.Log("tag", "message")  - 零开销，对应Unity LogType.Warning  
        // ✅ EZLog.Error?.Log("tag", "message")    - 零开销，对应Unity LogType.Error
        // ✅ EZLog.Exception?.Log("tag", "message") - 零开销，对应Unity LogType.Exception
        // 
        // 这样设计的好处：
        // 1. 禁用级别时连参数都不会计算
        // 2. 代码更简洁，维护成本更低
        // 3. 完全与Unity LogType对齐

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

        /// <summary>手动刷新输出器配置</summary>
        public static void RefreshConfiguration() => EZLoggerManager.Instance.RefreshConfiguration();

        /// <summary>打开日志文件夹</summary>
        /// <param name="openMode">WebGL平台的打开模式：download=下载文件，list=显示文件列表</param>
        public static void OpenLogFolder(string openMode = "list") => EZLoggerManager.Instance.OpenLogFolder(openMode);

        /// <summary>WebGL平台专用：下载日志文件夹</summary>
        public static void DownloadLogFolder() => EZLoggerManager.Instance.DownloadLogFolder();

        /// <summary>WebGL平台专用：显示日志文件列表</summary>
        public static void ShowLogFilesList() => EZLoggerManager.Instance.ShowLogFilesList();

        /// <summary>获取平台特定的日志访问说明</summary>
        /// <returns>说明文本</returns>
        public static string GetLogAccessInfo() => EZLoggerManager.Instance.GetLogAccessInfo();

        #region Logger类型管理
        /// <summary>注册自定义Logger类型</summary>
        /// <param name="level">日志级别</param>
        /// <param name="factory">Logger工厂方法</param>
        public static void RegisterLoggerType(LogLevel level, System.Func<LogLevel, ILogger, ConditionalLogger> factory)
            => EZLoggerManager.RegisterLoggerType(level, factory);

        /// <summary>获取指定级别的Logger类型名称</summary>
        /// <param name="level">日志级别</param>
        /// <returns>Logger类型名称</returns>
        public static string GetLoggerTypeName(LogLevel level) => EZLoggerManager.GetLoggerTypeName(level);

        /// <summary>
        /// 获取所有级别的Logger类型映射信息
        /// </summary>
        /// <returns>级别到Logger类型的映射字典</returns>
        public static Dictionary<LogLevel, string> GetAllLoggerTypes()
        {
            var result = new Dictionary<LogLevel, string>();
            foreach (LogLevel level in System.Enum.GetValues(typeof(LogLevel)))
            {
                if (level != LogLevel.None && level != LogLevel.All && level != LogLevel.ErrorAndWarning)
                {
                    result[level] = GetLoggerTypeName(level);
                }
            }
            return result;
        }
        #endregion
        #endregion
    }
}
