using System;

namespace EZLogger
{
    /// <summary>
    /// 日志记录器接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>当前启用的日志级别</summary>
        LogLevel EnabledLevels { get; set; }
        
        /// <summary>日志记录器名称</summary>
        string Name { get; }
        
        /// <summary>是否启用</summary>
        bool IsEnabled { get; set; }
        
        /// <summary>
        /// 检查指定级别是否启用
        /// </summary>
        bool IsLevelEnabled(LogLevel level);
        
        /// <summary>
        /// 记录日志消息
        /// </summary>
        void Log(LogMessage message);
        
        /// <summary>
        /// 记录日志（简化版本）
        /// </summary>
        void Log(LogLevel level, string tag, string message);
        
        /// <summary>
        /// 记录Log级别日志 (对应Unity LogType.Log)
        /// </summary>
        void LogLog(string tag, string message);
        
        /// <summary>
        /// 记录Warning级别日志 (对应Unity LogType.Warning)
        /// </summary>
        void LogWarning(string tag, string message);
        
        /// <summary>
        /// 记录Assert级别日志 (对应Unity LogType.Assert)
        /// </summary>
        void LogAssert(string tag, string message);
        
        /// <summary>
        /// 记录Error级别日志 (对应Unity LogType.Error)
        /// </summary>
        void LogError(string tag, string message);
        
        /// <summary>
        /// 记录Exception级别日志 (对应Unity LogType.Exception)
        /// </summary>
        void LogException(string tag, string message);
    }
    
    /// <summary>
    /// 日志记录器扩展方法
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// 使用对象作为标签记录日志
        /// </summary>
        public static void Log(this ILogger logger, LogLevel level, object tagObj, string message)
        {
            if (!logger.IsLevelEnabled(level)) return;
            logger.Log(level, tagObj?.ToString() ?? "NULL", message);
        }
        
        /// <summary>
        /// 记录带格式化的日志
        /// </summary>
        public static void LogFormat(this ILogger logger, LogLevel level, string tag, string format, params object[] args)
        {
            if (!logger.IsLevelEnabled(level)) return;
            try
            {
                string message = string.Format(format, args);
                logger.Log(level, tag, message);
            }
            catch (FormatException)
            {
                logger.Log(LogLevel.Error, "Logger", $"Format error: {format}");
            }
        }
        
        /// <summary>
        /// 记录异常信息
        /// </summary>
        public static void LogException(this ILogger logger, string tag, Exception exception)
        {
            if (!logger.IsLevelEnabled(LogLevel.Error)) return;
            string message = $"{exception.Message}\n{exception.StackTrace}";
            logger.Log(LogLevel.Error, tag, message);
        }
        
        // 便捷的格式化方法
        public static void LogLogFormat(this ILogger logger, string tag, string format, params object[] args)
            => logger.LogFormat(LogLevel.Log, tag, format, args);
            
        public static void LogWarningFormat(this ILogger logger, string tag, string format, params object[] args)
            => logger.LogFormat(LogLevel.Warning, tag, format, args);
            
        public static void LogAssertFormat(this ILogger logger, string tag, string format, params object[] args)
            => logger.LogFormat(LogLevel.Assert, tag, format, args);
            
        public static void LogErrorFormat(this ILogger logger, string tag, string format, params object[] args)
            => logger.LogFormat(LogLevel.Error, tag, format, args);
            
        public static void LogExceptionFormat(this ILogger logger, string tag, string format, params object[] args)
            => logger.LogFormat(LogLevel.Exception, tag, format, args);
    }
}
