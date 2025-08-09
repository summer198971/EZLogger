# 更新日志

所有重要更改都将记录在此文件中。

## [1.0.6] - 2025-08-08

### 新增
- 🔄 **运行时动态输出器管理**：支持运行时启用/禁用Unity控制台和文件输出器
- 🎯 **智能配置刷新机制**：Configuration setter自动调用RefreshAppenders()
- 📱 **平台特定配置示例**：针对不同平台的最佳日志配置策略
- 🎮 **游戏玩法集成示例**：展示在游戏不同状态下的动态日志管理
- 📋 **配置模板系统**：提供开发、发布、移动端等预设配置模板
- 🔧 **公开配置刷新API**：RefreshConfiguration()方法支持手动刷新

### 改进
- ⚡ **输出器生命周期管理**：智能检测现有输出器，避免重复创建
- 🛡️ **完善错误处理**：单个输出器失败不影响其他输出器工作
- 📊 **实时配置监控**：UI界面实时显示当前输出器状态
- 🚀 **性能优化策略**：根据游戏状态动态调整日志性能配置

### 示例更新
- 📁 **Runtime Configuration示例集**：完整的运行时配置管理示例
- 🎛️ **交互式配置界面**：展示如何创建用户友好的日志配置UI
- 🏗️ **架构最佳实践**：演示在实际项目中集成EZ Logger的方法
- 📖 **详细文档说明**：包含使用场景、最佳实践和扩展建议

### 技术细节
- `EZLoggerManager.Configuration` setter现在会自动触发`RefreshAppenders()`
- 新增`ManageUnityAppender()`和`ManageFileAppender()`智能管理方法
- 新增`GetAppenderByName()`用于查找现有输出器
- 提供`ConfigurationTemplates`静态类包含常用配置预设

## [1.0.5] - 2025-08-08

### 新增
- 🎛️ **Unity项目设置界面集成**：在Project Settings → EZ Logger中配置
- 🖥️ **完整图形化配置界面**：支持所有EZ Logger功能的可视化配置
- ⚡ **快速配置预设**：开发模式、发布模式、性能模式、默认配置
- 📁 **配置导入导出**：JSON格式，便于团队协作和配置管理
- 📋 **实现状态标注**：明确显示已实现(60%)和待实现功能(40%)
- 🔄 **运行时自动应用**：进入Play Mode时自动应用项目设置
- 🛠️ **Tools菜单集成**：提供快捷操作入口

### 改进
- ⚠️ **功能状态透明化**：未实现的配置项被禁用并标注说明
- 📝 **用户友好提示**：提供详细的功能说明和实现状态
- 🎯 **避免用户混淆**：清晰区分已实现和待实现功能

### 技术细节
- 🏗️ 实现EZLoggerSettings ScriptableObject存储配置
- 🔧 自动转换为运行时LoggerConfiguration
- 📱 支持实时配置应用和状态显示

## [1.0.4] - 2025-08-08

### 修复
- 🔧 确保所有必要的.meta文件包含在Unity Package中
- 📋 在package.json的files字段中明确指定.meta文件
- 🛠️ 解决Unity Package Manager中"has no meta file"警告

### 改进
- ✅ Unity Package现在包含所有必要的元数据文件
- 🎯 完全符合Unity Package规范

## [1.0.3] - 2025-08-08

### 修复
- 🚫 彻底移除package-build.json等构建配置文件
- 📋 优化.gitignore、.npmignore、.gitattributes配置
- 🔧 精确指定package.json中的files字段
- 📁 确保Unity Package只包含必要的运行时文件
- 🛡️ 加强多层文件过滤保护

### 改进
- 📦 进一步清理Unity Package内容
- 🎯 确保完全没有构建和开发工具相关的警告

## [1.0.2] - 2025-08-08

### 修复
- 🚫 彻底解决Unity Package Manager中DevTools目录警告问题
- 📋 从Git跟踪中完全移除DevTools目录
- 📁 添加.npmignore文件进行包管理优化
- 🔧 在package.json中明确指定包含的文件列表
- 📝 更新.gitignore以防止DevTools目录再次被跟踪

### 改进
- 📦 确保Unity Package完全干净，无任何开发工具文件
- 🛡️ 多层保护防止开发文件进入Unity Package分发

## [1.0.1] - 2025-08-08

### 修复
- 🔧 修复Unity Package Manager安装时的meta文件警告
- 📁 重组项目结构，将开发工具移至DevTools目录
- 🚫 排除.cursor、DevTools、Build目录在Unity Package分发之外
- 🔗 更新package.json中的GitHub仓库URL
- 📋 添加.gitattributes文件进行包分发控制

### 改进
- 📦 优化Unity Package结构，提供更干净的安装体验
- 🛠️ 修正构建脚本路径问题
- 📖 更新安装说明和文档

## [1.0.0] - 2025-08-08

### 新增
- 🎉 首次发布EZ Logger
- ✨ 支持5个日志级别，与Unity LogType完全对齐 (Log, Warning, Assert, Error, Exception)
- 🚀 零GC分配的性能模式
- 📁 异步文件输出支持
- 🔄 自动文件轮转功能
- 🎯 Unity编辑器集成
- 🔧 可扩展的输出器架构
- 📊 线程安全的日志队列
- 🎨 Unity控制台彩色输出
- 📝 完整的API文档和示例

### 功能特性
- 支持多种输出目标（Unity控制台、文件、服务器）
- 可配置的日志级别控制
- 异步写入不阻塞主线程
- 自动堆栈跟踪获取
- 线程安全的设计
- 对象池优化内存使用
- 支持格式化日志输出
- 条件编译符号支持

### 技术细节
- 基于.NET Standard 2.0
- 支持Unity 2020.3+
- 使用Assembly Definition分离运行时和编辑器代码
- 完整的单元测试覆盖
- 性能基准测试

## 计划中的功能

### [1.1.0] - 规划中
- 🌐 服务器输出器实现
- 🔥 Firebase Crashlytics集成
- 📊 日志统计和分析
- 🔍 高级日志过滤器
- 💾 日志压缩功能

### [1.2.0] - 规划中
- 📱 移动端优化
- 🌍 多语言支持
- 🎛️ 运行时配置修改
- 📈 性能监控面板
- 🔐 日志加密功能

### 长期规划
- 🤖 AI辅助日志分析
- ☁️ 云端日志管理
- 📊 实时日志仪表板
- 🔗 第三方服务集成

---

## 版本命名规则

本项目遵循 [语义化版本控制](https://semver.org/lang/zh-CN/) 规范：

- **主版本号**: 不兼容的API修改
- **次版本号**: 向下兼容的功能性新增
- **修订号**: 向下兼容的问题修正

## 支持的Unity版本

- Unity 2020.3 LTS (推荐)
- Unity 2021.3 LTS
- Unity 2022.3 LTS
- Unity 2023.x (最新版本)

## 平台支持

- ✅ Windows
- ✅ macOS  
- ✅ Linux
- ✅ iOS
- ✅ Android
- ✅ WebGL
- ✅ Console Platforms
