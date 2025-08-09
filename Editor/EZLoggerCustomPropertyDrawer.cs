using UnityEngine;
using UnityEditor;

namespace EZLogger.Editor
{
    /// <summary>
    /// 自定义属性绘制器 - 用于在Inspector中展示EZ Logger设置
    /// </summary>
    [CustomPropertyDrawer(typeof(EZLoggerSettings))]
    public class EZLoggerCustomPropertyDrawer : PropertyDrawer
    {
        // 使用CustomPropertyDrawer避免复杂的SettingsProvider依赖问题
    }

    /// <summary>
    /// EZ Logger设置面板 - 作为Inspector界面显示
    /// </summary>
    [CustomEditor(typeof(EZLoggerSettings))]
    public class EZLoggerSettingsInspector : UnityEditor.Editor
    {
        private bool showBasicSettings = true;
        private bool showAsyncSettings = true;
        private bool showUnityConsoleSettings = true;
        private bool showFileOutputSettings = true;
        private bool showServerSettings = true;
        private bool showSystemSettings = true;

        public override void OnInspectorGUI()
        {
            var settings = target as EZLoggerSettings;
            if (settings == null) return;

            serializedObject.Update();

            EditorGUILayout.Space();
            
            // 标题
            EditorGUILayout.LabelField("EZ Logger Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 实现状态说明
            DrawImplementationStatus();
            EditorGUILayout.Space();
            
            // 预设配置按钮
            DrawPresetButtons(settings);
            EditorGUILayout.Space();
            
            // 配置项 - 带折叠功能
            showBasicSettings = EditorGUILayout.Foldout(showBasicSettings, "基础配置", true);
            if (showBasicSettings)
            {
                EditorGUI.indentLevel++;
                DrawBasicSettings();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            showAsyncSettings = EditorGUILayout.Foldout(showAsyncSettings, "异步处理", true);
            if (showAsyncSettings)
            {
                EditorGUI.indentLevel++;
                DrawAsyncSettings(settings);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            showUnityConsoleSettings = EditorGUILayout.Foldout(showUnityConsoleSettings, "Unity控制台", true);
            if (showUnityConsoleSettings)
            {
                EditorGUI.indentLevel++;
                DrawUnityConsoleSettings(settings);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            showFileOutputSettings = EditorGUILayout.Foldout(showFileOutputSettings, "文件输出", true);
            if (showFileOutputSettings)
            {
                EditorGUI.indentLevel++;
                DrawFileOutputSettings(settings);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            showServerSettings = EditorGUILayout.Foldout(showServerSettings, "服务器上报", true);
            if (showServerSettings)
            {
                EditorGUI.indentLevel++;
                DrawServerSettings(settings);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            
            showSystemSettings = EditorGUILayout.Foldout(showSystemSettings, "系统监控", true);
            if (showSystemSettings)
            {
                EditorGUI.indentLevel++;
                DrawSystemSettings();
                EditorGUI.indentLevel--;
            }
            
            // 应用修改
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                settings.Save();
                
                // 运行时应用配置
                ApplySettingsToRuntime(settings);
            }
        }

        private void DrawImplementationStatus()
        {
            EditorGUILayout.LabelField("📋 功能实现状态", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("✅ 已实现 (60%): 基础日志、零开销API、Unity控制台、基础文件输出、基础服务器上报、系统监控\n" +
                                   "⚠️ 待实现 (40%): 自动堆栈跟踪、文件轮转管理、完整服务器配置、性能信息收集、扩展配置", 
                                   MessageType.Info);
        }

        private void DrawPresetButtons(EZLoggerSettings settings)
        {
            EditorGUILayout.LabelField("快速配置", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("开发模式", GUILayout.Width(100)))
                {
                    ApplyDevelopmentPreset(settings);
                }
                
                if (GUILayout.Button("发布模式", GUILayout.Width(100)))
                {
                    ApplyReleasePreset(settings);
                }
                
                if (GUILayout.Button("性能模式", GUILayout.Width(100)))
                {
                    ApplyPerformancePreset(settings);
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("重置为默认", GUILayout.Width(100)))
                {
                    ApplyDefaultPreset(settings);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("globalEnabledLevels"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("performanceMode"));
            
            // 堆栈跟踪相关 - 未实现
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox("⚠️ 以下功能待实现", MessageType.Warning);
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableStackTrace"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stackTraceMinLevel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxStackTraceDepth"));
            GUI.enabled = true;
        }

        private void DrawAsyncSettings(EZLoggerSettings settings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableAsyncWrite"));
            
            if (settings.enableAsyncWrite)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxQueueSize"));
                
                // BufferSize - 未实现
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bufferSize"));
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        private void DrawUnityConsoleSettings(EZLoggerSettings settings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("unityConsoleEnabled"));
            
            if (settings.unityConsoleEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unityConsoleColors"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unityConsoleShowFrame"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unityConsoleShowThread"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unityConsoleMinLevel"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawFileOutputSettings(EZLoggerSettings settings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fileOutputEnabled"));
            
            if (settings.fileOutputEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("logDirectory"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fileNameTemplate"));
                
                // 文件大小管理 - 部分实现
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("⚠️ 以下功能部分实现", MessageType.Warning);
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxFileSizeMB"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("keepSizeMB"));
                GUI.enabled = true;
                
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableSizeCheck"));
                if (settings.enableSizeCheck)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("sizeCheckInterval"));
                    EditorGUI.indentLevel--;
                }
                
                // 文件压缩 - 未实现
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableFileCompression"));
                GUI.enabled = true;
                EditorGUI.indentLevel--;
            }
        }

        private void DrawServerSettings(EZLoggerSettings settings)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("serverReportEnabled"));
            
            if (settings.serverReportEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serverUrl"));
                
                // 基础服务器配置 - 部分实现
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("⚠️ 以下功能待实现", MessageType.Warning);
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeoutMs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("retryCount"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("batchSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sendInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("serverMinLevel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableServerCompression"));
                GUI.enabled = true;
                
                // 显示当前状态
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("当前状态:", EditorStyles.boldLabel);
                if (Application.isPlaying)
                {
                    var manager = EZLoggerManager.Instance;
                    EditorGUILayout.LabelField($"  系统监控: {(manager.IsSystemLogMonitorEnabled ? "启用" : "禁用")}");
                    EditorGUILayout.LabelField($"  服务器上报: {(manager.IsServerReportingEnabled ? "启用" : "禁用")}");
                    EditorGUILayout.LabelField($"  服务器URL: {(string.IsNullOrEmpty(manager.GetServerReportUrl()) ? "未配置" : manager.GetServerReportUrl())}");
                }
                else
                {
                    EditorGUILayout.LabelField("  (需要运行时才能显示状态)");
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSystemSettings()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableSystemLogMonitor"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("系统监控说明:\n• 自动捕获Unity内部错误和异常\n• 与自己API的错误进行防重复处理\n• 支持错误自动上报到服务器", MessageType.Info);
        }

        private void ApplyDevelopmentPreset(EZLoggerSettings settings)
        {
            var config = LoggerConfiguration.CreateDevelopment();
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = true;
            settings.serverReportEnabled = false;
            EditorUtility.SetDirty(settings);
        }

        private void ApplyReleasePreset(EZLoggerSettings settings)
        {
            var config = LoggerConfiguration.CreateRelease();
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = true;
            settings.serverReportEnabled = true;
            EditorUtility.SetDirty(settings);
        }

        private void ApplyPerformancePreset(EZLoggerSettings settings)
        {
            var config = LoggerConfiguration.CreateRelease();
            config.PerformanceMode = true;
            config.EnableAsyncWrite = true;
            config.FileOutput.Enabled = false;
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = false;
            settings.serverReportEnabled = false;
            EditorUtility.SetDirty(settings);
        }

        private void ApplyDefaultPreset(EZLoggerSettings settings)
        {
            var config = LoggerConfiguration.CreateDefault();
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = true;
            settings.serverReportEnabled = false;
            EditorUtility.SetDirty(settings);
        }

        private void ApplySettingsToRuntime(EZLoggerSettings settings)
        {
            if (Application.isPlaying)
            {
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
                    manager.SetReportExtraData("timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
        }
    }

    /// <summary>
    /// 菜单项集成
    /// </summary>
    public static class EZLoggerMenuItems
    {
        [MenuItem("Tools/EZ Logger/Open Settings")]
        public static void OpenSettings()
        {
            var settings = EZLoggerSettings.GetOrCreateSettings();
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("Tools/EZ Logger/Create Settings Asset")]
        public static void CreateSettingsAsset()
        {
            var settings = EZLoggerSettings.GetOrCreateSettings();
            var defaultConfig = LoggerConfiguration.CreateDefault();
            settings.FromLoggerConfiguration(defaultConfig);
            settings.Save();
            
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            
            EditorUtility.DisplayDialog("创建完成", 
                $"设置文件已创建: {EZLoggerSettings.GetSettingsPath()}\n\n现在可以在Inspector中编辑配置", "确定");
        }
    }
}
