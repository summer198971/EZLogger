using UnityEngine;
using UnityEditor;
using System.IO;

namespace EZLogger.Editor
{
    /// <summary>
    /// EZ Logger运行时初始化器
    /// 在游戏启动时自动应用项目设置中的配置
    /// </summary>
    [InitializeOnLoad]
    public static class EZLoggerInitializer
    {
        static EZLoggerInitializer()
        {
            // 编辑器启动时初始化
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // 进入播放模式时应用设置
                ApplyProjectSettings();
            }
        }

        /// <summary>
        /// 应用项目设置到运行时
        /// </summary>
        public static void ApplyProjectSettings()
        {
            try
            {
                var settings = EZLoggerSettings.GetOrCreateSettings();
                var manager = EZLoggerManager.Instance;
                var config = settings.ToLoggerConfiguration();

                // 应用主要配置
                manager.Configuration = config;
                manager.EnabledLevels = config.GlobalEnabledLevels;

                // 应用系统监控
                manager.EnableSystemLogMonitor(settings.enableSystemLogMonitor);

                // 应用服务器配置
                manager.EnableServerReporting(settings.serverReportEnabled);
                if (!string.IsNullOrEmpty(settings.serverUrl))
                {
                    manager.SetServerReportUrl(settings.serverUrl);
                }

                // 设置设备信息收集
                if (settings.collectDeviceInfo)
                {
                    manager.SetReportExtraData("configVersion", "ProjectSettings");
                    manager.SetReportExtraData("timestamp", config.Timezone.FormatTime());
                }

                Debug.Log("[EZLogger] 项目设置已应用到运行时");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EZLogger] 应用项目设置失败: {e.Message}");
            }
        }

        /// <summary>
        /// 菜单项：手动应用设置
        /// </summary>
        [MenuItem("Tools/EZ Logger/Apply Settings")]
        public static void ApplySettingsMenuItem()
        {
            if (Application.isPlaying)
            {
                ApplyProjectSettings();
                EditorUtility.DisplayDialog("设置应用", "EZ Logger设置已应用到运行时", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请在播放模式下使用此功能", "确定");
            }
        }

        /// <summary>
        /// 菜单项：打开设置
        /// </summary>
        [MenuItem("Tools/EZ Logger/Open Settings")]
        public static void OpenSettingsMenuItem()
        {
            SettingsService.OpenProjectSettings("Project/EZ Logger");
        }

        /// <summary>
        /// 菜单项：创建默认设置
        /// </summary>
        [MenuItem("Tools/EZ Logger/Create Default Settings")]
        public static void CreateDefaultSettingsMenuItem()
        {
            var settings = EZLoggerSettings.GetOrCreateSettings();
            var defaultConfig = LoggerConfiguration.CreateDefault();
            settings.FromLoggerConfiguration(defaultConfig);
            settings.Save();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            EditorUtility.DisplayDialog("创建完成",
                $"默认设置已创建: {EZLoggerSettings.GetSettingsPath()}", "确定");
        }

        /// <summary>
        /// 菜单项：运行时测试
        /// </summary>
        [MenuItem("Tools/EZ Logger/Runtime Test")]
        public static void RuntimeTestMenuItem()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("提示", "请在播放模式下测试", "确定");
                return;
            }

            // 测试各种日志级别
            EZLog.LogLog("Test", "This is a log message");
            EZLog.LogWarning("Test", "This is a warning message");
            EZLog.LogError("Test", "This is an error message");

            // 测试零开销API
            EZLog.Log?.Log("ZeroGC", "Zero GC log message");
            EZLog.Warning?.Log("ZeroGC", "Zero GC warning message");
            EZLog.Error?.Log("ZeroGC", "Zero GC error message");

            Debug.Log("[EZLogger] 运行时测试完成，请查看控制台输出");
        }

        [MenuItem("Tools/EZ Logger/Open Log Folder")]
        public static void OpenLogFolderMenuItem()
        {
            string logFolder = GetLogFolderPath();
            OpenFolderInFileManager(logFolder);
        }

        /// <summary>
        /// 获取日志文件夹路径
        /// </summary>
        private static string GetLogFolderPath()
        {
            // 首先尝试从当前配置获取日志目录
            try
            {
                var settings = EZLoggerSettings.GetOrCreateSettings();
                if (settings != null && !string.IsNullOrEmpty(settings.logDirectory))
                {
                    string configuredPath = settings.logDirectory;
                    
                    // 如果是相对路径，基于persistentDataPath
                    if (!Path.IsPathRooted(configuredPath))
                    {
                        configuredPath = Path.Combine(Application.persistentDataPath, configuredPath);
                    }
                    
                    // 确保目录存在
                    if (Directory.Exists(configuredPath))
                    {
                        return configuredPath;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[EZLogger] 获取配置的日志目录失败: {ex.Message}");
            }

            // 回退到默认路径
            var defaultLogFolder = Path.Combine(Application.persistentDataPath, "Logs");
            
            // 确保默认目录存在
            if (!Directory.Exists(defaultLogFolder))
            {
                try
                {
                    Directory.CreateDirectory(defaultLogFolder);
                    Debug.Log($"[EZLogger] 创建日志目录: {defaultLogFolder}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EZLogger] 创建日志目录失败: {ex.Message}");
                }
            }
            
            return defaultLogFolder;
        }

        /// <summary>
        /// 使用系统默认文件管理器打开文件夹（跨平台）
        /// </summary>
        private static void OpenFolderInFileManager(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("[EZLogger] 文件夹路径为空");
                return;
            }

            // 确保路径存在
            if (!Directory.Exists(folderPath))
            {
                Debug.LogError($"[EZLogger] 文件夹不存在: {folderPath}");
                return;
            }

            try
            {
#if UNITY_EDITOR_WIN
                // Windows平台：使用explorer命令
                folderPath = folderPath.Replace('/', '\\'); // 统一使用反斜杠
                System.Diagnostics.Process.Start("explorer.exe", $"\"{folderPath}\"");
                Debug.Log($"[EZLogger] 在Windows资源管理器中打开: {folderPath}");

#elif UNITY_EDITOR_OSX
                // macOS平台：使用open命令
                System.Diagnostics.Process.Start("open", $"\"{folderPath}\"");
                Debug.Log($"[EZLogger] 在macOS Finder中打开: {folderPath}");

#elif UNITY_EDITOR_LINUX
                // Linux平台：使用xdg-open命令
                System.Diagnostics.Process.Start("xdg-open", $"\"{folderPath}\"");
                Debug.Log($"[EZLogger] 在Linux文件管理器中打开: {folderPath}");

#else
                // 通用方法：直接使用路径启动（可能不适用于所有平台）
                System.Diagnostics.Process.Start(folderPath);
                Debug.Log($"[EZLogger] 使用通用方法打开文件夹: {folderPath}");
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EZLogger] 打开文件夹失败: {ex.Message}");
                
                // 备用方案：尝试直接启动路径
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = folderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                    Debug.Log($"[EZLogger] 使用备用方案打开文件夹: {folderPath}");
                }
                catch (System.Exception backupEx)
                {
                    Debug.LogError($"[EZLogger] 备用方案也失败: {backupEx.Message}");
                    
                    // 最后的信息提示
                    Debug.Log($"[EZLogger] 请手动打开日志文件夹: {folderPath}");
                    
                    // 将路径复制到剪贴板（如果可能）
                    CopyToClipboard(folderPath);
                }
            }
        }

        /// <summary>
        /// 将文本复制到剪贴板（尽力而为）
        /// </summary>
        private static void CopyToClipboard(string text)
        {
            try
            {
                EditorGUIUtility.systemCopyBuffer = text;
                Debug.Log("[EZLogger] 日志文件夹路径已复制到剪贴板");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[EZLogger] 复制到剪贴板失败: {ex.Message}");
            }
        }
    }
}
