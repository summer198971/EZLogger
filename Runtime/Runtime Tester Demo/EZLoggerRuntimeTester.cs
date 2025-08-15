using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EZLogger;

namespace EZLoggerSamples
{
    /// <summary>
    /// EZ Logger 运行时测试器 - 用于测试打包后运行时配置修改功能
    /// 这个脚本可以在打包后的应用中测试各种日志配置的实时修改效果
    /// 
    /// 重要说明：
    /// 1. 这是一个示例脚本，位于Samples~目录中
    /// 2. 在实际项目中使用时，请将此脚本复制到项目的适当位置
    /// 3. 需要确保项目中包含Unity UI模块的引用：
    ///    - UnityEngine.UI
    ///    - UnityEngine.UIModule  
    ///    - UnityEngine.TextRenderingModule
    /// 4. 如果在Samples目录中直接使用报错，这是正常的，因为Samples不包含完整的程序集引用
    /// </summary>
    public class EZLoggerRuntimeTester : MonoBehaviour
    {
        [Header("UI元素引用")]
        public Canvas testCanvas;
        public Transform panelContainer
        {
            get
            {
                return testLogPanelGO.transform;
            }
        }
        public GameObject testLogPanelGO;
        public Button showHideButton;
        public Text titleText;
        public ScrollRect scrollRect;
        public Transform contentParent
        {
            get
            {
                return contentParentGO.transform;
            }
        }
        public GameObject contentParentGO;
        public Button testLogButton;
        public Button clearConsoleButton;
        public Button stackTracePerformanceButton;

        public Button OpenLogFileButton;

        [Header("测试配置")]
        [Tooltip("是否在启动时显示测试面板")]
        public bool showOnStart = true;

        [Tooltip("测试面板的显示键")]
        public KeyCode toggleKey = KeyCode.F12;

        // UI控制状态
        private bool _isPanelVisible = true;
        private Dictionary<string, Toggle> _levelToggles = new Dictionary<string, Toggle>();
        private Dictionary<string, Toggle> _featureToggles = new Dictionary<string, Toggle>();
        private Dictionary<string, Toggle> _toggles = new Dictionary<string, Toggle>();
        private Dictionary<string, InputField> _inputFields = new Dictionary<string, InputField>();
        private Dictionary<string, Text> _statusTexts = new Dictionary<string, Text>();

        // 运行时配置引用
        private EZLogger.EZLoggerManager _loggerManager;
        private EZLogger.LoggerConfiguration _runtimeConfig;

        // 测试计数器
        private int _testCounter = 0;
        private float _lastTestTime = 0f;

        private void Awake()
        {
            // 获取Logger实例
            _loggerManager = EZLogger.EZLoggerManager.Instance;
            _runtimeConfig = _loggerManager.Configuration;

            // 初始化UI
            InitializeUI();
        }

        private void Start()
        {
            if (!showOnStart)
            {
                HidePanel();
            }

            // 注册级别变化事件
            EZLogger.EZLoggerManager.OnLevelsChanged += OnLogLevelsChanged;

            // 延迟显示初始状态，避免UI重建问题
            StartCoroutine(DelayedInitialRefresh());

            LogTestMessage("EZLogger运行时测试器启动完成");

            // 验证UI创建结果
            ValidateUICreation();
        }

        /// <summary>
        /// 验证UI创建是否成功
        /// </summary>
        private void ValidateUICreation()
        {
            if (testCanvas == null)
            {
                LogTestMessage("错误：Canvas创建失败");
                return;
            }

            if (panelContainer == null)
            {
                LogTestMessage("错误：主面板创建失败");
                return;
            }

            if (contentParent == null)
            {
                LogTestMessage("错误：内容容器创建失败");
                return;
            }

            if (EventSystem.current == null)
            {
                LogTestMessage("警告：EventSystem未找到，UI交互可能无法正常工作");
            }

            // 检查UI层级关系
            var canvasChildCount = testCanvas.transform.childCount;
            var panelChildCount = panelContainer.childCount;
            var contentChildCount = contentParent.childCount;

            LogTestMessage($"UI创建验证完成:");
            LogTestMessage($"Canvas: {testCanvas.name} (子元素: {canvasChildCount})");
            LogTestMessage($"Panel: {panelContainer.name} (子元素: {panelChildCount})");
            LogTestMessage($"Content: {contentParent.name} (子元素: {contentChildCount})");

            // 修复散落的UI元素
            StartCoroutine(FixScatteredUIElements());
        }

        /// <summary>
        /// 修复散落在场景中的UI元素
        /// </summary>
        private System.Collections.IEnumerator FixScatteredUIElements()
        {
            yield return new WaitForEndOfFrame();

            LogTestMessage("开始修复散落的UI元素...");

            // 重新获取引用，因为可能在UI创建过程中丢失
            RefreshReferences();

            // 查找所有散落的UI元素并移到正确的父级下
            var allObjects = FindObjectsOfType<Transform>();
            int fixedCount = 0;

            foreach (var obj in allObjects)
            {
                if (obj.parent == null || obj.parent == transform) // 散落在根目录或组件根目录
                {
                    var name = obj.name;

                    // 标题和按钮应该在面板下
                    if (name.Contains("Title") || name.Contains("Button_") || name.Contains("Scroll View"))
                    {
                        if (panelContainer != null && obj.transform != panelContainer)
                        {
                            obj.SetParent(panelContainer, false);
                            fixedCount++;
                            LogTestMessage($"修复元素: {name} -> {panelContainer.name}");
                        }
                    }
                    // 其他控件应该在内容区域下
                    else if (name.Contains("Header_") || name.Contains("Toggle_") ||
                            name.Contains("InputField_") || name.Contains("Status_") ||
                            name.Contains("Quick Level Buttons"))
                    {
                        if (contentParent != null)
                        {
                            obj.SetParent(contentParent, false);
                            fixedCount++;
                            LogTestMessage($"修复元素: {name} -> {contentParent.name}");
                        }
                    }
                }
            }

            LogTestMessage($"UI修复完成，共修复了 {fixedCount} 个元素");

            // 强制刷新布局
            if (contentParent != null)
            {
                var layoutGroup = contentParent.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup != null)
                {
                    layoutGroup.enabled = false;
                    yield return null;
                    layoutGroup.enabled = true;
                }
            }

