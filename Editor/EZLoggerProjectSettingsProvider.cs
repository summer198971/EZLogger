using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EZLogger.Editor
{
    /// <summary>
    /// EZ Logger项目设置提供程序
    /// 在Unity Project Settings中显示EZ Logger配置界面
    /// </summary>
    public class EZLoggerProjectSettingsProvider : SettingsProvider
    {
        private EZLoggerSettings settings;
        private SerializedObject serializedSettings;

        // 折叠状态
        private bool showBasicSettings = true;
        private bool showAsyncSettings = true;
        private bool showUnityConsoleSettings = true;
        private bool showFileOutputSettings = true;
        private bool showServerSettings = true;
        private bool showSystemSettings = true;

        public EZLoggerProjectSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            settings = EZLoggerSettings.GetOrCreateSettings();
            serializedSettings = new SerializedObject(settings);
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.Space();

            // 标题
            var headerStyle = new GUIStyle(EditorStyles.largeLabel);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.LabelField("EZ Logger Configuration", headerStyle);
            EditorGUILayout.Space();

            // 实现状态说明
            DrawImplementationStatus();
            EditorGUILayout.Space();

            // 预设配置按钮
            DrawPresetButtons();
            EditorGUILayout.Space();

            // 配置项 - 带折叠功能
            showBasicSettings = DrawFoldoutSection("基础配置", showBasicSettings, DrawBasicSettings);
            EditorGUILayout.Space();

            showAsyncSettings = DrawFoldoutSection("异步处理", showAsyncSettings, DrawAsyncSettings);
            EditorGUILayout.Space();

            showUnityConsoleSettings = DrawFoldoutSection("Unity控制台", showUnityConsoleSettings, DrawUnityConsoleSettings);
            EditorGUILayout.Space();

            showFileOutputSettings = DrawFoldoutSection("文件输出", showFileOutputSettings, DrawFileOutputSettings);
            EditorGUILayout.Space();

            showServerSettings = DrawFoldoutSection("服务器上报", showServerSettings, DrawServerSettings);
            EditorGUILayout.Space();

            showSystemSettings = DrawFoldoutSection("系统监控", showSystemSettings, DrawSystemSettings);
            EditorGUILayout.Space();
            
            DrawFoldoutSection("时区配置", true, DrawTimezoneSettings);
            EditorGUILayout.Space();

            // 应用修改
            if (serializedSettings.hasModifiedProperties)
            {
                serializedSettings.ApplyModifiedProperties();
                settings.Save();

                // 运行时应用配置
                ApplySettingsToRuntime();
            }
        }

        private void DrawImplementationStatus()
        {
            EditorGUILayout.LabelField("📋 功能实现状态", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("✅ 已实现 (60%): 基础日志、零开销API、Unity控制台、基础文件输出、基础服务器上报、系统监控\n" +
                                   "⚠️ 待实现 (40%): 自动堆栈跟踪、文件轮转管理、完整服务器配置、性能信息收集、扩展配置",
                                   MessageType.Info);
        }

        private bool DrawFoldoutSection(string title, bool isExpanded, System.Action drawContent)
        {
            EditorGUILayout.Space(5);

            // 创建带样式的折叠标题
            var foldoutStyle = new GUIStyle(EditorStyles.foldout);
            foldoutStyle.fontSize = 13;
            foldoutStyle.fontStyle = FontStyle.Bold;

            bool newExpanded = EditorGUILayout.Foldout(isExpanded, title, true, foldoutStyle);

            if (newExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(3);
                drawContent?.Invoke();
                EditorGUI.indentLevel--;
            }

            return newExpanded;
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("快速配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("开发模式", GUILayout.Width(100)))
                {
                    ApplyDevelopmentPreset();
                }

                if (GUILayout.Button("发布模式", GUILayout.Width(100)))
                {
                    ApplyReleasePreset();
                }

                if (GUILayout.Button("性能模式", GUILayout.Width(100)))
                {
                    ApplyPerformancePreset();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("重置为默认", GUILayout.Width(100)))
                {
                    ApplyDefaultPreset();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("globalEnabledLevels"), new GUIContent("全局启用级别"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("performanceMode"), new GUIContent("性能模式"));

            // 堆栈跟踪相关 - 未实现
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox("⚠️ 以下功能待实现", MessageType.Warning);
            var oldEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableStackTrace"), new GUIContent("启用堆栈跟踪 (未实现)"));

            if (settings.enableStackTrace)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("stackTraceMinLevel"), new GUIContent("堆栈跟踪最小级别 (未实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxStackTraceDepth"), new GUIContent("最大堆栈深度 (未实现)"));
                EditorGUI.indentLevel--;
            }
            GUI.enabled = oldEnabled;
        }

        private void DrawAsyncSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableAsyncWrite"), new GUIContent("启用异步写入"));

            if (settings.enableAsyncWrite)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxQueueSize"), new GUIContent("队列最大大小"));

                // BufferSize - 未实现
                var oldEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("bufferSize"), new GUIContent("缓冲区大小 (未实现)"));
                GUI.enabled = oldEnabled;
                EditorGUI.indentLevel--;
            }
        }

        private void DrawUnityConsoleSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("unityConsoleEnabled"), new GUIContent("启用Unity控制台"));

            if (settings.unityConsoleEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("unityConsoleColors"), new GUIContent("启用颜色"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("unityConsoleShowFrame"), new GUIContent("显示帧数"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("unityConsoleShowThread"), new GUIContent("显示线程ID"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("unityConsoleMinLevel"), new GUIContent("最小输出级别"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawFileOutputSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("fileOutputEnabled"), new GUIContent("启用文件输出"));

            if (settings.fileOutputEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("logDirectory"), new GUIContent("日志目录"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("fileNameTemplate"), new GUIContent("文件名模板"));

                // 文件大小管理 - 部分实现
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("⚠️ 以下功能部分实现", MessageType.Warning);
                var oldEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxFileSizeMB"), new GUIContent("最大文件大小(MB) (部分实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("keepSizeMB"), new GUIContent("保留大小(MB) (未实现)"));
                GUI.enabled = oldEnabled;

                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableSizeCheck"), new GUIContent("启用大小检查"));

                if (settings.enableSizeCheck)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("sizeCheckInterval"), new GUIContent("检查间隔(秒)"));
                    EditorGUI.indentLevel--;
                }

                // 文件压缩 - 未实现
                oldEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableFileCompression"), new GUIContent("启用文件压缩 (未实现)"));
                GUI.enabled = oldEnabled;
                EditorGUI.indentLevel--;
            }
        }

        private void DrawServerSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("serverReportEnabled"), new GUIContent("启用服务器上报"));

            if (settings.serverReportEnabled)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("serverUrl"), new GUIContent("服务器URL"));

                // 基础服务器配置 - 部分实现
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("⚠️ 以下功能待实现", MessageType.Warning);
                var oldEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("timeoutMs"), new GUIContent("超时时间(毫秒) (未实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("retryCount"), new GUIContent("重试次数 (未实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("batchSize"), new GUIContent("批量大小 (未实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("sendInterval"), new GUIContent("发送间隔(毫秒) (未实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("serverMinLevel"), new GUIContent("最小上报级别 (未实现)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableServerCompression"), new GUIContent("启用数据压缩 (已内置GZip)"));
                GUI.enabled = oldEnabled;

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
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableSystemLogMonitor"), new GUIContent("启用系统日志监控"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("系统监控说明:\n• 自动捕获Unity内部错误和异常\n• 与自己API的错误进行防重复处理\n• 支持错误自动上报到服务器", MessageType.Info);
        }

        private void DrawTimezoneSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("useUtcTime"), new GUIContent("使用UTC时间"));
            
            if (!settings.useUtcTime)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("customTimezoneId"), new GUIContent("自定义时区ID"));
                EditorGUILayout.HelpBox("时区ID示例:\n• China Standard Time\n• UTC\n• Eastern Standard Time\n• Pacific Standard Time", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("timeFormat"), new GUIContent("时间格式"));
            
            EditorGUILayout.Space(5);
            
            // 显示当前时间示例
            EditorGUILayout.LabelField("时间示例:", EditorStyles.boldLabel);
            
            try
            {
                var timezone = new TimezoneConfig
                {
                    UseUtc = settings.useUtcTime,
                    TimezoneId = settings.customTimezoneId,
                    TimeFormat = settings.timeFormat
                };
                
                string currentTime = timezone.FormatTime();
                EditorGUILayout.LabelField($"  当前时间: {currentTime}");
                EditorGUILayout.LabelField($"  时区: {(settings.useUtcTime ? "UTC" : (string.IsNullOrEmpty(settings.customTimezoneId) ? "本地时间" : settings.customTimezoneId))}");
            }
            catch (System.Exception ex)
            {
                EditorGUILayout.HelpBox($"时区配置错误: {ex.Message}", MessageType.Warning);
            }
        }

        private void ApplyDevelopmentPreset()
        {
            var config = LoggerConfiguration.CreateDevelopment();
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = true;
            settings.serverReportEnabled = false;
            serializedSettings.Update();
        }

        private void ApplyReleasePreset()
        {
            var config = LoggerConfiguration.CreateRelease();
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = true;
            settings.serverReportEnabled = true;
            serializedSettings.Update();
        }

        private void ApplyPerformancePreset()
        {
            var config = LoggerConfiguration.CreateRelease();
            config.PerformanceMode = true;
            config.EnableAsyncWrite = true;
            config.FileOutput.Enabled = false;
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = false;
            settings.serverReportEnabled = false;
            serializedSettings.Update();
        }

        private void ApplyDefaultPreset()
        {
            var config = LoggerConfiguration.CreateDefault();
            settings.FromLoggerConfiguration(config);
            settings.enableSystemLogMonitor = true;
            settings.serverReportEnabled = false;
            serializedSettings.Update();
        }

        private void ApplySettingsToRuntime()
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
                    manager.SetReportExtraData("timestamp", config.Timezone.FormatTime());
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateEZLoggerSettingsProvider()
        {
            var provider = new EZLoggerProjectSettingsProvider("Project/EZ Logger", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<EZLoggerSettings>()
            };
            return provider;
        }
    }
}
