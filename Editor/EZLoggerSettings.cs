using System;
using UnityEngine;
using UnityEditor;

namespace EZLogger.Editor
{
    /// <summary>
    /// EZ Logger项目设置数据
    /// </summary>
    [CreateAssetMenu(fileName = "EZLoggerSettings", menuName = "EZLogger/Settings", order = 1)]
    public class EZLoggerSettings : ScriptableObject
    {
        [Header("基础配置")]
        [Tooltip("全局启用的日志级别")]
        public LogLevel globalEnabledLevels = LogLevel.All;

        [Tooltip("启用堆栈跟踪")]
        public bool enableStackTrace = true;

        [Tooltip("最大堆栈跟踪深度")]
        [Range(1, 50)]
        public int maxStackTraceDepth = 10;



        //[Header("异步处理")]
        [Tooltip("日志队列最大大小")]
        [Range(100, 10000)]
        public int maxQueueSize = 1000;

        [Tooltip("日志缓冲区大小")]
        [Range(1024, 65536)]
        public int bufferSize = 4096;

        [Header("Unity控制台")]
        [Tooltip("启用Unity控制台输出")]
        public bool unityConsoleEnabled = true;

        [Tooltip("启用颜色")]
        public bool unityConsoleColors = true;

        [Tooltip("显示帧数")]
        public bool unityConsoleShowFrame = true;

        [Tooltip("显示线程ID")]
        public bool unityConsoleShowThread = false;

        [Tooltip("Unity控制台最小输出级别")]
        public LogLevel unityConsoleMinLevel = LogLevel.Log;

        [Header("文件输出")]
        [Tooltip("启用文件输出")]
        public bool fileOutputEnabled = true;

        [Tooltip("日志文件目录")]
        public string logDirectory = "Logs";

        [Tooltip("日志文件名模板")]
        public string fileNameTemplate = "log_{0:yyyyMMdd}.txt";

        [Tooltip("是否按日期分割文件（默认：一天一个文件）")]
        public bool enableDailyRotation = true;

        [Tooltip("启用文件压缩")]
        public bool enableFileCompression = false;

        [Header("服务器上报")]
        [Tooltip("启用服务器上报")]
        public bool serverReportEnabled = false;

        [Tooltip("服务器URL")]
        public string serverUrl = "";

        [Tooltip("发送超时时间（毫秒）")]
        [Range(1000, 30000)]
        public int timeoutMs = 3000;

        [Tooltip("重试次数")]
        [Range(0, 10)]
        public int retryCount = 3;

        [Tooltip("批量发送大小")]
        [Range(1, 100)]
        public int batchSize = 10;

        [Tooltip("发送间隔（毫秒）")]
        [Range(100, 10000)]
        public int sendInterval = 1000;

        [Tooltip("服务器上报最小级别")]
        public LogLevel serverMinLevel = LogLevel.Warning;

        [Tooltip("启用服务器数据压缩")]
        public bool enableServerCompression = true;

        [Header("系统监控")]
        [Tooltip("启用系统日志监控")]
        public bool enableSystemLogMonitor = true;

        [Header("扩展数据")]
        [Tooltip("收集设备信息")]
        public bool collectDeviceInfo = true;

        [Tooltip("收集性能信息")]
        public bool collectPerformanceInfo = false;

        [Header("时区配置")]
        [Tooltip("是否使用UTC时间（默认true）")]
        public bool useUtcTime = true;

        [Tooltip("UTC偏移小时数（范围-12到+14，当不使用UTC时）")]
        [Range(-12, 14)]
        public int utcOffsetHours = 0;

