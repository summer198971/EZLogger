using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EZLogger
{
    /// <summary>
    /// 条件日志记录器基类 - 实现真正的零开销
    /// 当级别被禁用时，返回null，避免任何性能开销
    /// </summary>
    public class ConditionalLogger
    {
        protected readonly LogLevel _level;
        protected readonly ILogger _logger;

        internal ConditionalLogger(LogLevel level, ILogger logger)
        {
            _level = level;
            _logger = logger;
        }

        /// <summary>
        /// 记录日志 - 只有在级别启用时才会有实际开销
        /// 支持智能堆栈跟踪：只在配置的级别才获取堆栈
        /// 可以被子类重写以实现特殊处理逻辑
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Log(string tag, string message)
        {
            // 基础实现：使用新的智能堆栈跟踪
            _logger.Log(_level, tag, message);
        }

        /// <summary>
        /// 记录日志消息对象 - 可以被子类重写
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Log(LogMessage logMessage)
        {
            _logger.Log(logMessage);
        }

        /// <summary>
        /// 记录日志（使用对象作为标签）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(object tag, string message)
        {
            if (tag == null)
            {
                Log("NULL", message);
            }
            else
            {
                Log(tag.GetType().Name, message);
            }
        }

        /// <summary>
        /// 格式化日志 - 保留兼容性，但推荐使用字符串插值
        /// 推荐：EZLog.Log?.Log("tag", $"Value: {value}") 
        /// 而非：EZLog.Log?.LogFormat("tag", "Value: {0}", value)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogFormat(string tag, string format, params object[] args)
        {
            try
            {
                string message = string.Format(format, args);
                Log(tag, message); // 使用统一的Log方法，确保子类处理逻辑生效
            }
            catch (FormatException)
            {
                _logger.Log(LogLevel.Error, "Logger", $"Format error: {format}");
            }
        }

        /// <summary>
        /// 带堆栈跟踪的日志记录（高性能版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(string tag, string message, StackTrace stackTrace)
        {
            // 使用StackTraceHelper格式化堆栈跟踪
            string formattedStackTrace = null;
            if (stackTrace != null)
            {
                try
                {
                    // 这里调用内部方法会有性能开销，但只在真正需要时调用
                    formattedStackTrace = FormatStackTraceInternal(stackTrace);
                }
                catch
                {
                    // 如果格式化失败，忽略堆栈跟踪
                    formattedStackTrace = null;
                }
            }

            var logMessage = new LogMessage(_level, tag, message, formattedStackTrace,
                StackTraceHelper.GetCurrentFrameCount());

            // 使用虚方法，让子类决定如何处理
            Log(logMessage);
        }

        /// <summary>
        /// 内部堆栈跟踪格式化方法 - 使用StackTraceHelper的逻辑
        /// </summary>
        private string FormatStackTraceInternal(StackTrace stackTrace)
        {
            // 将堆栈跟踪转换为字符串，然后使用StackTraceHelper格式化
            string stackTraceStr = stackTrace?.ToString();
            return StackTraceHelper.FormatSystemStackTrace(stackTraceStr);
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

    /// <summary>
    /// 专用于Error和Exception级别的条件日志记录器
    /// 实现防重复机制和服务器上报功能
    /// </summary>
    internal sealed class CriticalConditionalLogger : ConditionalLogger
    {
        internal CriticalConditionalLogger(LogLevel level, ILogger logger) : base(level, logger)
        {
        }

        /// <summary>
        /// 重写Log方法，添加防重复机制和服务器上报
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Log(string tag, string message)
        {
            LogWithCriticalHandling(tag, message);
        }

        /// <summary>
        /// 重写LogMessage方法，处理带堆栈跟踪的日志
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Log(LogMessage logMessage)
        {
            ExecuteWithPreventDuplicate(() => _logger.Log(logMessage));
        }

        /// <summary>
        /// 带防重复机制和服务器上报的关键日志记录
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogWithCriticalHandling(string tag, string message)
        {
            ExecuteWithPreventDuplicate(() =>
            {
                // 通过正常的日志管道输出，由UnityAppender负责Unity控制台输出
                _logger.Log(_level, tag, message);

                // 服务器上报（如果启用）
                ReportToServerIfEnabled(message, tag);
            });
        }

        /// <summary>
        /// 执行带防重复标志的操作
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteWithPreventDuplicate(System.Action action)
        {
            // 设置防重复标志，避免调用Unity Debug时重复记录
            SystemLogMonitor.Instance.SetPreventDuplicate(_level, true);
            try
            {
                action();
            }
            finally
            {
                SystemLogMonitor.Instance.SetPreventDuplicate(_level, false);
            }
        }

        /// <summary>
        /// 如果启用服务器上报，则上报错误到服务器
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReportToServerIfEnabled(string message, string tag)
        {
            if (_logger is EZLoggerManager manager && manager.IsServerReportingEnabled)
            {
                manager.ReportToServer(message, _level, tag);
            }
        }
    }
}