            // 刷新显示
            RefreshAllDisplays();
        }

        /// <summary>
        /// 重新获取UI引用
        /// </summary>
        private void RefreshReferences()
        {
            // // 重新查找并设置引用
            // if (panelContainer == null)
            // {
            //     var panel = GameObject.Find("Test Panel");
            //     if (panel != null)
            //     {
            //         panelContainer = panel.transform;
            //         LogTestMessage("重新找到 panelContainer 引用");
            //     }
            // }

            // if (contentParent == null)
            // {
            //     var content = GameObject.Find("Content");
            //     if (content != null)
            //     {
            //         contentParent = content.transform;
            //         LogTestMessage("重新找到 contentParent 引用");
            //     }
            // }
        }

        private void Update()
        {
            // 检查切换键
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }
        }

        private void OnDestroy()
        {
            // 取消事件订阅
            EZLogger.EZLoggerManager.OnLevelsChanged -= OnLogLevelsChanged;
        }

        #region UI初始化

        /// <summary>
        /// 初始化UI界面
        /// </summary>
        private void InitializeUI()
        {
            if (testCanvas == null)
            {
                CreateUIFromScratch();
            }
            else
            {
                SetupExistingUI();
            }
        }

        /// <summary>
        /// 从头创建UI界面（当没有预制件时）
        /// </summary>
        private void CreateUIFromScratch()
        {
            // 确保有EventSystem用于UI交互
            EnsureEventSystem();

            // 创建Canvas
            var canvasGO = new GameObject("EZLogger Test Canvas");
            // ScreenSpaceOverlay Canvas 不应该有父对象

            testCanvas = canvasGO.AddComponent<Canvas>();
            testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            testCanvas.sortingOrder = 1000; // 确保在最上层

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            canvasScaler.matchWidthOrHeight = 0.5f; // 平衡宽高适配

            canvasGO.AddComponent<GraphicRaycaster>();

            // 创建主面板
            CreateMainPanel();

            // 创建UI控件
            CreateUIControls();
        }

        /// <summary>
        /// 确保场景中有EventSystem组件用于UI交互
        /// </summary>
        private void EnsureEventSystem()
        {
            if (EventSystem.current == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
                LogTestMessage("已创建EventSystem组件");
            }
        }

        /// <summary>
        /// 设置现有UI（当有预制件时）
        /// </summary>
        private void SetupExistingUI()
        {
            // 绑定按钮事件
            if (showHideButton != null)
                showHideButton.onClick.AddListener(TogglePanel);

            if (testLogButton != null)
                testLogButton.onClick.AddListener(RunLogTests);

            // if (clearConsoleButton != null)
            //     clearConsoleButton.onClick.AddListener(ClearConsole);

            // 查找或创建控件
            FindOrCreateControls();
        }

        /// <summary>
        /// 创建主面板
        /// </summary>
        private void CreateMainPanel()
        {
            var panelGO = new GameObject("Test Panel");
            panelGO.transform.SetParent(testCanvas.transform, false);
            // panelContainer = panelGO.transform;
            testLogPanelGO = panelGO;

            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            // 添加阴影效果
            var shadow = panelGO.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(3, -3);

            // 添加标题
            CreateTitle();

            // 添加按钮
            CreateButtons();

            // 添加滚动区域
            CreateScrollArea();
        }

        /// <summary>
        /// 创建标题
        /// </summary>
        private void CreateTitle()
        {
            var titleGO = new GameObject("Title");

            // 确保在添加RectTransform之后再设置父级
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleGO.transform.SetParent(panelContainer, false);

            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);

            titleText = titleGO.AddComponent<Text>();
            titleText.text = "EZ Logger 运行时配置测试器 (Samples)";
            titleText.font = GetBuiltinFont();
            titleText.fontSize = 24;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 创建控制按钮
        /// </summary>
        private void CreateButtons()
        {
            // 隐藏/显示按钮
            var hideButtonGO = CreateButton("Hide Panel", new Vector2(0.8f, 0.85f), new Vector2(0.95f, 0.9f));
            showHideButton = hideButtonGO.GetComponent<Button>();
            showHideButton.onClick.AddListener(TogglePanel);

            // 测试日志按钮
            var testButtonGO = CreateButton("Test Logs", new Vector2(0.05f, 0.85f), new Vector2(0.2f, 0.9f));
            testLogButton = testButtonGO.GetComponent<Button>();
            testLogButton.onClick.AddListener(RunLogTests);

            // 清除控制台按钮
            // var clearButtonGO = CreateButton("Clear Console", new Vector2(0.25f, 0.85f), new Vector2(0.4f, 0.9f));
            // clearConsoleButton = clearButtonGO.GetComponent<Button>();
            // clearConsoleButton.onClick.AddListener(ClearConsole);

            // 打开日志文件按钮
            var openLogFileButtonGO = CreateButton("Open Log File", new Vector2(0.45f, 0.85f), new Vector2(0.6f, 0.9f));
            OpenLogFileButton = openLogFileButtonGO.GetComponent<Button>();
            OpenLogFileButton.onClick.AddListener(OpenLogFolder);

            // 堆栈跟踪性能测试按钮
            var performanceButtonGO = CreateButton("StackTrace Perf", new Vector2(0.65f, 0.85f), new Vector2(0.78f, 0.9f));
            stackTracePerformanceButton = performanceButtonGO.GetComponent<Button>();
            stackTracePerformanceButton.onClick.AddListener(RunStackTracePerformanceTest);
        }

        /// <summary>
        /// 创建按钮辅助方法
        /// </summary>
        private GameObject CreateButton(string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var buttonGO = new GameObject($"Button_{text}");
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonGO.transform.SetParent(panelContainer, false);

            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMax;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.3f, 0.8f, 0.8f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            var textGO = new GameObject("Text");
            var textRect = textGO.AddComponent<RectTransform>();
            textGO.transform.SetParent(buttonGO.transform, false);

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = GetBuiltinFont();
            textComponent.fontSize = 14;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;

            return buttonGO;
        }

        /// <summary>
        /// 创建滚动区域
        /// </summary>
        private void CreateScrollArea()
        {
            var scrollGO = new GameObject("Scroll View");
            scrollGO.transform.SetParent(panelContainer, false);

            var scrollRectTransform = scrollGO.AddComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0.05f);
            scrollRectTransform.anchorMax = new Vector2(1, 0.8f);
            scrollRectTransform.offsetMin = new Vector2(10, 0);
            scrollRectTransform.offsetMax = new Vector2(-10, 0);

            var scrollImage = scrollGO.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);

            this.scrollRect = scrollGO.AddComponent<ScrollRect>();

            // 创建视口
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);

            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            // 使用RectMask2D替代Mask，更适合UI裁剪
            viewportGO.AddComponent<RectMask2D>();

            // 创建内容区域
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            contentParentGO = contentGO;

            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero; // 确保位置为0
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // 设置ScrollRect引用
            this.scrollRect.content = contentRect;
            this.scrollRect.viewport = viewportRect;
            this.scrollRect.horizontal = false;
            this.scrollRect.vertical = true;
            this.scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // 强制重置滚动位置到顶部
            this.scrollRect.normalizedPosition = new Vector2(0, 1);

            // 添加垂直布局组件
            var layoutGroup = contentGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;

            var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        /// <summary>
        /// 创建UI控件
        /// </summary>
        private void CreateUIControls()
        {
            // 创建日志级别控制区域
            CreateLogLevelControls();

            // 创建功能开关控制区域
            CreateFeatureControls();

            // 创建配置输入区域
            CreateConfigurationInputs();

            // 创建状态显示区域
            CreateStatusDisplays();
        }

        /// <summary>
        /// 查找或创建控件（用于预制件模式）
        /// </summary>
        private void FindOrCreateControls()
        {
            if (contentParent == null && scrollRect != null)
            {
                contentParentGO = scrollRect.content.gameObject;
            }

            if (contentParent != null)
            {
                CreateUIControls();
            }
        }

        #endregion

        #region 控件创建

        /// <summary>
        /// 创建日志级别控制区域
        /// </summary>
        private void CreateLogLevelControls()
        {
            CreateSectionHeader("日志级别控制");

            // 为每个日志级别创建开关
            var logLevels = new (string name, EZLogger.LogLevel level, string description)[]
            {
                ("Log", EZLogger.LogLevel.Log, "普通日志 (对应Unity LogType.Log)"),
                ("Warning", EZLogger.LogLevel.Warning, "警告日志 (对应Unity LogType.Warning)"),
                ("Assert", EZLogger.LogLevel.Assert, "断言日志 (对应Unity LogType.Assert)"),
                ("Error", EZLogger.LogLevel.Error, "错误日志 (对应Unity LogType.Error)"),
                ("Exception", EZLogger.LogLevel.Exception, "异常日志 (对应Unity LogType.Exception)")
            };

            foreach (var logLevel in logLevels)
            {
                var toggle = CreateToggle($"level_{logLevel.name}", $"{logLevel.name} - {logLevel.description}",
                    HasLogLevel(_runtimeConfig.GlobalEnabledLevels, logLevel.level));

                var capturedLevel = logLevel.level; // 闭包捕获
                toggle.onValueChanged.AddListener(enabled => OnLogLevelToggled(capturedLevel, enabled));
                _levelToggles[logLevel.name] = toggle;
            }

            // 添加快捷设置按钮
            CreateQuickLevelButtons();
        }

        /// <summary>
        /// 创建快捷级别设置按钮
        /// </summary>
        private void CreateQuickLevelButtons()
        {
            var quickButtonsGO = new GameObject("Quick Level Buttons");
            quickButtonsGO.transform.SetParent(contentParent, false);

            var layoutGroup = quickButtonsGO.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 5;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            var layoutElement = quickButtonsGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30;

            // 创建快捷按钮
            var quickButtons = new (string name, string text, System.Action action)[]
            {
                ("All", "全部启用", () => SetLogLevels(EZLogger.LogLevel.All)),
                ("None", "全部禁用", () => SetLogLevels(EZLogger.LogLevel.None)),
                ("Error+", "仅错误", () => SetLogLevels(EZLogger.LogLevel.ErrorAndAbove)),
                ("Warn+", "警告以上", () => SetLogLevels(EZLogger.LogLevel.WarningAndAbove))
            };

            foreach (var quickButton in quickButtons)
            {
                var buttonGO = CreateQuickButton(quickButton.text, quickButton.action);
                buttonGO.transform.SetParent(quickButtonsGO.transform, false);
            }
        }

        /// <summary>
        /// 创建功能开关控制区域
        /// </summary>
        private void CreateFeatureControls()
        {
            CreateSectionHeader("功能开关");

            // Unity控制台相关
            var unityConsoleToggle = CreateToggle("unity_console", "Unity控制台输出",
                _runtimeConfig.UnityConsole.Enabled);
            unityConsoleToggle.onValueChanged.AddListener(enabled => OnUnityConsoleToggled(enabled));
            _featureToggles["unity_console"] = unityConsoleToggle;

            var unityColorsToggle = CreateToggle("unity_colors", "Unity控制台颜色",
                _runtimeConfig.UnityConsole.EnableColors);
            unityColorsToggle.onValueChanged.AddListener(enabled => OnUnityColorsToggled(enabled));
            _featureToggles["unity_colors"] = unityColorsToggle;

            // 文件输出相关
            var fileOutputToggle = CreateToggle("file_output", "文件输出",
                _runtimeConfig.FileOutput.Enabled);
            fileOutputToggle.onValueChanged.AddListener(enabled => OnFileOutputToggled(enabled));
            _featureToggles["file_output"] = fileOutputToggle;




            // 堆栈跟踪
            var stackTraceToggle = CreateToggle("stack_trace", "堆栈跟踪",
                _runtimeConfig.EnableStackTrace);
            stackTraceToggle.onValueChanged.AddListener(enabled => OnStackTraceToggled(enabled));
            _featureToggles["stack_trace"] = stackTraceToggle;

            // 系统日志监控
            var systemMonitorToggle = CreateToggle("system_monitor", "系统日志监控",
                _loggerManager.IsSystemLogMonitorEnabled);
            systemMonitorToggle.onValueChanged.AddListener(enabled => OnSystemMonitorToggled(enabled));
            _featureToggles["system_monitor"] = systemMonitorToggle;

            // 服务器上报
            var serverReportToggle = CreateToggle("server_report", "服务器错误上报",
                _loggerManager.IsServerReportingEnabled);
            serverReportToggle.onValueChanged.AddListener(enabled => OnServerReportToggled(enabled));
            _featureToggles["server_report"] = serverReportToggle;
        }

        /// <summary>
        /// 创建配置输入区域
        /// </summary>
        private void CreateConfigurationInputs()
        {
            CreateSectionHeader("运行时配置");

            // 服务器URL配置
            var serverUrlField = CreateInputField("server_url", "服务器上报URL:",
                _loggerManager.GetServerReportUrl(), "http://example.com/api/error-report");
            serverUrlField.onEndEdit.AddListener(url => OnServerUrlChanged(url));
            _inputFields["server_url"] = serverUrlField;

            // 日志目录配置
            var logDirField = CreateInputField("log_dir", "日志文件目录:",
                _runtimeConfig.FileOutput.LogDirectory, "Logs");
            logDirField.onEndEdit.AddListener(dir => OnLogDirectoryChanged(dir));
            _inputFields["log_dir"] = logDirField;

            // 日期轮转配置
            var dailyRotationToggle = CreateToggle("daily_rotation", "按日期分文件:",
                _runtimeConfig.FileOutput.EnableDailyRotation);
            dailyRotationToggle.onValueChanged.AddListener(enabled => OnDailyRotationChanged(enabled));
            _toggles["daily_rotation"] = dailyRotationToggle;

            // 时区偏移配置
            var timezoneField = CreateInputField("timezone", "UTC时区偏移:",
                _runtimeConfig.Timezone.UtcOffsetHours.ToString(), "0");
            timezoneField.onEndEdit.AddListener(offset => OnTimezoneOffsetChanged(offset));
            _inputFields["timezone"] = timezoneField;
        }

        /// <summary>
        /// 创建状态显示区域
        /// </summary>
        private void CreateStatusDisplays()
        {
            CreateSectionHeader("运行状态");

            // 当前启用的级别
            var enabledLevelsText = CreateStatusText("enabled_levels", "当前启用级别:");
            _statusTexts["enabled_levels"] = enabledLevelsText;

            // Logger类型信息
            var loggerTypesText = CreateStatusText("logger_types", "Logger类型:");
            _statusTexts["logger_types"] = loggerTypesText;

            // 输出器状态
            var appendersText = CreateStatusText("appenders", "活动输出器:");
            _statusTexts["appenders"] = appendersText;

            // 系统信息
            var systemInfoText = CreateStatusText("system_info", "系统信息:");
            _statusTexts["system_info"] = systemInfoText;

            // 时区信息
            var timezoneText = CreateStatusText("timezone_info", "时区信息:");
            _statusTexts["timezone_info"] = timezoneText;
        }

        #endregion

        #region UI辅助方法

        /// <summary>
        /// 创建章节标题
        /// </summary>
        private void CreateSectionHeader(string title)
        {
            var headerGO = new GameObject($"Header_{title}");
            headerGO.transform.SetParent(contentParent, false);

            var layoutElement = headerGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 25;

            var headerText = headerGO.AddComponent<Text>();
            headerText.text = title;
            headerText.font = GetBuiltinFont();
            headerText.fontSize = 16;
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = new Color(0.8f, 0.9f, 1f, 1f);
            headerText.alignment = TextAnchor.MiddleLeft;
        }

        /// <summary>
        /// 创建开关控件
        /// </summary>
        private Toggle CreateToggle(string name, string label, bool initialValue)
        {
            var toggleGO = new GameObject($"Toggle_{name}");
            toggleGO.transform.SetParent(contentParent, false);

            var layoutElement = toggleGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 20;

            var toggle = toggleGO.AddComponent<Toggle>();
            toggle.isOn = initialValue;

            // 创建背景
            var backgroundGO = new GameObject("Background");
            backgroundGO.transform.SetParent(toggleGO.transform, false);

            var bgRect = backgroundGO.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(10, 0);
            bgRect.sizeDelta = new Vector2(14, 14);

            var bgImage = backgroundGO.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            // 创建勾选标记
            var checkmarkGO = new GameObject("Checkmark");
            checkmarkGO.transform.SetParent(backgroundGO.transform, false);

            var checkRect = checkmarkGO.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            var checkImage = checkmarkGO.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            // 创建标签
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(toggleGO.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(25, 0);
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = GetBuiltinFont();
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;

            // 设置Toggle组件引用
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;

            return toggle;
        }

        /// <summary>
        /// 创建输入框
        /// </summary>
        private InputField CreateInputField(string name, string label, string initialValue, string placeholder)
        {
            var containerGO = new GameObject($"InputField_{name}");
            containerGO.transform.SetParent(contentParent, false);

            var layoutElement = containerGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 25;

            var hLayout = containerGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 5;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;

            // 创建标签
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(containerGO.transform, false);

            var labelLayoutElement = labelGO.AddComponent<LayoutElement>();
            labelLayoutElement.preferredWidth = 150;

            var labelText = labelGO.AddComponent<Text>();
            labelText.text = label;
            labelText.font = GetBuiltinFont();
            labelText.fontSize = 12;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;

            // 创建输入框
            var inputGO = new GameObject("Input");
            inputGO.transform.SetParent(containerGO.transform, false);

            var inputLayoutElement = inputGO.AddComponent<LayoutElement>();
            inputLayoutElement.flexibleWidth = 1;

            var inputImage = inputGO.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // 创建文本显示
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 0);
            textRect.offsetMax = new Vector2(-5, 0);

            var textComponent = textGO.AddComponent<Text>();
            textComponent.font = GetBuiltinFont();
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;

            // 创建占位符文本
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);

            var placeholderRect = placeholderGO.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(5, 0);
            placeholderRect.offsetMax = new Vector2(-5, 0);

            var placeholderText = placeholderGO.AddComponent<Text>();
            placeholderText.text = placeholder;
            placeholderText.font = GetBuiltinFont();
            placeholderText.fontSize = 12;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            placeholderText.alignment = TextAnchor.MiddleLeft;

            // 最后创建InputField并设置引用
            var inputField = inputGO.AddComponent<InputField>();
            inputField.targetGraphic = inputImage;
            inputField.textComponent = textComponent;
            inputField.placeholder = placeholderText;

            // 设置初始值（在所有引用设置完成后）
            inputField.text = initialValue;

            return inputField;
        }

        /// <summary>
        /// 创建状态文本
        /// </summary>
        private Text CreateStatusText(string name, string label)
        {
            var textGO = new GameObject($"Status_{name}");
            textGO.transform.SetParent(contentParent, false);

            var layoutElement = textGO.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 40;

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = $"{label}\n(加载中...)";
            textComponent.font = GetBuiltinFont();
            textComponent.fontSize = 11;
            textComponent.color = new Color(0.9f, 0.9f, 0.7f, 1f);
            textComponent.alignment = TextAnchor.UpperLeft;

            return textComponent;
        }

        /// <summary>
        /// 创建快捷按钮
        /// </summary>
        private GameObject CreateQuickButton(string text, System.Action onClick)
        {
            var buttonGO = new GameObject($"QuickButton_{text}");

            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.7f, 0.8f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(() => onClick());

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var textComponent = textGO.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = GetBuiltinFont();
            textComponent.fontSize = 10;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleCenter;

            return buttonGO;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查日志级别是否包含指定级别（兼容旧版本C#）
        /// </summary>
        private bool HasLogLevel(EZLogger.LogLevel levels, EZLogger.LogLevel target)
        {
            return (levels & target) == target;
        }

        /// <summary>
        /// 获取Unity内置字体（兼容不同Unity版本，优先使用中文字体）
        /// </summary>
        private Font GetBuiltinFont()
        {
            // 1. 优先使用Resources中的DreamHanSans字体（支持中文）
            var cjkFont = Resources.Load<Font>("wryh");
            if (cjkFont != null)
            {
                return cjkFont;
            }
            else
            {
                Debug.LogError("DreamHanSans-W15字体未找到");
            }
            return null;

            // // 2. 尝试加载其他可能的中文字体
            // var alternativeFonts = new string[]
            // {
            //     "EZLogger/Fonts/DefaultCJK",
            //     "EZLogger/Fonts/ChineseFont",
            //     "Fonts/DreamHanSans-W15",
            //     "Fonts/ChineseFont"
            // };

            // foreach (var fontPath in alternativeFonts)
            // {
            //     var font = Resources.Load<Font>(fontPath);
            //     if (font != null)
            //         return font;
            // }

            // // 3. 回退到Unity内置字体（仅支持英文，打包后中文可能显示不正常）
            // var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // if (builtinFont != null)
            //     return builtinFont;

            // // 4. 尝试旧版本Arial字体
            // try
            // {
            //     builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            //     if (builtinFont != null)
            //         return builtinFont;
            // }
            // catch (System.ArgumentException)
            // {
            //     // Arial.ttf 在新版本Unity中不可用
            // }

            // // 5. 最后尝试场景中任何可用字体
            // builtinFont = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
            // return builtinFont ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 日志级别开关事件
        /// </summary>
        private void OnLogLevelToggled(EZLogger.LogLevel level, bool enabled)
        {
            if (enabled)
            {
                _loggerManager.EnableLevel(level);
            }
            else
            {
                _loggerManager.DisableLevel(level);
            }

            LogTestMessage($"级别 {level} 已{(enabled ? "启用" : "禁用")}");
            RefreshStatusDisplays();
        }

        /// <summary>
        /// 日志级别变化事件
        /// </summary>
        private void OnLogLevelsChanged(EZLogger.LogLevel newLevels)
        {
            // 更新UI显示
            RefreshLogLevelToggles();
            RefreshStatusDisplays();
        }

        /// <summary>
        /// Unity控制台开关事件
        /// </summary>
        private void OnUnityConsoleToggled(bool enabled)
        {
            _runtimeConfig.UnityConsole.Enabled = enabled;
            _loggerManager.RefreshConfiguration();
            LogTestMessage($"Unity控制台输出已{(enabled ? "启用" : "禁用")}");
            RefreshStatusDisplays();
        }

        /// <summary>
        /// Unity控制台颜色开关事件
        /// </summary>
        private void OnUnityColorsToggled(bool enabled)
        {
            _runtimeConfig.UnityConsole.EnableColors = enabled;
            _loggerManager.RefreshConfiguration();
            LogTestMessage($"Unity控制台颜色已{(enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 文件输出开关事件
        /// </summary>
        private void OnFileOutputToggled(bool enabled)
        {
            _runtimeConfig.FileOutput.Enabled = enabled;
            _loggerManager.RefreshConfiguration();
            LogTestMessage($"文件输出已{(enabled ? "启用" : "禁用")}");
            RefreshStatusDisplays();
        }



        /// <summary>
        /// 堆栈跟踪开关事件
        /// </summary>
        private void OnStackTraceToggled(bool enabled)
        {
            _runtimeConfig.EnableStackTrace = enabled;
            LogTestMessage($"堆栈跟踪已{(enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 系统日志监控开关事件
        /// </summary>
        private void OnSystemMonitorToggled(bool enabled)
        {
            _loggerManager.EnableSystemLogMonitor(enabled);
            LogTestMessage($"系统日志监控已{(enabled ? "启用" : "禁用")}");
            RefreshStatusDisplays();
        }

        /// <summary>
        /// 服务器上报开关事件
        /// </summary>
        private void OnServerReportToggled(bool enabled)
        {
            _loggerManager.EnableServerReporting(enabled);
            LogTestMessage($"服务器错误上报已{(enabled ? "启用" : "禁用")}");
            RefreshStatusDisplays();
        }

        /// <summary>
        /// 服务器URL变更事件
        /// </summary>
        private void OnServerUrlChanged(string url)
        {
            _loggerManager.SetServerReportUrl(url);
            LogTestMessage($"服务器URL已设置为: {url}");
        }

        /// <summary>
        /// 日志目录变更事件
        /// </summary>
        private void OnLogDirectoryChanged(string directory)
        {
            _runtimeConfig.FileOutput.LogDirectory = directory;
            _loggerManager.RefreshConfiguration();
            LogTestMessage($"日志目录已设置为: {directory}");
        }

        /// <summary>
        /// 日期轮转设置变更事件
        /// </summary>
        private void OnDailyRotationChanged(bool enabled)
        {
            _runtimeConfig.FileOutput.EnableDailyRotation = enabled;
            _loggerManager.RefreshConfiguration();
            LogTestMessage($"日期轮转已{(enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 时区偏移变更事件
        /// </summary>
        private void OnTimezoneOffsetChanged(string offsetText)
        {
            if (int.TryParse(offsetText, out int offset))
            {
                _runtimeConfig.Timezone.UtcOffsetHours = offset;
                _runtimeConfig.Timezone.ClampUtcOffset();
                LogTestMessage($"UTC时区偏移已设置为: {_runtimeConfig.Timezone.UtcOffsetHours}小时");
                RefreshStatusDisplays();
            }
            else
            {
                LogTestMessage($"无效的时区偏移: {offsetText}");
            }
        }

        #endregion

        #region 显示刷新

        /// <summary>
        /// 刷新所有显示
        /// </summary>
        private void RefreshAllDisplays()
        {
            // 延迟刷新，避免UI重建循环
            StartCoroutine(DelayedRefreshDisplays());
        }

        /// <summary>
        /// 延迟初始刷新，确保UI完全创建后再刷新
        /// </summary>
        private System.Collections.IEnumerator DelayedInitialRefresh()
        {
            // 等待两帧，确保UI完全创建
            yield return null;
            yield return null;

            RefreshLogLevelToggles();
            RefreshFeatureToggles();
            RefreshInputFields();
            RefreshStatusDisplays();
        }

        /// <summary>
        /// 延迟刷新显示，避免UI重建循环问题
        /// </summary>
        private System.Collections.IEnumerator DelayedRefreshDisplays()
        {
            yield return null; // 等待一帧

            RefreshLogLevelToggles();
            RefreshFeatureToggles();
            RefreshInputFields();
            RefreshStatusDisplays();
        }

        /// <summary>
        /// 刷新日志级别开关显示
        /// </summary>
        private void RefreshLogLevelToggles()
        {
            var currentLevels = _loggerManager.EnabledLevels;

            foreach (var kvp in _levelToggles)
            {
                var levelName = kvp.Key;
                var toggle = kvp.Value;

                EZLogger.LogLevel level = levelName switch
                {
                    "Log" => EZLogger.LogLevel.Log,
                    "Warning" => EZLogger.LogLevel.Warning,
                    "Assert" => EZLogger.LogLevel.Assert,
                    "Error" => EZLogger.LogLevel.Error,
                    "Exception" => EZLogger.LogLevel.Exception,
                    _ => EZLogger.LogLevel.None
                };

                if (level != EZLogger.LogLevel.None)
                {
                    toggle.isOn = HasLogLevel(currentLevels, level);
                }
            }
        }

        /// <summary>
        /// 刷新功能开关显示
        /// </summary>
        private void RefreshFeatureToggles()
        {
            if (_featureToggles.TryGetValue("unity_console", out var unityToggle))
                unityToggle.isOn = _runtimeConfig.UnityConsole.Enabled;

            if (_featureToggles.TryGetValue("unity_colors", out var colorsToggle))
                colorsToggle.isOn = _runtimeConfig.UnityConsole.EnableColors;

            if (_featureToggles.TryGetValue("file_output", out var fileToggle))
                fileToggle.isOn = _runtimeConfig.FileOutput.Enabled;




            if (_featureToggles.TryGetValue("stack_trace", out var stackToggle))
                stackToggle.isOn = _runtimeConfig.EnableStackTrace;

            if (_featureToggles.TryGetValue("system_monitor", out var sysToggle))
                sysToggle.isOn = _loggerManager.IsSystemLogMonitorEnabled;

            if (_featureToggles.TryGetValue("server_report", out var serverToggle))
                serverToggle.isOn = _loggerManager.IsServerReportingEnabled;
        }

        /// <summary>
        /// 刷新输入框显示
        /// </summary>
        private void RefreshInputFields()
        {
            if (_inputFields.TryGetValue("server_url", out var urlField))
                urlField.text = _loggerManager.GetServerReportUrl();

            if (_inputFields.TryGetValue("log_dir", out var dirField))
                dirField.text = _runtimeConfig.FileOutput.LogDirectory;

            if (_toggles.TryGetValue("daily_rotation", out var rotationToggle))
                rotationToggle.isOn = _runtimeConfig.FileOutput.EnableDailyRotation;

            if (_inputFields.TryGetValue("timezone", out var tzField))
                tzField.text = _runtimeConfig.Timezone.UtcOffsetHours.ToString();
        }

        /// <summary>
        /// 刷新状态显示
        /// </summary>
        private void RefreshStatusDisplays()
        {
            // 当前启用级别
            if (_statusTexts.TryGetValue("enabled_levels", out var levelsText))
            {
                var currentLevels = _loggerManager.EnabledLevels;
                var levelsList = new List<string>();

                if (HasLogLevel(currentLevels, EZLogger.LogLevel.Log)) levelsList.Add("Log");
                if (HasLogLevel(currentLevels, EZLogger.LogLevel.Warning)) levelsList.Add("Warning");
                if (HasLogLevel(currentLevels, EZLogger.LogLevel.Assert)) levelsList.Add("Assert");
                if (HasLogLevel(currentLevels, EZLogger.LogLevel.Error)) levelsList.Add("Error");
                if (HasLogLevel(currentLevels, EZLogger.LogLevel.Exception)) levelsList.Add("Exception");

                levelsText.text = $"当前启用级别:\n{(levelsList.Count > 0 ? string.Join(", ", levelsList) : "无")}";
            }

            // Logger类型信息
            if (_statusTexts.TryGetValue("logger_types", out var typesText))
            {
                var loggerTypes = EZLogger.EZLog.GetAllLoggerTypes();
                var sb = new StringBuilder("Logger类型:\n");

                foreach (var kvp in loggerTypes)
                {
                    sb.AppendLine($"{kvp.Key}: {kvp.Value}");
                }

                typesText.text = sb.ToString();
            }

            // 输出器状态
            if (_statusTexts.TryGetValue("appenders", out var appendersText))
            {
                var appenders = _loggerManager.GetAppenders();
                var sb = new StringBuilder("活动输出器:\n");

                foreach (var appender in appenders)
                {
                    sb.AppendLine($"• {appender.Name} ({(appender.IsEnabled ? "启用" : "禁用")})");
                }

                appendersText.text = sb.ToString();
            }

            // 系统信息
            if (_statusTexts.TryGetValue("system_info", out var systemText))
            {
                var sb = new StringBuilder("系统信息:\n");
                sb.AppendLine($"平台: {Application.platform}");
                sb.AppendLine($"Unity版本: {Application.unityVersion}");

                systemText.text = sb.ToString();
            }

            // 时区信息
            if (_statusTexts.TryGetValue("timezone_info", out var timezoneText))
            {
                var timezone = _runtimeConfig.Timezone;
                var sb = new StringBuilder("时区信息:\n");
                sb.AppendLine($"使用UTC: {(timezone.UseUtc ? "是" : "否")}");
                sb.AppendLine($"偏移: {timezone.GetTimezoneDisplayName()}");
                sb.AppendLine($"当前时间: {timezone.FormatTime()}");

                timezoneText.text = sb.ToString();
            }
        }

        #endregion

        #region 测试功能

        /// <summary>
        /// 设置日志级别
        /// </summary>
        private void SetLogLevels(EZLogger.LogLevel levels)
        {
            _loggerManager.EnabledLevels = levels;
            LogTestMessage($"日志级别已设置为: {levels}");
        }

        /// <summary>
        /// 运行日志测试
        /// </summary>
        private void RunLogTests()
        {
            _testCounter++;
            var testTime = Time.time;
            var deltaTime = testTime - _lastTestTime;
            _lastTestTime = testTime;

            LogTestMessage($"开始运行测试 #{_testCounter} (间隔: {deltaTime:F1}s)");

            // 测试所有级别的日志
            EZLogger.EZLog.Log?.Log("测试", $"这是一条Log级别的测试消息 #{_testCounter}");
            EZLogger.EZLog.Warning?.Log("测试", $"这是一条Warning级别的测试消息 #{_testCounter}");
            EZLogger.EZLog.Assert?.Log("测试", $"这是一条Assert级别的测试消息 #{_testCounter}");
            EZLogger.EZLog.Error?.Log("测试", $"这是一条Error级别的测试消息 #{_testCounter}");
            EZLogger.EZLog.Exception?.Log("测试", $"这是一条Exception级别的测试消息 #{_testCounter}");

            // 测试格式化日志
            EZLogger.EZLog.Log?.LogFormat("格式化测试", "测试参数: count={0}, time={1:F2}, bool={2}",
                _testCounter, testTime, _testCounter % 2 == 0);

            // 测试字符串插值（推荐方式）
            EZLogger.EZLog.Warning?.Log("性能测试", $"字符串插值测试: {_testCounter} 次调用");

            // 测试零开销特性 - 即使禁用级别，这些调用也不会产生性能开销
            EZLogger.EZLog.Log?.Log("零开销测试", GetExpensiveString());

            // 🎯 堆栈跟踪演示（每次都执行）
            TestStackTraceFeatures();

            LogTestMessage($"测试 #{_testCounter} 完成");
        }

        /// <summary>
        /// 测试堆栈跟踪功能（演示版本）
        /// </summary>
        private void TestStackTraceFeatures()
        {
            LogTestMessage("--- 堆栈跟踪演示开始 ---");

            // 堆栈跟踪功能已正常工作

            // 1. 测试默认堆栈跟踪配置（只有Error和Exception级别有堆栈）
            LogTestMessage("演示默认配置: 只有Error/Exception级别应该有堆栈跟踪");
            EZLogger.EZLog.Log?.Log("堆栈演示", "Log级别消息（无堆栈跟踪）");
            EZLogger.EZLog.Warning?.Log("堆栈演示", "Warning级别消息（无堆栈跟踪）");
            EZLogger.EZLog.Error?.Log("堆栈演示", "Error级别消息（有堆栈跟踪）");
            EZLogger.EZLog.Exception?.Log("堆栈演示", "Exception级别消息（有堆栈跟踪）");

            // 2. 测试调用链堆栈跟踪
            LogTestMessage("演示调用链堆栈跟踪");
            TestCallChainLevel1();

            // 3. 测试文件日志中的堆栈跟踪
            LogTestMessage("演示文件日志堆栈跟踪（查看日志文件以验证格式）");
            EZLogger.EZLog.Error?.Log("文件堆栈测试", "此错误消息的堆栈跟踪应该同时出现在Unity控制台和日志文件中");

            LogTestMessage("--- 堆栈跟踪演示完成 ---");
            LogTestMessage("💡 提示: 点击'Open Log File'按钮查看文件中的堆栈跟踪格式");
        }

        /// <summary>
        /// 测试调用链堆栈跟踪 - 第1层
        /// </summary>
        private void TestCallChainLevel1()
        {
            TestCallChainLevel2();
        }

        /// <summary>
        /// 测试调用链堆栈跟踪 - 第2层
        /// </summary>
        private void TestCallChainLevel2()
        {
            TestCallChainLevel3();
        }

        /// <summary>
        /// 测试调用链堆栈跟踪 - 第3层
        /// </summary>
        private void TestCallChainLevel3()
        {
            // 这里应该能看到完整的调用链：Level3 -> Level2 -> Level1 -> TestStackTraceFeatures
            EZLogger.EZLog.Error?.Log("调用链测试", $"深层调用错误 #{_testCounter}（应显示完整调用链）");
        }

        /// <summary>
        /// 堆栈跟踪性能基准测试（独立测试）
        /// </summary>
        private void RunStackTracePerformanceTest()
        {
            LogTestMessage("=== 堆栈跟踪性能基准测试开始 ===");

            const int testCount = 1000; // 更大的测试数量用于准确的性能测试
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 保存当前级别设置
            var originalLevels = EZLogger.EZLog.GetEnabledLevels();
            LogTestMessage($"当前日志级别: {originalLevels}");

            // 1. 测试完全禁用时的性能（应该接近零开销）
            LogTestMessage($"开始测试禁用状态性能 ({testCount} 次调用)...");
            EZLogger.EZLog.DisableAll();
            stopwatch.Restart();

            for (int i = 0; i < testCount; i++)
            {
                EZLogger.EZLog.Error?.Log("性能测试", $"禁用的错误日志 {i}");
            }

            stopwatch.Stop();
            long disabledTime = stopwatch.ElapsedTicks;
            LogTestMessage($"禁用状态耗时: {disabledTime} ticks ({stopwatch.ElapsedMilliseconds} ms)");

            // 2. 测试启用Error级别的性能（有堆栈跟踪）
            LogTestMessage($"开始测试Error级别启用性能 ({testCount} 次调用)...");
            EZLogger.EZLog.SetErrorAndAbove(); // 只启用Error和Exception
            stopwatch.Restart();

            for (int i = 0; i < testCount; i++)
            {
                EZLogger.EZLog.Error?.Log("性能测试", $"启用的错误日志 {i}");
            }

            stopwatch.Stop();
            long enabledErrorTime = stopwatch.ElapsedTicks;
            LogTestMessage($"Error级别启用耗时: {enabledErrorTime} ticks ({stopwatch.ElapsedMilliseconds} ms)");

            // 3. 测试启用Log级别的性能（无堆栈跟踪）
            LogTestMessage($"开始测试Log级别启用性能 ({testCount} 次调用)...");
            EZLogger.EZLog.EnableAll();
            stopwatch.Restart();

            for (int i = 0; i < testCount; i++)
            {
                EZLogger.EZLog.Log?.Log("性能测试", $"启用的普通日志 {i}");
            }

            stopwatch.Stop();
            long enabledLogTime = stopwatch.ElapsedTicks;
            LogTestMessage($"Log级别启用耗时: {enabledLogTime} ticks ({stopwatch.ElapsedMilliseconds} ms)");

            // 恢复原始级别设置
            EZLogger.EZLog.SetEnabledLevels(originalLevels);
            LogTestMessage($"已恢复原始日志级别: {originalLevels}");

            // 输出性能分析结果
            LogTestMessage("--- 性能分析结果 ---");
            LogTestMessage($"测试次数: {testCount}");
            LogTestMessage($"禁用状态: {disabledTime} ticks");
            LogTestMessage($"Log级别(无堆栈): {enabledLogTime} ticks");
            LogTestMessage($"Error级别(有堆栈): {enabledErrorTime} ticks");

            // 计算性能比率
            if (disabledTime < enabledLogTime)
            {
                float ratio = enabledLogTime / (float)disabledTime;
                LogTestMessage($"✅ 零开销验证通过: 禁用比Log级别快 {ratio:F1}x");
            }
            else
            {
                LogTestMessage("⚠️ 零开销验证失败: 禁用时应该比启用时更快");
            }

            if (enabledLogTime < enabledErrorTime)
            {
                float stackTraceOverhead = enabledErrorTime / (float)enabledLogTime;
                LogTestMessage($"📊 堆栈跟踪开销: Error级别比Log级别慢 {stackTraceOverhead:F1}x");
            }

            LogTestMessage("=== 堆栈跟踪性能基准测试完成 ===");
        }

        /// <summary>
        /// 模拟昂贵的字符串操作（用于测试零开销特性）
        /// </summary>
        private string GetExpensiveString()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                sb.Append($"昂贵操作{i} ");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 清除控制台
        /// </summary>
        private void ClearConsole()
        {
            // 这里可以添加清除Unity控制台的代码
            // 由于Unity API限制，运行时无法直接清除控制台
            LogTestMessage("清除控制台功能仅在Editor中可用");

            // 但我们可以刷新Logger状态
            _loggerManager.Flush();
            LogTestMessage("Logger缓冲区已刷新");
        }

        /// <summary>
        /// 记录测试消息
        /// </summary>
        private void LogTestMessage(string message)
        {
            EZLogger.EZLog.Log?.Log("测试器", $"[{Time.time:F1}s] {message}");
        }

        private void OpenLogFolder()
        {
            _loggerManager.OpenLogFolder();
        }

        #endregion

        #region 界面控制

        /// <summary>
        /// 切换面板显示
        /// </summary>
        public void TogglePanel()
        {
            if (_isPanelVisible)
            {
                HidePanel();
            }
            else
            {
                ShowPanel();
            }
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        public void ShowPanel()
        {
            if (testCanvas != null)
            {
                testCanvas.enabled = true;
                _isPanelVisible = true;

                if (showHideButton != null)
                {
                    var buttonText = showHideButton.GetComponentInChildren<Text>();
                    if (buttonText != null) buttonText.text = "Hide Panel";
                }

                RefreshAllDisplays();
                LogTestMessage("测试面板已显示");
            }
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        public void HidePanel()
        {
            if (testCanvas != null)
            {
                testCanvas.enabled = false;
                _isPanelVisible = false;

                if (showHideButton != null)
                {
                    var buttonText = showHideButton.GetComponentInChildren<Text>();
                    if (buttonText != null) buttonText.text = "Show Panel";
                }

                LogTestMessage("测试面板已隐藏");
            }
        }

        /// <summary>
        /// 检查面板是否显示
        /// </summary>
        public bool IsPanelVisible => _isPanelVisible && testCanvas != null && testCanvas.enabled;

        #endregion
    }
}
