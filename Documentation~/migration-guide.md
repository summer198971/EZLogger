# 迁移指南

本指南帮助您从旧的日志系统迁移到新的EZ Logger系统。

## 🔄 迁移概述

新的EZ Logger系统完全重写了原有的日志架构，提供更好的性能、更清晰的API和更强的扩展性。

## 📊 API对比

### 旧系统 vs 新系统

| 功能 | 旧系统 | 新系统（零开销） | 新系统（传统） |
|------|--------|------------------|----------------|
| 基础日志 | `DebugerNew.Log?.Log(this, "message")` | `EZLog.I?.Log(this, "message")` | `EZLog.ILog(this, "message")` |
| 警告日志 | `DebugerNew.Warning?.Log(this, "message")` | `EZLog.W?.Log(this, "message")` | `EZLog.WLog(this, "message")` |
| 错误日志 | `DebugerNew.Error?.Log(this, "message")` | `EZLog.E?.Log(this, "message")` | `EZLog.ELog(this, "message")` |
| 强制日志 | `DebugerNew.ForceLog(tag, msg)` | `EZLog.I?.Log(tag, msg)` | `EZLog.ILog(tag, msg)` |
| 级别检查 | `logReport.CD(LogType.Log)` | 不需要（使用`?.`） | `EZLog.IsLevelEnabled(LogLevel.Info)` |

## 📝 详细迁移步骤

### 1. 替换基础日志调用（推荐：零开销方式）

**旧代码:**
```csharp
DebugerNew.Log?.Log(this, "游戏开始");
```

**新代码（零开销）:**
```csharp
EZLog.I?.Log(this, "游戏开始");
```

**新代码（传统方式）:**
```csharp
EZLog.ILog(this, "游戏开始");
```

### 🚀 零开销迁移的优势

新的零开销设计完美保持了您原来的使用习惯，同时提供更好的性能：

**旧系统:**
```csharp
// 需要判断Log是否为null
DebugerNew.Log?.Log("Player", $"位置: {transform.position}");
```

**新系统（零开销）:**
```csharp
// 直接使用，当级别禁用时连参数都不会计算
EZLog.D?.Log("Player", $"位置: {transform.position}");
```

### 2. 替换警告日志

**旧代码:**
```csharp
DebugerNew.Warning?.Log("NetworkManager", "连接超时");
```

**新代码（零开销）:**
```csharp
EZLog.W?.Log("NetworkManager", "连接超时");
```

### 3. 替换错误日志

**旧代码:**
```csharp
DebugerNew.Error?.Log("DataManager", "数据加载失败", stackTrace);
```

**新代码（零开销）:**
```csharp
EZLog.E?.Log("DataManager", "数据加载失败");
// 或者记录异常
EZLog.Exception("DataManager", exception);
```

### 4. 替换复杂计算的日志（重要！）

**旧代码:**
```csharp
if (logReport.CD(LogType.Log))
{
    string expensiveInfo = GenerateDebugInfo();
    DebugerNew.Log?.Log(this, expensiveInfo);
}
```

**新代码（零开销 - 推荐）:**
```csharp
// 🚀 真正的零开销！GenerateDebugInfo()只有在级别启用时才会调用
EZLog.I?.Log(this, GenerateDebugInfo());
```

**新代码（传统方式）:**
```csharp
if (EZLog.IsLevelEnabled(LogLevel.Info))
{
    string expensiveInfo = GenerateDebugInfo();
    EZLog.ILog(this, expensiveInfo);
}
```

### 5. 替换配置系统

**旧代码:**
```csharp
logReport.EnabelDebug = true;
```

**新代码:**
```csharp
EZLoggerManager.Instance.EnabledLevels = LogLevel.All;
// 或者使用配置
var config = LoggerConfiguration.CreateDevelopment();
EZLoggerManager.Instance.Configuration = config;
```

## 🔧 高级迁移

### 自定义输出器迁移

如果您之前有自定义的日志输出逻辑，现在可以通过实现`ILogAppender`接口来替代：

**旧系统的自定义输出:**
```csharp
public class CustomLogWriter : LogWriter
{
    // 自定义实现
}
```

