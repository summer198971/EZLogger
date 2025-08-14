using UnityEngine;
using EZLogger;

namespace EZLoggerSamples
{
    /// <summary>
    /// 简化版运行时测试器 - 不依赖Unity UI，纯代码实现
    /// 适用于快速测试和验证EZ Logger的运行时功能
    /// 
    /// 使用方法：
    /// 1. 将此脚本添加到场景中的任意GameObject
    /// 2. 运行游戏后查看控制台输出
    /// 3. 按数字键1-5切换不同的日志级别
    /// 4. 按T键运行测试
    /// </summary>
    public class SimpleRuntimeTester : MonoBehaviour
    {
        [Header("测试配置")]
        [Tooltip("是否在启动时自动运行演示")]
        public bool autoDemo = true;

        [Tooltip("演示间隔时间")]
        public float demoInterval = 5f;

        private float _lastDemoTime = 0f;
        private int _testCounter = 0;

        private void Start()
        {
            LogMessage("=== EZ Logger 简化版运行时测试器启动 ===");
            LogMessage("按键说明:");
            LogMessage("1 - 启用Log级别");
            LogMessage("2 - 启用Warning级别");
            LogMessage("3 - 启用Assert级别");
            LogMessage("4 - 启用Error级别");
            LogMessage("5 - 启用Exception级别");
            LogMessage("0 - 禁用所有级别");
            LogMessage("9 - 启用所有级别");
            LogMessage("T - 运行测试");
            LogMessage("=====================================");

            // 显示当前状态
            ShowCurrentStatus();
        }

        private void Update()
        {
            // 键盘控制
            HandleKeyboardInput();

            // 自动演示
            if (autoDemo && Time.time - _lastDemoTime > demoInterval)
            {
                RunAutoDemo();
                _lastDemoTime = Time.time;
            }
        }

