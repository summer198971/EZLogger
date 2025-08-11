using UnityEngine;
using EZLogger;

namespace EZLogger.Tests
{
    /// <summary>
    /// 测试初始化顺序修复的测试类
    /// </summary>
    public class InitializationOrderTest : MonoBehaviour
    {
        void Start()
        {
            TestInitializationOrder();
        }

        /// <summary>
        /// 测试运行时配置加载是否正确
        /// </summary>
        private void TestInitializationOrder()
        {
            Debug.Log("[InitializationOrderTest] 开始测试运行时配置加载...");

            // 1. 测试配置来源
            Debug.Log("[InitializationOrderTest] 测试配置加载源...");
            var loadedConfig = RuntimeSettingsLoader.LoadConfiguration();
            Debug.Log($"[InitializationOrderTest] 运行时配置加载完成 - 性能模式: {loadedConfig.PerformanceMode}");

            // 2. 测试EZLoggerManager是否使用了正确的配置
            var manager = EZLoggerManager.Instance;
            Debug.Log($"[InitializationOrderTest] Manager实例化完成，当前启用级别: {manager.EnabledLevels}");

            // 3. 测试配置一致性
            var currentConfig = manager.Configuration;
            Debug.Log($"[InitializationOrderTest] Manager配置 - 性能模式: {currentConfig.PerformanceMode}, 异步写入: {currentConfig.EnableAsyncWrite}");
            Debug.Log($"[InitializationOrderTest] 配置源路径检查完成");

            // 4. 测试构建类型相关的默认配置
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[InitializationOrderTest] 当前为开发构建，应使用开发配置");
#else
            Debug.Log("[InitializationOrderTest] 当前为发布构建，应使用优化配置");
#endif

            // 5. 测试零开销API是否工作正常
            Debug.Log("[InitializationOrderTest] 测试零开销API...");

            // 这些调用应该能正确工作，不管级别是否启用
            EZLog.Log?.Log("Test", "这是一个Log级别消息");
            EZLog.Warning?.Log("Test", "这是一个Warning级别消息");
            EZLog.Error?.Log("Test", "这是一个Error级别消息");

            // 6. 测试级别控制
            Debug.Log("[InitializationOrderTest] 测试动态级别控制...");

            var originalLevels = EZLog.GetEnabledLevels();
            Debug.Log($"[InitializationOrderTest] 原始启用级别: {originalLevels}");

            // 禁用Log级别
            EZLog.DisableLevel(LogLevel.Log);
            Debug.Log($"[InitializationOrderTest] 禁用Log后的级别: {EZLog.GetEnabledLevels()}");

            // 测试禁用后的零开销特性
            EZLog.Log?.Log("Test", "这条消息应该不会显示");
            EZLog.Warning?.Log("Test", "这条Warning消息应该仍然显示");

            // 恢复原始级别
            EZLog.SetEnabledLevels(originalLevels);
            Debug.Log($"[InitializationOrderTest] 恢复后的级别: {EZLog.GetEnabledLevels()}");

            // 7. 测试Critical Logger重构
            Debug.Log("[InitializationOrderTest] 测试Critical Logger重构...");
            TestCriticalLoggerRefactoring();

            // 8. 测试配置路径信息
            Debug.Log($"[InitializationOrderTest] StreamingAssets路径: {Application.streamingAssetsPath}");
            Debug.Log($"[InitializationOrderTest] PersistentData路径: {Application.persistentDataPath}");

            Debug.Log("[InitializationOrderTest] 运行时配置加载测试完成！");
        }

