using System;

namespace EZLogger.Appenders
{
    /// <summary>
    /// 日志输出器接口
    /// </summary>
    public interface ILogAppender : IDisposable
    {
        /// <summary>输出器名称</summary>
        string Name { get; }

        /// <summary>是否启用</summary>
        bool IsEnabled { get; set; }

        /// <summary>支持的日志级别</summary>
        LogLevel SupportedLevels { get; set; }

        /// <summary>是否支持异步写入（文件输出器为true，控制台输出器为false）</summary>
        bool SupportsAsyncWrite { get; }

        /// <summary>
        /// 写入日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        void WriteLog(LogMessage message);

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        void Flush();

        /// <summary>
        /// 初始化输出器
        /// </summary>
        /// <param name="config">配置对象</param>
        void Initialize(object config);
    }

    /// <summary>
    /// 抽象日志输出器基类
    /// </summary>
    public abstract class LogAppenderBase : ILogAppender
    {
        /// <summary>输出器名称</summary>
        public abstract string Name { get; }

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>支持的日志级别</summary>
        public LogLevel SupportedLevels { get; set; } = LogLevel.All;

        /// <summary>是否支持异步写入</summary>
        public abstract bool SupportsAsyncWrite { get; }

        /// <summary>是否已初始化</summary>
        protected bool IsInitialized { get; private set; }

        /// <summary>是否已释放</summary>
        protected bool IsDisposed { get; private set; }

        /// <summary>
        /// 写入日志消息
        /// </summary>
        public void WriteLog(LogMessage message)
        {
            // 检查状态
            if (IsDisposed || !IsEnabled || !IsInitialized)
                return;

            // 检查级别
            if (!SupportedLevels.Contains(message.Level))
                return;

            try
            {
                WriteLogCore(message);
            }
            catch (Exception ex)
            {
                // 输出器内部错误，避免递归
                HandleInternalError(ex);
            }
        }

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        public virtual void Flush()
        {
            if (IsDisposed || !IsInitialized)
                return;

            try
            {
                FlushCore();
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        /// <summary>
        /// 初始化输出器
        /// </summary>
        public void Initialize(object config)
        {
            if (IsInitialized)
                return;

            try
            {
                InitializeCore(config);
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
                throw;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            try
            {
                Flush();
                DisposeCore();
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
            finally
            {
                IsDisposed = true;
            }
        }

        /// <summary>
        /// 核心写入逻辑（子类实现）
        /// </summary>
        protected abstract void WriteLogCore(LogMessage message);

        /// <summary>
        /// 核心刷新逻辑（子类可重写）
        /// </summary>
        protected virtual void FlushCore() { }

        /// <summary>
        /// 核心初始化逻辑（子类可重写）
        /// </summary>
        protected virtual void InitializeCore(object config) { }

        /// <summary>
        /// 核心释放逻辑（子类可重写）
        /// </summary>
        protected virtual void DisposeCore() { }

        /// <summary>
        /// 处理内部错误
        /// </summary>
        protected virtual void HandleInternalError(Exception ex)
        {
            // 默认实现：输出到Unity控制台（如果可用）
#if UNITY_2018_1_OR_NEWER
            UnityEngine.Debug.LogError($"[EZLogger] Appender '{Name}' error: {ex.Message}");
#else
            System.Console.WriteLine($"[EZLogger] Appender '{Name}' error: {ex.Message}");
#endif
        }
    }
}