        /// <summary>
        /// 处理键盘输入
        /// </summary>
        private void HandleKeyboardInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                EZLog.EnableLevel(LogLevel.Log);
                LogMessage("已启用Log级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                EZLog.EnableLevel(LogLevel.Warning);
                LogMessage("已启用Warning级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                EZLog.EnableLevel(LogLevel.Assert);
                LogMessage("已启用Assert级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                EZLog.EnableLevel(LogLevel.Error);
                LogMessage("已启用Error级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                EZLog.EnableLevel(LogLevel.Exception);
                LogMessage("已启用Exception级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                EZLog.DisableAll();
                LogMessage("已禁用所有级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                EZLog.EnableAll();
                LogMessage("已启用所有级别");
                ShowCurrentStatus();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                RunLogTests();
            }
        }

        /// <summary>
        /// 显示当前状态
        /// </summary>
        private void ShowCurrentStatus()
        {
            var currentLevels = EZLog.GetEnabledLevels();
            LogMessage($"当前启用级别: {currentLevels}");

            // 显示各级别状态
            LogMessage($"Log: {(HasLogLevel(currentLevels, LogLevel.Log) ? "✓" : "✗")} " +
                      $"Warning: {(HasLogLevel(currentLevels, LogLevel.Warning) ? "✓" : "✗")} " +
                      $"Assert: {(HasLogLevel(currentLevels, LogLevel.Assert) ? "✓" : "✗")} " +
                      $"Error: {(HasLogLevel(currentLevels, LogLevel.Error) ? "✓" : "✗")} " +
                      $"Exception: {(HasLogLevel(currentLevels, LogLevel.Exception) ? "✓" : "✗")}");
        }

        /// <summary>
        /// 检查日志级别是否包含指定级别
        /// </summary>
        private bool HasLogLevel(LogLevel levels, LogLevel target)
        {
            return (levels & target) == target;
        }

        /// <summary>
        /// 运行日志测试
        /// </summary>
        private void RunLogTests()
        {
            _testCounter++;
            LogMessage($"=== 开始运行测试 #{_testCounter} ===");

            // 测试所有级别的日志
            EZLog.Log?.Log("测试", $"这是一条Log级别的测试消息 #{_testCounter}");
            EZLog.Warning?.Log("测试", $"这是一条Warning级别的测试消息 #{_testCounter}");
            EZLog.Assert?.Log("测试", $"这是一条Assert级别的测试消息 #{_testCounter}");
            EZLog.Error?.Log("测试", $"这是一条Error级别的测试消息 #{_testCounter}");
            EZLog.Exception?.Log("测试", $"这是一条Exception级别的测试消息 #{_testCounter}");

            // 测试格式化日志
            EZLog.Log?.LogFormat("格式化测试", "测试参数: count={0}, time={1:F2}", _testCounter, Time.time);

            // 测试字符串插值（推荐方式）
            EZLog.Warning?.Log("性能测试", $"字符串插值测试: {_testCounter} 次调用");

            // 测试零开销特性
            EZLog.Log?.Log("零开销测试", GetExpensiveString());

            LogMessage($"=== 测试 #{_testCounter} 完成 ===");
        }

        /// <summary>
        /// 运行自动演示
        /// </summary>
        private void RunAutoDemo()
        {
            LogMessage("=== 自动演示 ===");

            // 演示动态级别控制
            var demoSteps = new System.Action[]
            {
                () => { EZLog.EnableAll(); LogMessage("演示: 启用所有级别"); },
                () => { EZLog.SetWarningAndAbove(); LogMessage("演示: 仅警告及以上"); },
                () => { EZLog.SetErrorAndAbove(); LogMessage("演示: 仅错误及以上"); },
                () => { EZLog.EnableAll(); LogMessage("演示: 重新启用所有级别"); }
            };

            var stepIndex = ((int)(Time.time / demoInterval)) % demoSteps.Length;
            demoSteps[stepIndex]();

            // 运行测试
            RunLogTests();
        }

        /// <summary>
        /// 模拟昂贵的字符串操作（用于测试零开销特性）
        /// </summary>
        private string GetExpensiveString()
        {
            var result = "";
            for (int i = 0; i < 50; i++)
            {
                result += $"昂贵操作{i} ";
            }
            return result;
        }

        /// <summary>
        /// 记录测试消息（使用Unity原生Debug，确保总是显示）
        /// </summary>
        private void LogMessage(string message)
        {
            Debug.Log($"[SimpleRuntimeTester] {message}");
        }

        /// <summary>
        /// 演示零开销特性
        /// </summary>
        private void DemonstrateZeroCostFeature()
        {
            LogMessage("=== 零开销特性演示 ===");

            // 禁用Log级别
            EZLog.DisableLevel(LogLevel.Log);
            LogMessage("已禁用Log级别");

            // 这些调用在Log级别被禁用时不会产生任何性能开销
            // 甚至GetExpensiveString()方法都不会被调用
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < 1000; i++)
            {
                EZLog.Log?.Log("性能测试", GetExpensiveString());
            }

            stopwatch.Stop();
            LogMessage($"禁用状态下1000次调用耗时: {stopwatch.ElapsedMilliseconds}ms");

            // 重新启用Log级别
            EZLog.EnableLevel(LogLevel.Log);
            LogMessage("已重新启用Log级别");

            stopwatch.Restart();

            for (int i = 0; i < 10; i++) // 减少次数，因为启用时会有真实开销
            {
                EZLog.Log?.Log("性能测试", GetExpensiveString());
            }

            stopwatch.Stop();
            LogMessage($"启用状态下10次调用耗时: {stopwatch.ElapsedMilliseconds}ms");
            LogMessage("对比可见禁用时的零开销特性");
        }

        /// <summary>
        /// 演示系统集成
        /// </summary>
        private void DemonstrateSystemIntegration()
        {
            LogMessage("=== 系统集成演示 ===");

            // 演示系统日志监控
            EZLog.EnableSystemLogMonitor(true);
            LogMessage("已启用系统日志监控");

            // 演示服务器上报（仅配置，不实际发送）
            EZLog.SetServerReportUrl("https://example.com/api/error-report");
            EZLog.EnableServerReporting(false); // 演示环境不实际发送
            LogMessage("已配置服务器上报URL（演示模式）");

            // 模拟一些系统事件
            LogMessage("模拟系统错误...");
            EZLog.Error?.Log("System", "模拟的系统错误，用于演示上报功能");
        }

        private void OnGUI()
        {
            // 显示简单的状态信息
            var guiStyle = new GUIStyle(GUI.skin.box);
            guiStyle.fontSize = 12;
            guiStyle.normal.textColor = Color.white;

            var currentLevels = EZLog.GetEnabledLevels();
            var statusText = $"EZ Logger Status: {currentLevels}\n" +
                           $"Test Count: {_testCounter}\n" +
                           $"Press T to test, 1-5 to toggle levels";

            GUI.Box(new Rect(10, 10, 300, 80), statusText, guiStyle);
        }
    }
}