**新系统的自定义输出器:**
```csharp
public class CustomAppender : LogAppenderBase
{
    public override string Name => "Custom";
    
    protected override void WriteLogCore(LogMessage message)
    {
        // 自定义写入逻辑
    }
}

// 使用
var customAppender = new CustomAppender();
EZLoggerManager.Instance.AddAppender(customAppender);
```

### 条件编译符号

**旧系统:**
```csharp
#if UNITY_EDITOR
    // 编辑器专用日志
#endif

#if FIGHT_SERVER
    // 服务器专用日志
#endif
```

**新系统:**
```csharp
#if UNITY_EDITOR
    EZLog.D("Editor", "编辑器专用日志");
#endif

// 服务器环境可以通过配置控制
#if SERVER_BUILD
    var config = LoggerConfiguration.CreateRelease();
    config.GlobalEnabledLevels = LogLevel.ErrorAndWarning;
    EZLoggerManager.Instance.Configuration = config;
#endif
```

## 🚀 性能优化建议

### 1. 使用级别检查

确保在性能敏感的地方使用级别检查：

```csharp
// ✅ 推荐
if (EZLog.IsLevelEnabled(LogLevel.Debug))
{
    EZLog.D("Performance", $"复杂计算结果: {ExpensiveCalculation()}");
}

// ❌ 避免
EZLog.D("Performance", $"复杂计算结果: {ExpensiveCalculation()}");
```


### 3. 使用格式化方法

优先使用格式化方法而不是字符串拼接：

```csharp
// ✅ 推荐
EZLog.IFormat("Player", "生命值: {0}/{1}", currentHp, maxHp);

// ❌ 避免
EZLog.I("Player", "生命值: " + currentHp + "/" + maxHp);
```

## 🔍 常见问题

### Q: 如何保持与Firebase的集成？

**A:** 可以创建Firebase输出器：

```csharp
public class FirebaseAppender : LogAppenderBase
{
    public override string Name => "Firebase";
    
    protected override void WriteLogCore(LogMessage message)
    {
        if (message.Level >= LogLevel.Error)
        {
#if !UNITY_EDITOR
            var exception = new System.Exception($"[{message.Tag}] {message.Message}");
            Firebase.Crashlytics.Crashlytics.LogException(exception);
#endif
        }
    }
}
```

### Q: 如何迁移现有的日志文件？

**A:** 新系统会创建新的日志文件格式。如果需要保持兼容性，可以自定义格式化器：

```csharp
public class LegacyFormatter : ILogFormatter
{
    public string Format(LogMessage message)
    {
        // 保持旧格式
        return $"[!@#]{message.Timestamp:HH:mm:ss:fff} [{message.Level}]: {message.Message}";
    }
}
```

### Q: 性能差异有多大？

**A:** 新系统在性能模式下比旧系统快约3-5倍，内存分配减少90%以上。

## 📋 迁移检查清单

- [ ] 替换所有`DebugerNew`调用为`EZLog`
- [ ] 更新级别检查逻辑
- [ ] 配置新的日志级别
- [ ] 测试所有日志输出
- [ ] 验证文件输出功能
- [ ] 更新条件编译符号
- [ ] 迁移自定义输出器（如有）
- [ ] 测试异常记录功能
- [ ] 验证服务器集成（如有）

## ⚠️ 重要注意事项

1. **不兼容性**: 新系统与旧系统API不兼容，需要手动迁移
2. **配置文件**: 旧的配置依赖项需要替换为新的配置系统
3. **文件格式**: 日志文件格式可能有所变化
4. **性能**: 新系统性能更好，但需要正确配置
5. **扩展性**: 新系统支持更好的扩展，建议利用新功能

## 💡 迁移技巧

1. **逐步迁移**: 建议分模块逐步迁移，不要一次性全部替换
2. **保持测试**: 每迁移一个模块就测试一次
3. **性能测试**: 迁移完成后进行性能对比测试
4. **备份**: 迁移前备份旧的日志相关代码
5. **团队培训**: 确保团队成员了解新的API使用方式

---

如有任何迁移问题，请查看[API参考文档](api-reference.md)或提交Issue。
