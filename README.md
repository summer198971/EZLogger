# EZ Logger

EZ Logger 是一个为Unity设计的高性能、零分配日志系统，支持多级别日志控制和服务器集成。

## ✨ 特性

- 🚀 **零GC分配**: 性能模式下确保零垃圾回收分配
- 📊 **多级别控制**: 支持5个日志级别，与Unity LogType完全对齐
- 🔄 **异步写入**: 支持异步文件写入，不阻塞主线程
- 📁 **文件轮转**: 自动文件大小管理和轮转
- 🌐 **服务器集成**: 支持日志发送到远程服务器
- 🔧 **可扩展**: 插件化架构，支持自定义输出器
- 🎯 **Unity优化**: 专为Unity环境优化，支持帧数、堆栈跟踪等

## 📦 安装

### 通过Unity Package Manager安装

1. 打开Unity编辑器
2. 在菜单栏选择 **Window > Package Manager**
3. 点击左上角的 **+** 按钮
4. 选择 **Add package from git URL**
5. 输入仓库URL：`https://github.com/summer198971/EZLogger.git` 并点击Add

### 手动安装

1. 下载最新版本的源码
2. 将整个 `EZLogger` 文件夹复制到项目的 `Packages` 目录下

## 🚀 快速开始

### 🚀 零开销日志记录 (与Unity LogType完全对齐)

EZ Logger 提供真正的零开销日志记录，**与Unity LogType完全对齐**，消除使用混乱：

```csharp
using EZLogger;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // ✅ 与Unity LogType完全对齐的零开销日志记录
        EZLog.Log?.Log("GameManager", "普通日志 (对应Unity LogType.Log)");
        EZLog.Warning?.Log("GameManager", "警告信息 (对应Unity LogType.Warning)");
        EZLog.Assert?.Log("GameManager", "断言信息 (对应Unity LogType.Assert)");
        EZLog.Error?.Log("GameManager", "错误信息 (对应Unity LogType.Error)");
        EZLog.Exception?.Log("GameManager", "异常信息 (对应Unity LogType.Exception)");
        
        // ✅ 使用对象作为标签
        EZLog.Log?.Log(this, "使用GameObject作为标签");
        
        // ✅ 零开销的格式化日志
        EZLog.Log?.LogFormat("Player", "玩家位置: {0}", transform.position);
    }
    
    void Update()
    {
        // ✅ 高频更新中的零开销日志
        // 当Log级别被禁用时，这行代码没有任何开销，包括参数计算！
        EZLog.Log?.Log("Update", $"Player position: {transform.position}");
        
        // ✅ 复杂计算的零开销保护
        EZLog.Warning?.Log("Performance", GetExpensiveDebugInfo());
    }
    
    // 这个方法只有在Warning级别启用时才会被调用
    string GetExpensiveDebugInfo()
    {
        // 复杂的调试信息生成
        return $"复杂调试信息: {System.DateTime.Now}";
    }
}
```

### 🎯 Unity LogType对齐

| EZ Logger | Unity LogType | 使用场景 |
|-----------|---------------|----------|
| `EZLog.Log` | `LogType.Log` | 普通日志消息 |
| `EZLog.Warning` | `LogType.Warning` | 警告消息 |
| `EZLog.Assert` | `LogType.Assert` | 断言失败 |
| `EZLog.Error` | `LogType.Error` | 错误消息 |
| `EZLog.Exception` | `LogType.Exception` | 异常信息 |

### 🔄 Unity filterLogType兼容

EZ Logger完全兼容Unity的filterLogType过滤行为：

```csharp
// Unity方式
Debug.unityLogger.filterLogType = LogType.Warning; // 显示Warning及以上

// EZ Logger对应方式
EZLog.SetWarningAndAbove(); // 完全相同的过滤效果

// 动态切换过滤级别
EZLog.EnableAll();           // 对应filterLogType = LogType.Log
EZLog.SetWarningAndAbove();  // 对应filterLogType = LogType.Warning  
EZLog.SetErrorAndAbove();    // 对应filterLogType = LogType.Error
```

### 传统方式（向后兼容）

