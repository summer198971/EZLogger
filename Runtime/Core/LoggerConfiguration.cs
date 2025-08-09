using System;
using System.Collections.Generic;

namespace EZLogger
{
    /// <summary>
    /// 时区配置类
    /// </summary>
    [Serializable]
    public class TimezoneConfig
    {
        /// <summary>是否使用UTC时间（默认true）</summary>
        public bool UseUtc = true;

        /// <summary>自定义时区ID（当UseUtc=false时使用）</summary>
        public string TimezoneId = "";

        /// <summary>时间格式化字符串</summary>
        public string TimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// 获取当前配置的时间
        /// </summary>
        public DateTime GetCurrentTime()
        {
            if (UseUtc)
            {
                return DateTime.UtcNow;
            }

            if (!string.IsNullOrEmpty(TimezoneId))
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
                }
                catch
                {
                    // 如果时区ID无效，回退到UTC
                    return DateTime.UtcNow;
                }
            }

            // 默认使用本地时间
            return DateTime.Now;
        }

        /// <summary>
        /// 格式化时间字符串
        /// </summary>
        public string FormatTime(DateTime? time = null)
        {
            var targetTime = time ?? GetCurrentTime();
            return targetTime.ToString(TimeFormat);
        }
    }

    /// <summary>
    /// 日志配置类
    /// </summary>
    [Serializable]
    public class LoggerConfiguration
    {
        /// <summary>全局启用的日志级别</summary>
        public LogLevel GlobalEnabledLevels = LogLevel.All;

        /// <summary>是否启用性能模式（零GC分配）</summary>
        public bool PerformanceMode = false;

        /// <summary>是否启用堆栈跟踪</summary>
        public bool EnableStackTrace = true;

        /// <summary>堆栈跟踪的最小级别</summary>
        public LogLevel StackTraceMinLevel = LogLevel.Warning;

        /// <summary>最大堆栈跟踪深度</summary>
        public int MaxStackTraceDepth = 10;

        /// <summary>是否启用异步写入</summary>
        public bool EnableAsyncWrite = true;

        /// <summary>日志队列最大大小</summary>
        public int MaxQueueSize = 1000;

        /// <summary>日志缓冲区大小</summary>
        public int BufferSize = 4096;

        /// <summary>文件输出配置</summary>
        public FileOutputConfig FileOutput = new FileOutputConfig();

        /// <summary>服务器输出配置</summary>
        public ServerOutputConfig ServerOutput = new ServerOutputConfig();

        /// <summary>Unity控制台输出配置</summary>
        public UnityConsoleConfig UnityConsole = new UnityConsoleConfig();

        /// <summary>扩展配置字典</summary>
        public Dictionary<string, object> ExtensionConfigs = new Dictionary<string, object>();

        /// <summary>时区配置</summary>
        public TimezoneConfig Timezone = new TimezoneConfig();

        /// <summary>
        /// 创建默认配置
        /// </summary>
        public static LoggerConfiguration CreateDefault()
        {
            return new LoggerConfiguration();
        }

        /// <summary>
        /// 创建发布版本配置（仅错误和警告）
        /// </summary>
        public static LoggerConfiguration CreateRelease()
        {
            return new LoggerConfiguration
            {
                GlobalEnabledLevels = LogLevel.ErrorAndWarning,
                PerformanceMode = true,
                EnableStackTrace = false,
                EnableAsyncWrite = true
            };
        }

        /// <summary>
        /// 创建开发版本配置（所有级别）
        /// </summary>
        public static LoggerConfiguration CreateDevelopment()
        {
            return new LoggerConfiguration
            {
                GlobalEnabledLevels = LogLevel.All,
                PerformanceMode = false,
                EnableStackTrace = true,
                EnableAsyncWrite = true
            };
        }
    }

    /// <summary>
    /// 文件输出配置
    /// </summary>
    [Serializable]
    public class FileOutputConfig
    {
        /// <summary>是否启用文件输出</summary>
        public bool Enabled = true;

        /// <summary>日志文件目录</summary>
        public string LogDirectory = "Logs";

        /// <summary>日志文件名模板</summary>
        public string FileNameTemplate = "log_{0:yyyyMMdd}.txt";

        /// <summary>最大文件大小（字节）</summary>
        public long MaxFileSize = 10 * 1024 * 1024; // 10MB

        /// <summary>文件轮转时保留的大小</summary>
        public long KeepSize = 5 * 1024 * 1024; // 5MB

        /// <summary>是否启用文件大小检查</summary>
        public bool EnableSizeCheck = true;

        /// <summary>文件大小检查间隔（秒）</summary>
        public int SizeCheckInterval = 60;

        /// <summary>是否启用文件压缩</summary>
        public bool EnableCompression = false;
    }

    /// <summary>
    /// 服务器输出配置
    /// </summary>
    [Serializable]
    public class ServerOutputConfig
    {
        /// <summary>是否启用服务器输出</summary>
        public bool Enabled = false;

        /// <summary>服务器URL</summary>
        public string ServerUrl = "";

        /// <summary>发送超时时间（毫秒）</summary>
        public int TimeoutMs = 3000;

        /// <summary>重试次数</summary>
        public int RetryCount = 3;

        /// <summary>批量发送大小</summary>
        public int BatchSize = 10;

        /// <summary>发送间隔（毫秒）</summary>
        public int SendInterval = 1000;

        /// <summary>最小发送级别</summary>
        public LogLevel MinLevel = LogLevel.Warning;

        /// <summary>是否启用压缩</summary>
        public bool EnableCompression = true;

        /// <summary>请求头</summary>
        public Dictionary<string, string> Headers = new Dictionary<string, string>();
    }

    /// <summary>
    /// Unity控制台配置
    /// </summary>
    [Serializable]
    public class UnityConsoleConfig
    {
        /// <summary>是否启用Unity控制台输出</summary>
        public bool Enabled = true;

        /// <summary>是否启用颜色</summary>
        public bool EnableColors = true;

        /// <summary>是否显示帧数</summary>
        public bool ShowFrameCount = true;

        /// <summary>是否显示线程ID</summary>
        public bool ShowThreadId = false;

        /// <summary>最小输出级别</summary>
        public LogLevel MinLevel = LogLevel.Log;
    }
}
