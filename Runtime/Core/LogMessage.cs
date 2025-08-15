using System;
using System.Diagnostics;

namespace EZLogger
{
    /// <summary>
    /// 日志消息结构体，包含完整的日志信息
    /// </summary>
    public readonly struct LogMessage
    {
        /// <summary>日志级别</summary>
        public readonly LogLevel Level;

        /// <summary>日志标签</summary>
        public readonly string Tag;

        /// <summary>日志内容</summary>
        public readonly string Message;

        /// <summary>时间戳</summary>
        public readonly DateTime Timestamp;

        /// <summary>帧数（Unity环境下有效）</summary>
        public readonly int FrameCount;

        /// <summary>线程ID</summary>
        public readonly int ThreadId;

        /// <summary>堆栈跟踪信息</summary>
        public readonly string StackTrace;

        /// <summary>
        /// 构造函数 - 智能堆栈跟踪版本
        /// </summary>
        public LogMessage(LogLevel level, string tag, string message, string stackTrace = null, int frameCount = 0, TimezoneConfig timezoneConfig = null)
        {
            Level = level;
            Tag = tag ?? "DEFAULT";
            Message = message ?? string.Empty;
            Timestamp = GetConfiguredTime(timezoneConfig);
            FrameCount = frameCount > 0 ? frameCount : StackTraceHelper.GetCurrentFrameCount();
            ThreadId = StackTraceHelper.GetCurrentThreadId();
            StackTrace = stackTrace;
        }

        /// <summary>
        /// 构造函数 - 自动堆栈跟踪版本（用于手动调用）
        /// </summary>
        public LogMessage(LogLevel level, string tag, string message, LoggerConfiguration config, TimezoneConfig timezoneConfig = null)
        {
            Level = level;
            Tag = tag ?? "DEFAULT";
            Message = message ?? string.Empty;
            Timestamp = GetConfiguredTime(timezoneConfig);
            FrameCount = StackTraceHelper.GetCurrentFrameCount();
            ThreadId = StackTraceHelper.GetCurrentThreadId();

            // 智能堆栈跟踪：只在需要时获取
            if (StackTraceHelper.ShouldCaptureStackTrace(level, config))
            {
                // 跳过更多帧，因为是通过多层调用到达这里的
                StackTrace = StackTraceHelper.CaptureStackTrace(skipFrames: 3, maxDepth: config?.MaxStackTraceDepth ?? 10);
            }
            else
            {
                StackTrace = null;
            }
        }

        /// <summary>
        /// 获取配置的时间（支持传入时区配置）
        /// </summary>
        private static DateTime GetConfiguredTime(TimezoneConfig timezoneConfig = null)
        {
            // 🎯 智能时区处理：支持传入时区配置，避免循环调用

            // 如果有时区配置传入，使用它
            if (timezoneConfig != null)
            {
                try
                {
                    return timezoneConfig.GetCurrentTime();
                }
                catch
                {
                    // 配置的时区有问题，回退到UTC
                }
            }

            // 默认使用UTC时间（初始化时或配置无效时）
            return DateTime.UtcNow;
        }

        /// <summary>
        /// 创建带有系统堆栈跟踪的日志消息（用于系统错误）
        /// </summary>
        public static LogMessage CreateWithSystemStackTrace(LogLevel level, string tag, string message, string systemStackTrace)
        {
            // 格式化系统提供的堆栈跟踪
            string formattedStackTrace = StackTraceHelper.FormatSystemStackTrace(systemStackTrace);

            return new LogMessage(level, tag, message, formattedStackTrace,
                StackTraceHelper.GetCurrentFrameCount());
        }

        /// <summary>
        /// 创建带有手动堆栈跟踪的日志消息（已废弃，推荐使用带配置的构造函数）
        /// </summary>
        [Obsolete("Use constructor with LoggerConfiguration instead for better performance")]
        public static LogMessage CreateWithStackTrace(LogLevel level, string tag, string message, int skipFrames = 1)
        {
            string stackTrace = null;

            // 只在错误级别时获取堆栈跟踪（符合新的默认策略）
            if (StackTraceHelper.IsErrorLevel(level))
            {
                stackTrace = StackTraceHelper.CaptureStackTrace(skipFrames + 1, 10);
            }

            return new LogMessage(level, tag, message, stackTrace, StackTraceHelper.GetCurrentFrameCount());
        }



        /// <summary>
        /// 转换为字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}][{Level.ToShortString()}][{Tag}] {Message}";
        }
    }
}
