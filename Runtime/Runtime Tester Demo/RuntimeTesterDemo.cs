using UnityEngine;
using EZLogger;


namespace EZLoggerSamples
{
    /// <summary>
    /// EZ Logger 运行时测试器演示脚本
    /// 展示如何在实际项目中集成和使用运行时配置测试功能
    /// 
    /// 推荐使用 SimpleRuntimeTester 避免UI依赖问题
    /// </summary>
    public class RuntimeTesterDemo : MonoBehaviour
    {
        [Header("测试器设置")]
        [Tooltip("运行时测试器预制件或组件")]
        public EZLoggerSamples.EZLoggerRuntimeTester runtimeTester;

        [Header("推荐设置")]
        [Tooltip("使用简化版测试器（推荐）")]
        public bool useSimpleRuntimeTester = true;


        [Tooltip("演示间隔时间")]
        public float demoInterval = 3f;

        [Header("模拟应用场景")]
        [Tooltip("是否模拟游戏事件")]
        public bool simulateGameEvents = true;

        [Tooltip("事件触发间隔")]
        public float eventInterval = 5f;

        // 演示计时器
        private float _lastDemoTime = 0f;
        private float _lastEventTime = 0f;
        private int _demoCounter = 0;
        private int _eventCounter = 0;

        // 模拟的游戏系统
        private SimulatedPlayer _player;
        private SimulatedGameManager _gameManager;

        private void Start()
        {
            // 初始化模拟的游戏系统
            InitializeSimulatedSystems();

            // 记录启动信息
            EZLog.Log?.Log("Demo", "EZ Logger 运行时测试器演示已启动");
            EZLog.Log?.Log("Demo", $"当前启用级别: {EZLog.GetEnabledLevels()}");

            // 显示使用说明
            ShowUsageInstructions();

            // 延迟创建测试器，避免UI重建循环问题
            StartCoroutine(DelayedInitializeTester());
        }

        /// <summary>
        /// 延迟初始化测试器，避免UI重建循环问题
        /// </summary>
        private System.Collections.IEnumerator DelayedInitializeTester()
        {
            // 等待一帧，确保当前帧的UI更新完成
            yield return null;

            // 检查运行时测试器
            if (runtimeTester == null)
            {
                runtimeTester = FindObjectOfType<EZLoggerSamples.EZLoggerRuntimeTester>();
            }

            if (runtimeTester == null)
            {
                CreateRuntimeTester();
            }
        }

        private void Update()
        {
            var currentTime = Time.time;

            // // 模拟游戏事件
            // if (simulateGameEvents && currentTime - _lastEventTime > eventInterval)
            // {
            //     SimulateGameEvent();
            //     _lastEventTime = currentTime;
            // }

            // // 更新模拟系统
            // UpdateSimulatedSystems();
        }

        /// <summary>
        /// 初始化模拟的游戏系统
        /// </summary>
        private void InitializeSimulatedSystems()
        {
            _player = new SimulatedPlayer();
            _gameManager = new SimulatedGameManager();

            EZLog.Log?.Log("Systems", "模拟游戏系统已初始化");
        }

        /// <summary>
        /// 创建运行时测试器
        /// </summary>
        private void CreateRuntimeTester()
        {
            if (useSimpleRuntimeTester)
            {
                // 使用简化版测试器（推荐）
                var testerGO = new GameObject("EZ Logger Simple Runtime Tester");
                testerGO.AddComponent<EZLoggerSamples.SimpleRuntimeTester>();
                EZLog.Log?.Log("Demo", "简化版运行时测试器已自动创建（推荐）");
                EZLog.Log?.Log("Demo", "按数字键1-5切换日志级别，按T键运行测试");
            }
            else
            {
                // 使用完整版测试器（需要UI模块）
                var testerGO = new GameObject("EZ Logger Runtime Tester");
                runtimeTester = testerGO.AddComponent<EZLoggerSamples.EZLoggerRuntimeTester>();

                // 配置测试器
                runtimeTester.showOnStart = true;
                runtimeTester.toggleKey = KeyCode.F12;

                EZLog.Log?.Log("Demo", "完整版运行时测试器已自动创建");
                EZLog.Log?.Log("Demo", "按F12显示/隐藏测试面板");
            }
        }

