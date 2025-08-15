using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace EZLogger
{
    /// <summary>
    /// 堆栈跟踪帮助类
    /// 高性能的堆栈跟踪获取和格式化工具
    /// </summary>
    public static class StackTraceHelper
    {
        /// <summary>
        /// 检查指定级别是否需要堆栈跟踪
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="config">日志配置</param>
        /// <returns>是否需要堆栈跟踪</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldCaptureStackTrace(LogLevel level, LoggerConfiguration config)
        {
            if (config == null || !config.EnableStackTrace)
                return false;

            // 检查级别是否达到最小堆栈跟踪级别
            // 使用位运算快速检查
            return (config.StackTraceMinLevel & level) == level;
        }

        /// <summary>
        /// 获取调用堆栈（手动调用时使用）
        /// 高性能实现，只在需要时创建堆栈跟踪
        /// </summary>
        /// <param name="skipFrames">跳过的帧数（默认跳过当前方法）</param>
        /// <param name="maxDepth">最大深度</param>
        /// <returns>格式化的堆栈跟踪字符串</returns>
        [MethodImpl(MethodImplOptions.NoInlining)] // 确保此方法不被内联，保证堆栈跟踪准确性
        public static string CaptureStackTrace(int skipFrames = 1, int maxDepth = 10)
        {
            try
            {
                // 跳过当前方法的帧
                var stackTrace = new StackTrace(skipFrames + 1, true);
                return FormatStackTrace(stackTrace, maxDepth);
            }
            catch (Exception)
            {
                // 堆栈跟踪获取失败时返回空字符串
                return string.Empty;
            }
        }

        /// <summary>
        /// 格式化系统提供的堆栈跟踪（系统错误时使用）
        /// </summary>
        /// <param name="systemStackTrace">系统提供的堆栈跟踪字符串</param>
        /// <returns>格式化后的堆栈跟踪</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatSystemStackTrace(string systemStackTrace)
        {
            if (string.IsNullOrEmpty(systemStackTrace))
                return string.Empty;

            // 系统堆栈跟踪通常已经格式化好了，只需要简单处理
            return CleanupStackTrace(systemStackTrace);
        }

        /// <summary>
        /// 格式化StackTrace对象为字符串
        /// </summary>
        /// <param name="stackTrace">StackTrace对象</param>
        /// <param name="maxDepth">最大深度</param>
        /// <returns>格式化的堆栈字符串</returns>
        private static string FormatStackTrace(StackTrace stackTrace, int maxDepth)
        {
            var frames = stackTrace?.GetFrames();
            if (frames == null || frames.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            int depth = Math.Min(frames.Length, maxDepth);

            for (int i = 0; i < depth; i++)
            {
                var frame = frames[i];
                var method = frame.GetMethod();

                // 跳过没有方法信息的帧
                if (method == null)
                    continue;

                var fileName = frame.GetFileName();
                var lineNumber = frame.GetFileLineNumber();

                // 构建方法信息
                string methodInfo = $"{method.DeclaringType?.Name}.{method.Name}";

                if (!string.IsNullOrEmpty(fileName))
                {
                    // 简化文件路径
                    string simplifiedPath = SimplifyFilePath(fileName);
                    sb.AppendLine($"  at {methodInfo} in {simplifiedPath}:{lineNumber}");
                }
                else
                {
                    sb.AppendLine($"  at {methodInfo}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 简化文件路径，只保留Assets之后的部分
        /// </summary>
        /// <param name="filePath">完整文件路径</param>
        /// <returns>简化后的路径</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string SimplifyFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return string.Empty;

            // 查找Assets目录的索引
            int assetsIndex = filePath.LastIndexOf("Assets", StringComparison.OrdinalIgnoreCase);
            if (assetsIndex >= 0)
            {
                return filePath.Substring(assetsIndex).Replace('\\', '/');
            }

            // 查找Scripts目录（对于一些特殊情况）
            int scriptsIndex = filePath.LastIndexOf("Scripts", StringComparison.OrdinalIgnoreCase);
            if (scriptsIndex >= 0)
            {
                return filePath.Substring(scriptsIndex).Replace('\\', '/');
            }

            // 如果都找不到，返回文件名
            return System.IO.Path.GetFileName(filePath);
        }

        /// <summary>
        /// 清理堆栈跟踪字符串，移除不必要的信息
        /// </summary>
        /// <param name="stackTrace">原始堆栈跟踪</param>
        /// <returns>清理后的堆栈跟踪</returns>
        private static string CleanupStackTrace(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace))
                return string.Empty;

            // 这里可以添加更多的清理逻辑
            // 比如移除不重要的Unity内部调用等
            return stackTrace.Trim();
        }

        /// <summary>
        /// 快速检查是否为错误级别（Error或Exception）
        /// 用于性能优化，避免不必要的堆栈跟踪计算
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <returns>是否为错误级别</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsErrorLevel(LogLevel level)
        {
            return level == LogLevel.Error || level == LogLevel.Exception;
        }

        /// <summary>
        /// 获取当前线程ID的缓存版本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentThreadId()
        {
            return System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// 获取当前帧数的缓存版本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCurrentFrameCount()
        {
#if UNITY_2018_1_OR_NEWER
            return UnityEngine.Time.frameCount;
#else
            return 0;
#endif
        }
    }
}