```csharp
// 传统方式 - 与Unity LogType对齐
EZLog.LogLog("GameManager", "普通日志");
EZLog.LogWarning("GameManager", "警告信息");  
EZLog.LogAssert("GameManager", "断言信息");
EZLog.LogError("GameManager", "错误信息");
EZLog.LogException("GameManager", "异常信息");

// 或者使用级别检查
if (EZLog.IsLevelEnabled(LogLevel.Log))
{
    EZLog.LogLog("GameManager", $"Player position: {transform.position}");
}
```

### 格式化日志和异常记录

```csharp
// ✅ 零开销的格式化日志 - 与Unity LogType对齐
EZLog.Log?.LogFormat("Battle", "玩家 {0} 获得 {1} 经验", playerName, expGained);
EZLog.Warning?.LogFormat("Network", "连接超时: {0}ms", timeoutMs);

// ✅ 异常记录 - 与Unity LogException完全对应
try
{
    // 一些可能出错的代码
}
catch (Exception ex)
{
    // 方式1: 使用零开销API
    EZLog.Exception?.Log("DataSystem", ex.Message);
    
    // 方式2: 使用传统API (与Unity LogException对应)
    EZLog.LogException("DataSystem", ex);
}
```

## ⚙️ 配置

### 创建自定义配置

```csharp
// 开发环境配置
var devConfig = LoggerConfiguration.CreateDevelopment();
devConfig.GlobalEnabledLevels = LogLevel.All;
devConfig.PerformanceMode = false;

// 发布环境配置
var releaseConfig = LoggerConfiguration.CreateRelease();
releaseConfig.GlobalEnabledLevels = LogLevel.ErrorAndWarning;
releaseConfig.PerformanceMode = true;

// 应用配置
EZLoggerManager.Instance.Configuration = devConfig;
```

### 🎮 运行时动态级别控制

EZ Logger 支持运行时动态开关任意级别的日志，**立即生效**：

```csharp
// ✅ 便捷的级别控制方法 - 与Unity filterLogType对齐
EZLog.EnableAll();           // 启用所有级别 (对应filterLogType = LogType.Log)
EZLog.DisableAll();          // 禁用所有级别
EZLog.SetWarningAndAbove();  // 启用警告及以上 (对应filterLogType = LogType.Warning)
EZLog.SetErrorAndAbove();    // 启用错误及以上 (对应filterLogType = LogType.Error)

// ✅ 单个级别控制
EZLog.EnableLevel(LogLevel.Assert);   // 启用Assert级别
EZLog.DisableLevel(LogLevel.Log);     // 禁用Log级别
EZLog.ToggleLevel(LogLevel.Warning);  // 切换Warning级别

// ✅ 直接设置级别组合
EZLog.SetEnabledLevels(LogLevel.Warning | LogLevel.Error | LogLevel.Exception);

// ✅ 查询当前状态
LogLevel current = EZLog.GetEnabledLevels();
bool logEnabled = EZLog.IsLevelEnabled(LogLevel.Log);
```

### 💡 动态控制的实际应用

```csharp
public class GameDebugController : MonoBehaviour
{
    void Update()
    {
        // 开发模式快捷键
        if (Input.GetKeyDown(KeyCode.F1))
        {
            EZLog.ToggleLevel(LogLevel.Log);
            EZLog.Log?.LogFormat("Debug", "Log级别已{0}", 
                EZLog.IsLevelEnabled(LogLevel.Log) ? "启用" : "禁用");
        }
        
        // 性能测试时禁用详细日志 (对应Unity filterLogType = LogType.Error)
        if (performanceTestMode)
        {
            EZLog.SetErrorAndAbove();
        }
        
        // 根据网络状态调整日志级别 (对应Unity filterLogType = LogType.Warning)
        if (NetworkReachability.NotReachable == Application.internetReachability)
        {
            EZLog.SetWarningAndAbove(); // 网络断开时减少日志
        }
        
        // 与Unity Logger保持同步
        if (Debug.unityLogger.filterLogType == LogType.Warning)
        {
            EZLog.SetWarningAndAbove(); // 保持一致的过滤行为
        }
    }
}
```

### 文件输出配置

