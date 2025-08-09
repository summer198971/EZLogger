using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EZLogger
{
    /// <summary>
    /// 条件日志记录器 - 实现真正的零开销
    /// 当级别被禁用时，返回null，避免任何性能开销
    /// </summary>
    public class ConditionalLogger
    {
        private readonly LogLevel _level;
        private readonly ILogger _logger;

        internal ConditionalLogger(LogLevel level, ILogger logger)
        {
            _level = level;
            _logger = logger;
        }

        /// <summary>
        /// 记录日志 - 只有在级别启用时才会有实际开销
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string tag, string message)
        {
            // 对Error和Exception级别应用防重复机制
            if (_level == LogLevel.Error || _level == LogLevel.Exception)
            {
                LogWithPreventDuplicate(tag, message);
            }
            else
            {
                // 其他级别直接记录
                _logger.Log(_level, tag, message);
            }
        }

        /// <summary>
        /// 带防重复机制的日志记录（包含服务器上报）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogWithPreventDuplicate(string tag, string message)
        {
            // 设置防重复标志，避免调用Unity Debug时重复记录
            SystemLogMonitor.Instance.SetPreventDuplicate(_level, true);
            try
            {
                // 通过正常的日志管道输出，由UnityAppender负责Unity控制台输出
                _logger.Log(_level, tag, message);

                // 如果是EZLoggerManager实例，且启用服务器上报，则上报零开销API的错误
                if (_logger is EZLoggerManager manager && manager.IsServerReportingEnabled)
                {
                    manager.ReportToServer(message, _level, tag);
                }
            }
            finally
            {
                SystemLogMonitor.Instance.SetPreventDuplicate(_level, false);
            }
        }

        /// <summary>
        /// 记录日志（使用对象作为标签）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(object tag, string message)
        {
            Log(tag?.ToString() ?? "NULL", message);
        }

        /// <summary>
        /// 格式化日志 - 零开销版本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(string tag, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                Log(tag, message); // 使用统一的Log方法，确保防重复机制生效
            }
            catch (FormatException)
            {
                _logger.Log(LogLevel.Error, "Logger", $"Format error: {format}");
            }
        }

        /// <summary>
        /// 带堆栈跟踪的日志记录
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string tag, string message, StackTrace stackTrace)
        {
            var logMessage = new LogMessage(_level, tag, message,
                FormatStackTrace(stackTrace), GetCurrentFrameCount());

            // 对Error和Exception级别应用防重复机制
            if (_level == LogLevel.Error || _level == LogLevel.Exception)
            {
                SystemLogMonitor.Instance.SetPreventDuplicate(_level, true);
                try
                {
                    _logger.Log(logMessage);
                }
                finally
                {
                    SystemLogMonitor.Instance.SetPreventDuplicate(_level, false);
                }
            }
            else
            {
                _logger.Log(logMessage);
            }
        }

        private string FormatStackTrace(StackTrace stackTrace)
        {
            var frames = stackTrace?.GetFrames();
            if (frames == null || frames.Length == 0)
                return null;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < frames.Length && i < 10; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();
                var fileName = frame.GetFileName();

                if (!string.IsNullOrEmpty(fileName))
                {
                    string assetPath = SimplifyFilePath(fileName);
                    int line = frame.GetFileLineNumber();
                    sb.AppendLine($"{assetPath}:{line} ({method?.DeclaringType?.Name}.{method?.Name})");
                }
            }
            return sb.ToString();
        }

        private string SimplifyFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            int assetsIndex = filePath.LastIndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return filePath.Substring(assetsIndex).Replace('\\', '/');
            }
            return System.IO.Path.GetFileName(filePath);
        }

        private int GetCurrentFrameCount()
        {
            return UnityEngine.Time.frameCount;
        }
    }

    /// <summary>
    /// 空的条件日志记录器 - 用于完全禁用的级别
    /// 编译器优化后，对这个对象的调用会被完全移除
    /// </summary>
    public sealed class NullConditionalLogger
    {
        public static readonly NullConditionalLogger Instance = new NullConditionalLogger();

        private NullConditionalLogger() { }

        /// <summary>
        /// 空操作 - 编译器会优化掉这些调用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NEVER_DEFINED")] // 这个条件永远不会被定义，确保方法被完全移除
        public void Log(string tag, string message) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NEVER_DEFINED")]
        public void Log(object tag, string message) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NEVER_DEFINED")]
        public void LogFormat(string tag, string format, params object[] args) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("NEVER_DEFINED")]
        public void Log(string tag, string message, StackTrace stackTrace) { }
    }
}