        /// <summary>
        /// 测试Critical Logger重构是否正确工作
        /// </summary>
        private void TestCriticalLoggerRefactoring()
        {
            Debug.Log("[CriticalLoggerTest] 开始测试Logger映射机制...");

            // 1. 测试Logger类型映射
            Debug.Log("[CriticalLoggerTest] ==> 测试Logger类型映射");
            var allLoggerTypes = EZLog.GetAllLoggerTypes();
            foreach (var kvp in allLoggerTypes)
            {
                Debug.Log($"[CriticalLoggerTest] {kvp.Key} -> {kvp.Value}");
            }

            // 2. 验证特定级别的Logger类型
            Debug.Log("[CriticalLoggerTest] ==> 验证Logger类型正确性");
            string errorType = EZLog.GetLoggerTypeName(LogLevel.Error);
            string exceptionType = EZLog.GetLoggerTypeName(LogLevel.Exception);
            string warningType = EZLog.GetLoggerTypeName(LogLevel.Warning);
            string logType = EZLog.GetLoggerTypeName(LogLevel.Log);

            Debug.Log($"[CriticalLoggerTest] Error使用: {errorType}");
            Debug.Log($"[CriticalLoggerTest] Exception使用: {exceptionType}");
            Debug.Log($"[CriticalLoggerTest] Warning使用: {warningType}");
            Debug.Log($"[CriticalLoggerTest] Log使用: {logType}");

            // 验证映射是否正确
            bool errorIsCritical = errorType == "CriticalConditionalLogger";
            bool exceptionIsCritical = exceptionType == "CriticalConditionalLogger";
            bool warningIsBasic = warningType == "ConditionalLogger";
            bool logIsBasic = logType == "ConditionalLogger";

            Debug.Log($"[CriticalLoggerTest] ✓ Error使用Critical Logger: {errorIsCritical}");
            Debug.Log($"[CriticalLoggerTest] ✓ Exception使用Critical Logger: {exceptionIsCritical}");
            Debug.Log($"[CriticalLoggerTest] ✓ Warning使用基础Logger: {warningIsBasic}");
            Debug.Log($"[CriticalLoggerTest] ✓ Log使用基础Logger: {logIsBasic}");

            // 3. 测试特殊处理功能
            Debug.Log("[CriticalLoggerTest] ==> 测试特殊处理功能");
            EZLog.Error?.Log("CriticalTest", "这是一个测试Error消息，应包含防重复机制");
            EZLog.Exception?.Log("CriticalTest", "这是一个测试Exception消息，应包含防重复机制");
            EZLog.Warning?.Log("CriticalTest", "这是一个测试Warning消息，无特殊处理");
            EZLog.Log?.Log("CriticalTest", "这是一个测试Log消息，无特殊处理");

            // 4. 测试禁用级别时的零开销特性
            Debug.Log("[CriticalLoggerTest] ==> 测试零开销特性");
            var originalLevels = EZLog.GetEnabledLevels();
            EZLog.DisableLevel(LogLevel.Error);

            // 这个调用应该是零开销的（EZLog.Error返回null）
            EZLog.Error?.Log("CriticalTest", "这条Error消息应该不会显示（零开销）");
            EZLog.Exception?.Log("CriticalTest", "这条Exception消息应该显示");

            // 恢复原始设置
            EZLog.SetEnabledLevels(originalLevels);

            // 5. 测试动态注册Logger类型（高级功能）
            Debug.Log("[CriticalLoggerTest] ==> 测试动态Logger类型注册");
            TestDynamicLoggerRegistration();

            Debug.Log("[CriticalLoggerTest] Logger映射机制测试完成！");
        }

        /// <summary>
        /// 测试动态Logger类型注册功能
        /// </summary>
        private void TestDynamicLoggerRegistration()
        {
            Debug.Log("[DynamicLoggerTest] 开始测试动态Logger注册...");

            // 保存原始的Log级别Logger类型
            string originalLogType = EZLog.GetLoggerTypeName(LogLevel.Log);
            Debug.Log($"[DynamicLoggerTest] 原始Log Logger类型: {originalLogType}");

            // 注册一个自定义的Logger类型（这里我们用CriticalConditionalLogger作为示例）
            EZLog.RegisterLoggerType(LogLevel.Log, (level, logger) => new CriticalConditionalLogger(level, logger));

            // 验证类型已更改
            string newLogType = EZLog.GetLoggerTypeName(LogLevel.Log);
            Debug.Log($"[DynamicLoggerTest] 新Log Logger类型: {newLogType}");

            bool registrationSuccess = newLogType == "CriticalConditionalLogger";
            Debug.Log($"[DynamicLoggerTest] ✓ 动态注册成功: {registrationSuccess}");

            // 测试新Logger的功能
            Debug.Log("[DynamicLoggerTest] 测试动态注册后的Logger功能...");
            EZLog.Log?.Log("DynamicTest", "这条Log消息现在也有Critical处理功能了");

            // 恢复原始的Logger类型
            EZLog.RegisterLoggerType(LogLevel.Log, (level, logger) => new ConditionalLogger(level, logger));
            string restoredLogType = EZLog.GetLoggerTypeName(LogLevel.Log);
            Debug.Log($"[DynamicLoggerTest] 恢复后Log Logger类型: {restoredLogType}");

            Debug.Log("[DynamicLoggerTest] 动态Logger注册测试完成！");
        }

        [ContextMenu("Run Manual Test")]
        public void RunManualTest()
        {
            TestInitializationOrder();
        }

        [ContextMenu("Test Critical Logger Only")]
        public void TestCriticalLoggerOnly()
        {
            TestCriticalLoggerRefactoring();
        }
    }
}
