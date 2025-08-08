using System;
using System.Collections.Generic;
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
                
            if (_configuration.EnableAsyncWrite && _isRunning)
            {
                // 异步写入
                if (!_logQueue.Enqueue(message))
                {
                    // 队列已满，强制入队（会丢弃最旧的消息）
                    _logQueue.ForceEnqueue(message);
                }
            }
            else
            {
                // 同步写入
                WriteToAppenders(message);
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
        public void LogError(string tag, string message) => Log(LogLevel.Error, tag, message);
        public void LogException(string tag, string message) => Log(LogLevel.Exception, tag, message);
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
        
        private void WriteToAppenders(LogMessage message)
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
                    appender?.WriteLog(message);
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
#if UNITY_2018_1_OR_NEWER
            return UnityEngine.Time.frameCount;
#else
            return 0;
#endif
        }
        
        private void HandleInternalError(Exception ex)
        {
#if UNITY_2018_1_OR_NEWER
            UnityEngine.Debug.LogError($"[EZLogger] Internal error: {ex.Message}");
#else
            Console.WriteLine($"[EZLogger] Internal error: {ex.Message}");
#endif
        }
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
    }
}
