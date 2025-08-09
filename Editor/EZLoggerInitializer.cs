using UnityEngine;
using UnityEditor;

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
                    manager.SetReportExtraData("timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
    }
}