        /// <summary>
        /// 显示使用说明
        /// </summary>
        private void ShowUsageInstructions()
        {
            EZLog.Log?.Log("说明", "=== EZ Logger 运行时测试器使用说明 ===");
            EZLog.Log?.Log("说明", "1. 按 F12 键显示/隐藏测试面板");
            EZLog.Log?.Log("说明", "2. 在测试面板中可以实时修改日志配置");
            EZLog.Log?.Log("说明", "3. 点击 'Test Logs' 按钮测试各级别日志");
            EZLog.Log?.Log("说明", "4. 修改配置后会立即生效，无需重启");
            EZLog.Log?.Log("说明", "5. 所有配置修改都支持运行时动态调整");
            EZLog.Log?.Log("说明", "=====================================");
        }

        /// <summary>
        /// 运行演示
        /// </summary>
        private void RunDemo()
        {
            _demoCounter++;

            EZLog.Log?.Log("Demo", $"=== 演示 #{_demoCounter} ===");

            // 演示不同级别的日志
            DemonstrateLogLevels();

            // 演示配置变更效果
            if (_demoCounter % 3 == 0)
            {
                DemonstrateConfigurationChanges();
            }

            // 演示零开销特性
            if (_demoCounter % 5 == 0)
            {
                DemonstrateZeroCostFeature();
            }
        }

        /// <summary>
        /// 演示不同级别的日志
        /// </summary>
        private void DemonstrateLogLevels()
        {
            EZLog.Log?.Log("Demo", $"这是Log级别消息 - 演示 #{_demoCounter}");
            EZLog.Warning?.Log("Demo", $"这是Warning级别消息 - 演示 #{_demoCounter}");

            // 模拟条件错误
            if (_demoCounter % 4 == 0)
            {
                EZLog.Error?.Log("Demo", $"模拟错误消息 - 演示 #{_demoCounter}");
            }

            // 模拟条件断言
            if (_demoCounter % 7 == 0)
            {
                EZLog.Assert?.Log("Demo", $"模拟断言消息 - 演示 #{_demoCounter}");
            }

            // 演示格式化日志
            EZLog.Log?.LogFormat("Demo", "格式化日志: count={0}, time={1:F2}", _demoCounter, Time.time);

            // 演示字符串插值（推荐方式）
            EZLog.Log?.Log("Demo", $"字符串插值日志: {_demoCounter} 次演示，时间 {Time.time:F2}s");
        }

        /// <summary>
        /// 演示配置变更效果
        /// </summary>
        private void DemonstrateConfigurationChanges()
        {
            EZLog.Log?.Log("Config", "演示配置变更效果...");

            // 获取当前级别
            var currentLevels = EZLog.GetEnabledLevels();
            EZLog.Log?.Log("Config", $"当前启用级别: {currentLevels}");

            // 临时修改级别（仅作演示，实际应用中建议通过UI操作）
            if (_demoCounter % 6 == 0)
            {
                EZLog.Log?.Log("Config", "提示: 可以通过测试面板实时修改日志级别");
                EZLog.Log?.Log("Config", "修改后的配置会立即生效，无需重启应用");
            }
        }

        /// <summary>
        /// 演示零开销特性
        /// </summary>
        private void DemonstrateZeroCostFeature()
        {
            EZLog.Log?.Log("Performance", "演示零开销特性...");

            // 这些调用在级别被禁用时不会产生任何性能开销
            EZLog.Log?.Log("Performance", GetExpensiveComputationResult());
            EZLog.Warning?.Log("Performance", ComputeComplexString());

            EZLog.Log?.Log("Performance", "提示: 即使禁用相应级别，上述昂贵操作也不会执行");
        }

        /// <summary>
        /// 模拟昂贵的计算结果
        /// </summary>
        private string GetExpensiveComputationResult()
        {
            // 在实际应用中，这里可能是复杂的字符串构建、数据库查询等
            var result = "";
            for (int i = 0; i < 50; i++)
            {
                result += $"计算步骤{i} ";
            }
            return $"昂贵计算结果: {result}";
        }

        /// <summary>
        /// 计算复杂字符串
        /// </summary>
        private string ComputeComplexString()
        {
            return $"复杂字符串: 时间={System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}, " +
                   $"帧数={Time.frameCount}, 内存={System.GC.GetTotalMemory(false) / 1024}KB";
        }

