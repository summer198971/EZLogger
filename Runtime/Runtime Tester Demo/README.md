# EZ Logger 运行时配置测试器

这个示例展示了如何在Unity应用中集成和使用EZ Logger的运行时配置测试功能。运行时测试器允许你在打包后的应用中实时修改日志配置，用于调试和性能测试。

## 🚀 功能特点

### 运行时动态配置
- **日志级别控制**: 实时启用/禁用各种日志级别
- **输出器管理**: 动态开关Unity控制台、文件输出等
- **功能开关**: 堆栈跟踪、系统监控等

### 零开销验证
- **性能测试**: 验证禁用级别时的零开销特性
- **实时监控**: 查看当前配置状态和Logger类型
- **压力测试**: 批量日志测试功能

### 打包后可用
- **生产环境**: 支持在正式发布的应用中使用
- **热键操作**: F12快速显示/隐藏测试面板
- **持久化配置**: 配置修改立即生效

## 📦 集成步骤

⚠️ **重要提醒**: 由于Unity Samples~目录的限制，`EZLoggerRuntimeTester.cs` 在Samples目录中会显示编译错误，这是正常现象。我们提供了两种解决方案：

**推荐方案**: 使用 `SimpleRuntimeTester.cs`（无UI依赖，开箱即用）
**完整方案**: 复制 `EZLoggerRuntimeTester.cs` 到项目中（需要Unity UI模块）

### 1. 添加运行时测试器

#### 方式A: 简化版测试器（推荐）

**优势**: 无UI依赖、开箱即用、避免编译错误

```csharp
// 使用SimpleRuntimeTester，不需要UI模块
// 1. 复制 SimpleRuntimeTester.cs 到你的项目
// 2. 添加到GameObject：
gameObject.AddComponent<EZLoggerSamples.SimpleRuntimeTester>();

// 控制方式：
// 按数字键1-5切换日志级别
// 按T键运行测试
// 按0禁用所有级别，按9启用所有级别
```

**特性**:
- ✅ 无需Unity UI模块
- ✅ 零编译错误
- ✅ 键盘快捷操作
- ✅ 控制台显示状态
- ✅ 完整的测试功能


#### 方式B: 完整UI测试器

**仅在需要图形界面时使用**

**步骤1**: 复制文件到项目中
```
1. 复制 Samples~/Runtime Tester Demo/EZLoggerRuntimeTester.cs 到你的项目中
2. 复制 Samples~/Runtime Tester Demo/RuntimeTesterDemo.cs 到你的项目中
3. 建议放在 Assets/Scripts/Logging/ 目录下
```

**步骤2**: 确保Unity UI模块引用
确保你的项目包含以下模块：
- **UnityEngine.UI** (通过Window > Package Manager > Unity Registry > UI Toolkit 安装)
- **Unity Legacy UI** (如果使用旧版Unity)

**步骤3**: 使用示例代码
```csharp
using EZLoggerSamples;  // 引用示例命名空间

public class YourGameManager : MonoBehaviour 
{
    private void Start() 
    {
        // 自动创建运行时测试器
        var testerGO = new GameObject("EZ Logger Runtime Tester");
        var tester = testerGO.AddComponent<EZLoggerRuntimeTester>();
        tester.showOnStart = true;  // 启动时显示
        tester.toggleKey = KeyCode.F12;  // F12切换显示
    }
}
```

#### 方式C: 预制件集成
1. 将 `EZLoggerRuntimeTester.cs` 复制到你的项目中
2. 创建空的GameObject
3. 添加 `EZLoggerSamples.EZLoggerRuntimeTester` 组件
4. 配置相关参数
5. 保存为预制件供复用

### 2. 基础配置

```csharp
public class GameInitializer : MonoBehaviour 
{
    private void Awake() 
    {
        // 初始化EZ Logger
        EZLog.EnableAll();  // 启用所有日志级别
        
        // 可选：配置服务器上报
        EZLog.SetServerReportUrl("https://your-server.com/api/logs");
        EZLog.EnableServerReporting(true);
        
        // 可选：启用系统日志监控
        EZLog.EnableSystemLogMonitor(true);
    }
}
```

## 🎮 使用方法

### 运行时操作

1. **显示测试面板**: 按 `F12` 键
2. **修改日志级别**: 在"日志级别控制"区域切换开关
3. **调整功能设置**: 在"功能开关"区域修改配置
4. **测试日志输出**: 点击"Test Logs"按钮
5. **查看状态信息**: 在"运行状态"区域查看当前配置

### 测试场景

运行 `RuntimeTesterDemo` 场景可以看到：

