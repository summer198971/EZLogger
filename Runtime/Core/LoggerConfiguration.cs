using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EZLogger
{
    /// <summary>
    /// 时区配置类
    /// </summary>
    [Serializable]
    public class TimezoneConfig
    {
        /// <summary>是否使用UTC时间（默认true）</summary>
        public bool UseUtc = false;

        /// <summary>UTC偏移小时数（当UseUtc=false时使用，范围-12到+14）</summary>
        public int UtcOffsetHours = 0;

        /// <summary>默认时间格式</summary>
        private const string DEFAULT_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

        /// <summary>
        /// 获取当前配置的时间
        /// </summary>
        public DateTime GetCurrentTime()
        {
            if (UseUtc)
            {
                return DateTime.UtcNow;
            }

            // 使用UTC偏移小时数计算时间
            var utcTime = DateTime.UtcNow;
            return utcTime.AddHours(UtcOffsetHours);
        }

        /// <summary>
        /// 格式化时间字符串（使用默认格式）
        /// </summary>
        public string FormatTime(DateTime? time = null)
        {
            var targetTime = time ?? GetCurrentTime();
            return targetTime.ToString(DEFAULT_TIME_FORMAT);
        }

        /// <summary>
        /// 获取时区显示名称
        /// </summary>
        public string GetTimezoneDisplayName()
        {
            if (UseUtc)
            {
                return "UTC";
            }

            var offsetSign = UtcOffsetHours >= 0 ? "+" : "";
            return $"UTC{offsetSign}{UtcOffsetHours}";
        }

        /// <summary>
        /// 验证UTC偏移小时数是否有效
        /// </summary>
        public bool IsValidUtcOffset()
        {
            return UtcOffsetHours >= -12 && UtcOffsetHours <= 14;
        }

        /// <summary>
        /// 确保UTC偏移小时数在有效范围内
        /// </summary>
        public void ClampUtcOffset()
        {
            if (UtcOffsetHours < -12) UtcOffsetHours = -12;
            if (UtcOffsetHours > 14) UtcOffsetHours = 14;
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

        /// <summary>是否启用堆栈跟踪</summary>
        public bool EnableStackTrace = true;

        /// <summary>堆栈跟踪的最小级别</summary>
        public LogLevel StackTraceMinLevel = LogLevel.Warning;

        /// <summary>最大堆栈跟踪深度</summary>
        public int MaxStackTraceDepth = 10;



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

        public string GetLogFolderPath()
        {
            return $"{Application.persistentDataPath}/{FileOutput.LogDirectory}/";
        }

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
                EnableStackTrace = false
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
                EnableStackTrace = true
            };
        }


        public static string GetLogFilePath(FileOutputConfig fileOutput)
        {
            return Path.Combine(Application.persistentDataPath + fileOutput.LogDirectory, "EZLogger.log");
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

        /// <summary>是否按日期分割文件（默认：一天一个文件）</summary>
        public bool EnableDailyRotation = true;

        /// <summary>是否启用文件压缩</summary>
        public bool EnableCompression = false;

        /// <summary>是否显示线程ID</summary>
        public bool ShowThreadId = false;

        /// <summary>日志条目开始标记（便于解析和区分多行日志）</summary>
        public string LogEntryPrefix = "[!@#]";
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

        /// <summary>是否显示线程ID</summary>
        public bool ShowThreadId = false;

        /// <summary>最小输出级别</summary>
        public LogLevel MinLevel = LogLevel.Log;
    }
}
