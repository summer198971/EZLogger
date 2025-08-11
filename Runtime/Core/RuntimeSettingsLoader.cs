using System;
using System.IO;
using UnityEngine;

namespace EZLogger
{
    /// <summary>
    /// 运行时设置加载器 - 负责在运行时环境中加载EZ Logger配置
    /// 支持多种配置源：Resources、StreamingAssets、默认配置
    /// </summary>
    public static class RuntimeSettingsLoader
    {
        private const string SETTINGS_RESOURCE_PATH = "EZLoggerSettings";
        private const string SETTINGS_STREAMING_PATH = "EZLoggerSettings.json";

        /// <summary>
        /// 加载运行时配置
        /// 优先级：Resources > StreamingAssets > 默认配置
        /// </summary>
        public static LoggerConfiguration LoadConfiguration()
        {
            LoggerConfiguration config = null;

            // 1. 尝试从Resources加载（主要用于开发和测试）
            config = LoadFromResources();
            if (config != null)
            {
                Debug.Log("[EZLogger] 从Resources加载配置成功");
                return config;
            }

            // 2. 尝试从StreamingAssets加载（用于发布版本）
            config = LoadFromStreamingAssets();
            if (config != null)
            {
                Debug.Log("[EZLogger] 从StreamingAssets加载配置成功");
                return config;
            }

            // 3. 使用默认配置
            Debug.Log("[EZLogger] 使用默认配置");
            return CreateRuntimeDefaultConfiguration();
        }