```csharp
var config = LoggerConfiguration.CreateDefault();
config.FileOutput.Enabled = true;
config.FileOutput.LogDirectory = "GameLogs";
config.FileOutput.MaxFileSize = 5 * 1024 * 1024; // 5MB
config.FileOutput.EnableSizeCheck = true;

EZLoggerManager.Instance.Configuration = config;
```

## 🔧 高级功能

### 添加自定义输出器

```csharp
// 添加文件输出器
var fileAppender = new FileAppender();
fileAppender.Initialize(config.FileOutput);
EZLoggerManager.Instance.AddAppender(fileAppender);

// 添加服务器输出器（需要实现）
var serverAppender = new ServerAppender();
serverAppender.Initialize(config.ServerOutput);
EZLoggerManager.Instance.AddAppender(serverAppender);
```

### 条件日志

```csharp
// 只在Debug模式下记录
#if DEBUG
    EZLog.Log?.Log("Debug", "这只在Debug构建中显示");
#endif

// 运行时级别检查
if (EZLog.IsLevelEnabled(LogLevel.Log))
{
    string expensiveDebugInfo = GenerateExpensiveDebugInfo();
    EZLog.Log?.Log("Performance", expensiveDebugInfo);
}
```

## 📊 性能优化

### 零GC分配

EZ Logger 在性能模式下确保零GC分配：

- 使用对象池复用StringBuilder
- 预先检查日志级别
- 异步队列避免阻塞
- 优化的字符串格式化

### 最佳实践

```csharp
// ✅ 最佳做法：零开销日志记录 - 与Unity LogType对齐
EZLog.Log?.Log("AI", $"寻路计算耗时: {pathfindingTime}ms");
// 当Log级别被禁用时，连字符串拼接都不会执行！

// ✅ 零开销的格式化方法
EZLog.Log?.LogFormat("AI", "寻路计算耗时: {0}ms", pathfindingTime);

// ✅ 复杂计算的零开销保护
EZLog.Warning?.Log("AI", GetComplexAIDebugInfo());
// GetComplexAIDebugInfo() 只有在Warning级别启用时才会被调用

// ❌ 避免的做法 - 传统方式仍有开销
if (EZLog.IsLevelEnabled(LogLevel.Log))
{
    EZLog.LogLog("AI", $"寻路计算耗时: {pathfindingTime}ms");
}

// ❌ 更要避免的做法 - 总是有开销
EZLog.LogLog("AI", $"寻路计算耗时: {pathfindingTime}ms");
```

## 🔍 日志级别（与Unity LogType完全对齐）

| EZ Logger级别 | Unity LogType | 用途 | 开发版本 | 发布版本 |
|---------------|---------------|------|----------|----------|
| `LogLevel.Log` | `LogType.Log` | 普通日志消息 | ✅ | ✅ |
| `LogLevel.Warning` | `LogType.Warning` | 警告消息 | ✅ | ✅ |
| `LogLevel.Assert` | `LogType.Assert` | 断言失败 | ✅ | ❌ |
| `LogLevel.Error` | `LogType.Error` | 错误消息 | ✅ | ✅ |
| `LogLevel.Exception` | `LogType.Exception` | 异常信息 | ✅ | ✅ |

### Unity filterLogType 兼容性

| Unity设置 | EZ Logger对应方法 | 显示级别 |
|-----------|-------------------|----------|
| `filterLogType = LogType.Log` | `EZLog.EnableAll()` | 显示所有级别 |
| `filterLogType = LogType.Warning` | `EZLog.SetWarningAndAbove()` | Warning + Assert + Error + Exception |
| `filterLogType = LogType.Error` | `EZLog.SetErrorAndAbove()` | Error + Exception |
| `filterLogType = LogType.Exception` | `EZLog.SetEnabledLevels(LogLevel.Exception)` | 仅Exception |

## 🛠️ 开发工具

### Unity编辑器集成

- 日志查看器窗口
- 实时日志过滤
- 配置设置界面
- 性能监控面板

## 📄 许可证

MIT License - 详见 [LICENSE.md](LICENSE.md)

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📚 更多文档

- [API参考](Documentation~/api-reference.md)
- [高级主题](Documentation~/advanced-topics.md)
- [迁移指南](Documentation~/migration-guide.md)
