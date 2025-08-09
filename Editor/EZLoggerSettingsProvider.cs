using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EZLogger.Editor
{
    /// <summary>
    /// EZ Logger项目设置提供程序
    /// 在Unity Project Settings中显示EZ Logger配置界面
    /// </summary>
    public class EZLoggerSettingsProvider : SettingsProvider
    {
        private EZLoggerSettings settings;
        private SerializedObject serializedSettings;
        
        // 样式
        private static GUIStyle headerStyle;
        private static GUIStyle foldoutStyle;
        private bool showBasicSettings = true;
        private bool showAsyncSettings = true;
        private bool showUnityConsoleSettings = true;
        private bool showFileOutputSettings = true;
        private bool showServerSettings = true;
        private bool showSystemSettings = true;
        private bool showExtensionSettings = true;

        public EZLoggerSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            settings = EZLoggerSettings.GetOrCreateSettings();
            serializedSettings = new SerializedObject(settings);
        }

        public override void OnGUI(string searchContext)
        {
            InitializeStyles();
            
            EditorGUILayout.Space();
            
            // 标题
            EditorGUILayout.LabelField("EZ Logger Configuration", headerStyle);
            EditorGUILayout.Space();
            
            // 预设配置按钮
            DrawPresetButtons();
            EditorGUILayout.Space();
            
            // 滚动视图
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(Vector2.zero))
            {
                // 基础配置
                showBasicSettings = DrawFoldoutSection("基础配置", showBasicSettings, DrawBasicSettings);
                
                // 异步处理
                showAsyncSettings = DrawFoldoutSection("异步处理", showAsyncSettings, DrawAsyncSettings);
                
                // Unity控制台
                showUnityConsoleSettings = DrawFoldoutSection("Unity控制台", showUnityConsoleSettings, DrawUnityConsoleSettings);
                
                // 文件输出
                showFileOutputSettings = DrawFoldoutSection("文件输出", showFileOutputSettings, DrawFileOutputSettings);
                
                // 服务器上报
                showServerSettings = DrawFoldoutSection("服务器上报", showServerSettings, DrawServerSettings);
                
                // 系统监控
                showSystemSettings = DrawFoldoutSection("系统监控", showSystemSettings, DrawSystemSettings);
                
                // 扩展设置
                showExtensionSettings = DrawFoldoutSection("扩展设置", showExtensionSettings, DrawExtensionSettings);
            }
            
            EditorGUILayout.Space();
            
            // 底部按钮
            DrawBottomButtons();
            
            // 应用修改
            if (serializedSettings.hasModifiedProperties)
            {
                serializedSettings.ApplyModifiedProperties();
                settings.Save();
                
                // 运行时应用配置
                ApplySettingsToRuntime();
            }
        }

        private bool DrawFoldoutSection(string title, bool isExpanded, System.Action drawContent)
        {
            EditorGUILayout.Space(5);
            
            var rect = EditorGUILayout.GetControlRect(false, 25);
            rect.x += 12;
            rect.width -= 12;
            
            bool newExpanded = EditorGUI.Foldout(rect, isExpanded, title, true, foldoutStyle);
            
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
            
            using (new EditorGUILayout.HorizontalScope())
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
        }

        private void DrawBasicSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("globalEnabledLevels"), new GUIContent("全局启用级别"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("performanceMode"), new GUIContent("性能模式"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableStackTrace"), new GUIContent("启用堆栈跟踪"));
            
            if (settings.enableStackTrace)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("stackTraceMinLevel"), new GUIContent("堆栈跟踪最小级别"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxStackTraceDepth"), new GUIContent("最大堆栈深度"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawAsyncSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableAsyncWrite"), new GUIContent("启用异步写入"));
            
            if (settings.enableAsyncWrite)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxQueueSize"), new GUIContent("队列最大大小"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("bufferSize"), new GUIContent("缓冲区大小"));
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
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("maxFileSizeMB"), new GUIContent("最大文件大小(MB)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("keepSizeMB"), new GUIContent("保留大小(MB)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableSizeCheck"), new GUIContent("启用大小检查"));
                
                if (settings.enableSizeCheck)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(serializedSettings.FindProperty("sizeCheckInterval"), new GUIContent("检查间隔(秒)"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableFileCompression"), new GUIContent("启用文件压缩"));
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
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("timeoutMs"), new GUIContent("超时时间(毫秒)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("retryCount"), new GUIContent("重试次数"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("batchSize"), new GUIContent("批量大小"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("sendInterval"), new GUIContent("发送间隔(毫秒)"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("serverMinLevel"), new GUIContent("最小上报级别"));
                EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableServerCompression"), new GUIContent("启用数据压缩"));
                
                // 显示当前状态
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("当前状态:", EditorStyles.boldLabel);
                var manager = EZLoggerManager.Instance;
                EditorGUILayout.LabelField($"  系统监控: {(manager.IsSystemLogMonitorEnabled ? "启用" : "禁用")}");
                EditorGUILayout.LabelField($"  服务器上报: {(manager.IsServerReportingEnabled ? "启用" : "禁用")}");
                EditorGUILayout.LabelField($"  服务器URL: {(string.IsNullOrEmpty(manager.GetServerReportUrl()) ? "未配置" : manager.GetServerReportUrl())}");
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSystemSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("enableSystemLogMonitor"), new GUIContent("启用系统日志监控"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("系统监控说明:", EditorStyles.helpBox);
            EditorGUILayout.LabelField("• 自动捕获Unity内部错误和异常", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• 与自己API的错误进行防重复处理", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• 支持错误自动上报到服务器", EditorStyles.miniLabel);
        }

        private void DrawExtensionSettings()
        {
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("collectDeviceInfo"), new GUIContent("收集设备信息"));
            EditorGUILayout.PropertyField(serializedSettings.FindProperty("collectPerformanceInfo"), new GUIContent("收集性能信息"));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("扩展说明:", EditorStyles.helpBox);
            EditorGUILayout.LabelField("• 设备信息包括平台、版本、设备型号等", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• 性能信息包括内存使用、FPS等", EditorStyles.miniLabel);
        }

        private void DrawBottomButtons()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开日志目录", GUILayout.Width(120)))
                {
                    OpenLogDirectory();
                }
                
                if (GUILayout.Button("测试服务器连接", GUILayout.Width(120)))
                {
                    TestServerConnection();
                }
                
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button("导出配置", GUILayout.Width(80)))
                {
                    ExportSettings();
                }
                
                if (GUILayout.Button("导入配置", GUILayout.Width(80)))
                {
                    ImportSettings();
                }
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
                    manager.SetReportExtraData("timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }
            }
        }

        private void OpenLogDirectory()
        {
            var logPath = System.IO.Path.Combine(Application.persistentDataPath, settings.logDirectory);
            if (!System.IO.Directory.Exists(logPath))
            {
                System.IO.Directory.CreateDirectory(logPath);
            }
            EditorUtility.RevealInFinder(logPath);
        }

        private void TestServerConnection()
        {
            if (string.IsNullOrEmpty(settings.serverUrl))
            {
                EditorUtility.DisplayDialog("测试连接", "请先配置服务器URL", "确定");
                return;
            }
            
            // 这里可以添加实际的连接测试逻辑
            EditorUtility.DisplayDialog("测试连接", $"正在测试连接到: {settings.serverUrl}\n(功能待实现)", "确定");
        }

        private void ExportSettings()
        {
            var path = EditorUtility.SaveFilePanel("导出EZ Logger配置", "", "EZLoggerConfig", "json");
            if (!string.IsNullOrEmpty(path))
            {
                var json = JsonUtility.ToJson(settings, true);
                System.IO.File.WriteAllText(path, json);
                EditorUtility.DisplayDialog("导出完成", $"配置已导出到: {path}", "确定");
            }
        }

        private void ImportSettings()
        {
            var path = EditorUtility.OpenFilePanel("导入EZ Logger配置", "", "json");
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(path);
                    JsonUtility.FromJsonOverwrite(json, settings);
                    serializedSettings.Update();
                    settings.Save();
                    EditorUtility.DisplayDialog("导入完成", "配置已成功导入", "确定");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog("导入失败", $"导入配置时出错: {e.Message}", "确定");
                }
            }
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            
            if (foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateEZLoggerSettingsProvider()
        {
            var provider = new EZLoggerSettingsProvider("Project/EZ Logger", SettingsScope.Project)
            {
                keywords = GetSearchKeywordsFromGUIContentProperties<EZLoggerSettings>()
            };
            return provider;
        }
    }
}
