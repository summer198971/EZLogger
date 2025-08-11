using System;

namespace EZLogger
{
    /// <summary>
    /// 日志记录器接口 - 简化版，专注于核心功能
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
        /// 记录日志消息（核心方法）
        /// </summary>
        void Log(LogMessage message);

        /// <summary>
        /// 记录日志（简化版本）
        /// </summary>
        void Log(LogLevel level, string tag, string message);
    }

    /// <summary>
    /// 日志记录器扩展方法 - 简化版，只保留必要功能
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
        /// 记录异常信息
        /// </summary>
        public static void LogException(this ILogger logger, string tag, Exception exception)
        {
            if (!logger.IsLevelEnabled(LogLevel.Exception)) return;
            string message = $"{exception.Message}\n{exception.StackTrace}";
            logger.Log(LogLevel.Exception, tag, message);
        }
    }

    // 注意：传统格式化方法已移除，推荐使用零开销API：
    // ✅ EZLog.Log?.Log("tag", "message")
    // ✅ EZLog.Error?.Log("tag", "message") 
    // 
    // 如需格式化，建议：
    // ✅ EZLog.Log?.Log("tag", $"Value: {value}") - 字符串插值
    // ✅ EZLog.Log?.Log("tag", string.Format("Value: {0}", value)) - 显式格式化
}
