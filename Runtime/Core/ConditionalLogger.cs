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
            // 此时级别已经确定是启用的，直接记录
            _logger.Log(_level, tag, message);
        }
        
        /// <summary>
        /// 记录日志（使用对象作为标签）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(object tag, string message)
        {
            _logger.Log(_level, tag?.ToString() ?? "NULL", message);
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
                _logger.Log(_level, tag, message);
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
            _logger.Log(logMessage);
        }
        
        private string FormatStackTrace(StackTrace stackTrace)
        {
            if (stackTrace?.GetFrames() is not { Length: > 0 } frames)
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
#if UNITY_2018_1_OR_NEWER
            return UnityEngine.Time.frameCount;
#else
            return 0;
#endif
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