        /// <summary>
        /// 转换为LoggerConfiguration
        /// </summary>
        public LoggerConfiguration ToLoggerConfiguration()
        {
            var config = new LoggerConfiguration
            {
                GlobalEnabledLevels = globalEnabledLevels,
                EnableStackTrace = enableStackTrace,
                StackTraceMinLevel = LogLevel.ErrorAndAbove, // 固定为Error和Exception级别
                MaxStackTraceDepth = maxStackTraceDepth,
                MaxQueueSize = maxQueueSize,
                BufferSize = bufferSize
            };

            // Unity控制台配置
            config.UnityConsole = new UnityConsoleConfig
            {
                Enabled = unityConsoleEnabled,
                EnableColors = unityConsoleColors,
                ShowThreadId = unityConsoleShowThread,
                MinLevel = unityConsoleMinLevel
            };

            // 文件输出配置
            config.FileOutput = new FileOutputConfig
            {
                Enabled = fileOutputEnabled,
                LogDirectory = logDirectory,
                FileNameTemplate = fileNameTemplate,
                EnableDailyRotation = enableDailyRotation,
                EnableCompression = enableFileCompression
            };

            // 服务器输出配置
            config.ServerOutput = new ServerOutputConfig
            {
                Enabled = serverReportEnabled,
                ServerUrl = serverUrl,
                TimeoutMs = timeoutMs,
                RetryCount = retryCount,
                BatchSize = batchSize,
                SendInterval = sendInterval,
                MinLevel = serverMinLevel,
                EnableCompression = enableServerCompression
            };

            // 时区配置
            config.Timezone.UseUtc = useUtcTime;
            config.Timezone.UtcOffsetHours = utcOffsetHours;
            config.Timezone.ClampUtcOffset(); // 确保偏移在有效范围内

            return config;
        }

        /// <summary>
        /// 从LoggerConfiguration加载
        /// </summary>
        public void FromLoggerConfiguration(LoggerConfiguration config)
        {
            globalEnabledLevels = config.GlobalEnabledLevels;
            enableStackTrace = config.EnableStackTrace;
            // stackTraceMinLevel 已移除，固定使用 ErrorAndAbove
            maxStackTraceDepth = config.MaxStackTraceDepth;
            maxQueueSize = config.MaxQueueSize;
            bufferSize = config.BufferSize;

            // Unity控制台
            if (config.UnityConsole != null)
            {
                unityConsoleEnabled = config.UnityConsole.Enabled;
                unityConsoleColors = config.UnityConsole.EnableColors;
                unityConsoleShowThread = config.UnityConsole.ShowThreadId;
                unityConsoleMinLevel = config.UnityConsole.MinLevel;
            }

            // 文件输出
            if (config.FileOutput != null)
            {
                fileOutputEnabled = config.FileOutput.Enabled;
                logDirectory = config.FileOutput.LogDirectory;
                fileNameTemplate = config.FileOutput.FileNameTemplate;
                enableDailyRotation = config.FileOutput.EnableDailyRotation;
                enableFileCompression = config.FileOutput.EnableCompression;
            }

            // 服务器输出
            if (config.ServerOutput != null)
            {
                serverReportEnabled = config.ServerOutput.Enabled;
                serverUrl = config.ServerOutput.ServerUrl;
                timeoutMs = config.ServerOutput.TimeoutMs;
                retryCount = config.ServerOutput.RetryCount;
                batchSize = config.ServerOutput.BatchSize;
                sendInterval = config.ServerOutput.SendInterval;
                serverMinLevel = config.ServerOutput.MinLevel;
                enableServerCompression = config.ServerOutput.EnableCompression;
            }

            // 时区配置
            if (config.Timezone != null)
            {
                useUtcTime = config.Timezone.UseUtc;
                utcOffsetHours = config.Timezone.UtcOffsetHours;
            }
        }

        private static EZLoggerSettings s_instance;

        /// <summary>
        /// 获取或创建设置实例
        /// </summary>
        public static EZLoggerSettings GetOrCreateSettings()
        {
            if (s_instance == null)
            {
                var settings = AssetDatabase.LoadAssetAtPath<EZLoggerSettings>(GetSettingsPath());
                if (settings == null)
                {
                    settings = CreateInstance<EZLoggerSettings>();
                    // 确保目录存在
                    var directory = System.IO.Path.GetDirectoryName(GetSettingsPath());
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    AssetDatabase.CreateAsset(settings, GetSettingsPath());
                    AssetDatabase.SaveAssets();
                }
                s_instance = settings;
            }
            return s_instance;
        }

        /// <summary>
        /// 获取设置文件路径
        /// </summary>
        public static string GetSettingsPath()
        {
            return "Assets/Settings/EZLoggerSettings.asset";
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