- **自动演示**: 每3秒自动输出不同级别的日志
- **模拟游戏事件**: 玩家升级、物品收集、技能使用等
- **性能测试**: 展示零开销特性的效果
- **配置变更**: 实时查看配置修改的效果

## 🔧 高级配置

### 自定义UI布局

可以继承 `EZLoggerSamples.EZLoggerRuntimeTester` 并重写UI创建方法：

```csharp
using EZLoggerSamples;

public class CustomRuntimeTester : EZLoggerRuntimeTester 
{
    protected override void CreateUIControls() 
    {
        base.CreateUIControls();
        
        // 添加自定义控件
        CreateCustomSection();
    }
    
    private void CreateCustomSection() 
    {
        // 自定义UI实现
    }
}
```

### 配置持久化

```csharp
public class ConfigPersistence : MonoBehaviour 
{
    private void Start() 
    {
        // 加载保存的配置
        LoadConfiguration();
    }
    
    private void OnApplicationPause(bool pause) 
    {
        if (pause) 
        {
            // 保存当前配置
            SaveConfiguration();
        }
    }
    
    private void LoadConfiguration() 
    {
        var savedLevels = (LogLevel)PlayerPrefs.GetInt("EZLogger_Levels", (int)LogLevel.All);
        EZLog.SetEnabledLevels(savedLevels);
    }
    
    private void SaveConfiguration() 
    {
        PlayerPrefs.SetInt("EZLogger_Levels", (int)EZLog.GetEnabledLevels());
        PlayerPrefs.Save();
    }
}
```

## 📊 性能测试

### 零开销验证

```csharp
public class PerformanceTest : MonoBehaviour 
{
    private void TestZeroCost() 
    {
        // 禁用Log级别
        EZLog.DisableLevel(LogLevel.Log);
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // 这些调用应该接近零开销
        for (int i = 0; i < 100000; i++) 
        {
            EZLog.Log?.Log("Test", ExpensiveOperation());
        }
        
        stopwatch.Stop();
        Debug.Log($"禁用状态下100k次调用耗时: {stopwatch.ElapsedMilliseconds}ms");
    }
    
    private string ExpensiveOperation() 
    {
        // 模拟昂贵操作
        return $"Complex calculation: {Time.time * UnityEngine.Random.value}";
    }
}
```

### 性能对比测试

```csharp
public class PerformanceComparison : MonoBehaviour 
{
    [ContextMenu("Run Performance Test")]
    private void RunPerformanceTest() 
    {
        const int iterations = 50000;
        
        // 测试启用状态
        EZLog.EnableLevel(LogLevel.Log);
        var enabledTime = MeasureLogPerformance(iterations);
        
        // 测试禁用状态
        EZLog.DisableLevel(LogLevel.Log);
        var disabledTime = MeasureLogPerformance(iterations);
        
        Debug.Log($"启用: {enabledTime}ms, 禁用: {disabledTime}ms, 比率: {enabledTime / (float)disabledTime:F1}x");
    }
    
    private long MeasureLogPerformance(int iterations) 
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++) 
        {
            EZLog.Log?.LogFormat("Perf", "Test {0}: {1}", i, Time.time);
        }
        
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}
```

## 🚨 注意事项

### 生产环境使用

1. **安全考虑**: 确保测试面板在发布版本中有适当的访问控制
2. **性能影响**: 虽然UI本身开销很小，但建议在正式版本中考虑是否保留
3. **用户体验**: 提供合适的快捷键，避免与游戏操作冲突

### 内存管理

```csharp
using EZLoggerSamples;

public class TestMemoryManagement : MonoBehaviour 
{
    private EZLoggerRuntimeTester tester;
    
    private void Start() 
    {
        tester = GetComponent<EZLoggerRuntimeTester>();
    }
    
    private void OnDestroy() 
    {
        // 测试器会自动清理，但可以手动确保
        if (tester != null) 
        {
            // 清理工作已在测试器内部完成
        }
    }
}
```

### 跨平台支持

- **移动平台**: 支持触屏操作和虚拟键盘
- **WebGL**: 支持浏览器环境，部分功能可能受限
- **主机平台**: 支持手柄操作（需要自定义输入处理）

## 🎯 最佳实践

1. **开发阶段**: 保持测试器启用，便于调试
2. **测试阶段**: 使用测试器验证不同配置下的性能表现
3. **发布准备**: 考虑是否在正式版本中保留测试功能
4. **用户反馈**: 可以作为高级用户的调试工具

## 🔗 相关链接

- [EZ Logger 主要文档](../../README.md)
- [API参考文档](../../Documentation~/api-reference.md)
- [性能优化指南](../../Documentation~/performance-guide.md)
- [更多示例](../README.md)