        /// <summary>
        /// 从Resources文件夹加载配置
        /// </summary>
        private static LoggerConfiguration LoadFromResources()
        {
            try
            {
                var textAsset = Resources.Load<TextAsset>(SETTINGS_RESOURCE_PATH);
                if (textAsset != null)
                {
                    return DeserializeConfiguration(textAsset.text);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EZLogger] 从Resources加载配置失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 从StreamingAssets文件夹加载配置
        /// </summary>
        private static LoggerConfiguration LoadFromStreamingAssets()
        {
            try
            {
                string filePath = Path.Combine(Application.streamingAssetsPath, SETTINGS_STREAMING_PATH);

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return DeserializeConfiguration(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[EZLogger] 从StreamingAssets加载配置失败: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 反序列化配置JSON
        /// </summary>
        private static LoggerConfiguration DeserializeConfiguration(string json)
        {
            try
            {
                return UnityEngine.JsonUtility.FromJson<SerializableLoggerConfiguration>(json).ToLoggerConfiguration();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EZLogger] 配置反序列化失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建运行时默认配置（根据构建类型优化）
        /// </summary>
        private static LoggerConfiguration CreateRuntimeDefaultConfiguration()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            // 开发版本：启用所有功能
            return LoggerConfiguration.CreateDevelopment();
#else
            // 发布版本：性能优化配置
            return new LoggerConfiguration
            {
                GlobalEnabledLevels = LogLevel.WarningAndAbove, // 只保留警告和错误
                PerformanceMode = true,
                EnableStackTrace = false,
                EnableAsyncWrite = true,
                UnityConsole = new UnityConsoleConfig
                {
                    Enabled = true,
                    EnableColors = false,
                    ShowThreadId = false,
                    MinLevel = LogLevel.Warning
                },
                FileOutput = new FileOutputConfig
                {
                    Enabled = false // 发布版本默认不写文件
                },
                ServerOutput = new ServerOutputConfig
                {
                    Enabled = false // 发布版本默认不上报服务器
                }
            };
#endif
        }

        /// <summary>
        /// 序列化配置到JSON（供Editor使用）
        /// </summary>
        public static string SerializeConfiguration(LoggerConfiguration config)
        {
            try
            {
                var serializable = SerializableLoggerConfiguration.FromLoggerConfiguration(config);
                return UnityEngine.JsonUtility.ToJson(serializable, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EZLogger] 配置序列化失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存配置到StreamingAssets（供Editor使用）
        /// </summary>
        public static bool SaveConfigurationToStreamingAssets(LoggerConfiguration config)
        {
            try
            {
                string streamingAssetsPath = Application.streamingAssetsPath;
                if (!Directory.Exists(streamingAssetsPath))
                {
                    Directory.CreateDirectory(streamingAssetsPath);
                }

                string filePath = Path.Combine(streamingAssetsPath, SETTINGS_STREAMING_PATH);
                string json = SerializeConfiguration(config);

                if (!string.IsNullOrEmpty(json))
                {
                    File.WriteAllText(filePath, json);
                    Debug.Log($"[EZLogger] 配置已保存到: {filePath}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EZLogger] 保存配置到StreamingAssets失败: {ex.Message}");
            }

            return false;
        }
    }

    /// <summary>
    /// 可序列化的日志配置类（用于JSON序列化）
    /// </summary>
    [Serializable]
    public class SerializableLoggerConfiguration
    {
        public LogLevel globalEnabledLevels = LogLevel.All;
        public bool performanceMode = false;
        public bool enableStackTrace = true;
        public LogLevel stackTraceMinLevel = LogLevel.Warning;
        public int maxStackTraceDepth = 10;
        public bool enableAsyncWrite = true;
        public int maxQueueSize = 1000;
        public int bufferSize = 4096;

        // Unity控制台配置
        public bool unityConsoleEnabled = true;
        public bool unityConsoleColors = true;
        public bool unityConsoleShowThread = false;
        public LogLevel unityConsoleMinLevel = LogLevel.Log;

        // 文件输出配置
        public bool fileOutputEnabled = true;
        public string logDirectory = "Logs";
        public string fileNameTemplate = "log_{0:yyyyMMdd}.txt";
        public float maxFileSizeMB = 10f;
        public float keepSizeMB = 5f;
        public bool enableSizeCheck = true;
        public int sizeCheckInterval = 60;
        public bool enableFileCompression = false;

        // 服务器上报配置
        public bool serverReportEnabled = false;
        public string serverUrl = "";
        public int timeoutMs = 3000;
        public int retryCount = 3;
        public int batchSize = 10;
        public int sendInterval = 1000;
        public LogLevel serverMinLevel = LogLevel.Warning;
        public bool enableServerCompression = true;

        // 系统监控
        public bool enableSystemLogMonitor = true;

        // 扩展数据
        public bool collectDeviceInfo = true;
        public bool collectPerformanceInfo = false;

        // 时区配置
        public bool useUtcTime = true;
        public int utcOffsetHours = 0;

        /// <summary>
        /// 转换为LoggerConfiguration
        /// </summary>
        public LoggerConfiguration ToLoggerConfiguration()
        {
            var config = new LoggerConfiguration
            {
                GlobalEnabledLevels = globalEnabledLevels,
                PerformanceMode = performanceMode,
                EnableStackTrace = enableStackTrace,
                StackTraceMinLevel = stackTraceMinLevel,
                MaxStackTraceDepth = maxStackTraceDepth,
                EnableAsyncWrite = enableAsyncWrite,
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
                MaxFileSize = (long)(maxFileSizeMB * 1024 * 1024),
                KeepSize = (long)(keepSizeMB * 1024 * 1024),
                EnableSizeCheck = enableSizeCheck,
                SizeCheckInterval = sizeCheckInterval,
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
            config.Timezone.ClampUtcOffset();

            return config;
        }

        /// <summary>
        /// 从LoggerConfiguration创建
        /// </summary>
        public static SerializableLoggerConfiguration FromLoggerConfiguration(LoggerConfiguration config)
        {
            var serializable = new SerializableLoggerConfiguration
            {
                globalEnabledLevels = config.GlobalEnabledLevels,
                performanceMode = config.PerformanceMode,
                enableStackTrace = config.EnableStackTrace,
                stackTraceMinLevel = config.StackTraceMinLevel,
                maxStackTraceDepth = config.MaxStackTraceDepth,
                enableAsyncWrite = config.EnableAsyncWrite,
                maxQueueSize = config.MaxQueueSize,
                bufferSize = config.BufferSize
            };

            // Unity控制台
            if (config.UnityConsole != null)
            {
                serializable.unityConsoleEnabled = config.UnityConsole.Enabled;
                serializable.unityConsoleColors = config.UnityConsole.EnableColors;
                serializable.unityConsoleShowThread = config.UnityConsole.ShowThreadId;
                serializable.unityConsoleMinLevel = config.UnityConsole.MinLevel;
            }

            // 文件输出
            if (config.FileOutput != null)
            {
                serializable.fileOutputEnabled = config.FileOutput.Enabled;
                serializable.logDirectory = config.FileOutput.LogDirectory;
                serializable.fileNameTemplate = config.FileOutput.FileNameTemplate;
                serializable.maxFileSizeMB = config.FileOutput.MaxFileSize / (1024f * 1024f);
                serializable.keepSizeMB = config.FileOutput.KeepSize / (1024f * 1024f);
                serializable.enableSizeCheck = config.FileOutput.EnableSizeCheck;
                serializable.sizeCheckInterval = config.FileOutput.SizeCheckInterval;
                serializable.enableFileCompression = config.FileOutput.EnableCompression;
            }

            // 服务器输出
            if (config.ServerOutput != null)
            {
                serializable.serverReportEnabled = config.ServerOutput.Enabled;
                serializable.serverUrl = config.ServerOutput.ServerUrl;
                serializable.timeoutMs = config.ServerOutput.TimeoutMs;
                serializable.retryCount = config.ServerOutput.RetryCount;
                serializable.batchSize = config.ServerOutput.BatchSize;
                serializable.sendInterval = config.ServerOutput.SendInterval;
                serializable.serverMinLevel = config.ServerOutput.MinLevel;
                serializable.enableServerCompression = config.ServerOutput.EnableCompression;
            }

            // 时区配置
            if (config.Timezone != null)
            {
                serializable.useUtcTime = config.Timezone.UseUtc;
                serializable.utcOffsetHours = config.Timezone.UtcOffsetHours;
            }

            return serializable;
        }
    }
}
