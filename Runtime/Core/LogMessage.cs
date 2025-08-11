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
        /// 构造函数
        /// </summary>
        public LogMessage(LogLevel level, string tag, string message, string stackTrace = null, int frameCount = 0, TimezoneConfig timezoneConfig = null)
        {
            Level = level;
            Tag = tag ?? "DEFAULT";
            Message = message ?? string.Empty;
            Timestamp = GetConfiguredTime(timezoneConfig);
            FrameCount = frameCount;
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            StackTrace = stackTrace;
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
        /// 创建带有堆栈跟踪的日志消息
        /// </summary>
        public static LogMessage CreateWithStackTrace(LogLevel level, string tag, string message, int skipFrames = 1)
        {
            string stackTrace = null;

            // 只在需要时获取堆栈跟踪
            if (level >= LogLevel.Warning)
            {
                try
                {
                    var trace = new StackTrace(skipFrames, true);
                    stackTrace = FormatStackTrace(trace);
                }
                catch
                {
                    // 忽略堆栈跟踪获取失败
                }
            }

            return new LogMessage(level, tag, message, stackTrace, GetFrameCount());
        }

        /// <summary>
        /// 格式化堆栈跟踪信息
        /// </summary>
        private static string FormatStackTrace(StackTrace stackTrace)
        {
            if (stackTrace?.GetFrames() is not { Length: > 0 } frames)
                return string.Empty;

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < frames.Length && i < 10; i++) // 限制最多10层
            {
                var frame = frames[i];
                var method = frame.GetMethod();
                var fileName = frame.GetFileName();

                if (!string.IsNullOrEmpty(fileName))
                {
                    // 简化文件路径，只保留Assets后的部分
                    string assetPath = SimplifyFilePath(fileName);
                    int line = frame.GetFileLineNumber();
                    sb.AppendLine($"{assetPath}:{line} ({method?.DeclaringType?.Name}.{method?.Name})");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 简化文件路径
        /// </summary>
        private static string SimplifyFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            // 查找Assets目录
            int assetsIndex = filePath.LastIndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return filePath.Substring(assetsIndex).Replace('\\', '/');
            }

            // 如果没有Assets目录，返回文件名
            return System.IO.Path.GetFileName(filePath);
        }

        /// <summary>
        /// 获取当前帧数（Unity环境下）
        /// </summary>
        private static int GetFrameCount()
        {
#if UNITY_2018_1_OR_NEWER
            return UnityEngine.Time.frameCount;
#else
            return 0;
#endif
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