        /// <summary>
        /// 模拟游戏事件
        /// </summary>
        private void SimulateGameEvent()
        {
            _eventCounter++;

            var eventType = _eventCounter % 5;
            switch (eventType)
            {
                case 0:
                    _player.OnLevelUp();
                    break;
                case 1:
                    _player.OnItemCollected("金币");
                    break;
                case 2:
                    _gameManager.OnWaveCompleted(_eventCounter / 5 + 1);
                    break;
                case 3:
                    _player.OnSkillUsed("火球术");
                    break;
                case 4:
                    _gameManager.OnSpecialEvent("随机事件");
                    break;
            }
        }

        /// <summary>
        /// 更新模拟系统
        /// </summary>
        private void UpdateSimulatedSystems()
        {
            _player?.Update();
            _gameManager?.Update();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            EZLog.Log?.Log("Lifecycle", $"应用暂停状态变更: {pauseStatus}");
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            EZLog.Log?.Log("Lifecycle", $"应用焦点状态变更: {hasFocus}");
        }

        private void OnDestroy()
        {
            EZLog.Log?.Log("Demo", "运行时测试器演示结束");
        }
    }

    /// <summary>
    /// 模拟的玩家系统
    /// </summary>
    public class SimulatedPlayer
    {
        private int _level = 1;
        private int _experience = 0;
        private float _health = 100f;
        private float _mana = 50f;

        public void Update()
        {
            // 模拟生命值和法力值恢复
            _health = Mathf.Min(100f, _health + Time.deltaTime * 2f);
            _mana = Mathf.Min(50f, _mana + Time.deltaTime * 1f);
        }

        public void OnLevelUp()
        {
            _level++;
            EZLog.Log?.Log("Player", $"玩家升级! 新等级: {_level}");
            EZLog.Log?.Log("Player", $"属性提升: 生命值+20, 法力值+10");
        }

        public void OnItemCollected(string itemName)
        {
            EZLog.Log?.Log("Player", $"收集物品: {itemName}");

            _experience += 10;
            if (_experience >= _level * 100)
            {
                OnLevelUp();
                _experience = 0;
            }
        }

        public void OnSkillUsed(string skillName)
        {
            if (_mana >= 10f)
            {
                _mana -= 10f;
                EZLog.Log?.Log("Player", $"使用技能: {skillName} (剩余法力: {_mana:F1})");
            }
            else
            {
                EZLog.Warning?.Log("Player", $"法力不足，无法使用技能: {skillName}");
            }
        }

        public void TakeDamage(float damage)
        {
            _health -= damage;
            EZLog.Warning?.Log("Player", $"玩家受伤: -{damage} (剩余生命: {_health:F1})");

            if (_health <= 0)
            {
                EZLog.Error?.Log("Player", "玩家死亡!");
                _health = 0;
            }
        }
    }

    /// <summary>
    /// 模拟的游戏管理器
    /// </summary>
    public class SimulatedGameManager
    {
        private int _currentWave = 0;
        private float _gameTime = 0f;
        private int _score = 0;

        public void Update()
        {
            _gameTime += Time.deltaTime;
        }

        public void OnWaveCompleted(int waveNumber)
        {
            _currentWave = waveNumber;
            _score += waveNumber * 100;

            EZLog.Log?.Log("GameManager", $"第 {waveNumber} 波完成! 获得分数: {waveNumber * 100}");
            EZLog.Log?.Log("GameManager", $"总分数: {_score}, 游戏时间: {_gameTime:F1}s");

            if (waveNumber % 5 == 0)
            {
                EZLog.Warning?.Log("GameManager", $"Boss波次 {waveNumber} 完成! 获得特殊奖励!");
            }
        }

        public void OnSpecialEvent(string eventName)
        {
            EZLog.Log?.Log("GameManager", $"特殊事件触发: {eventName}");

            var eventTypes = new[]
            {
                "双倍经验",
                "金币雨",
                "随机传送",
                "时间加速",
                "全屏攻击"
            };

            var randomEvent = eventTypes[UnityEngine.Random.Range(0, eventTypes.Length)];
            EZLog.Log?.Log("GameManager", $"随机事件效果: {randomEvent}");
        }

        public void OnGameOver()
        {
            EZLog.Error?.Log("GameManager", "游戏结束!");
            EZLog.Log?.Log("GameManager", $"最终分数: {_score}");
            EZLog.Log?.Log("GameManager", $"游戏时长: {_gameTime:F1}s");
            EZLog.Log?.Log("GameManager", $"到达波次: {_currentWave}");
        }
    }
}
