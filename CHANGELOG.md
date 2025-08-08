# 更新日志

所有重要更改都将记录在此文件中。

## [1.0.2] - 2024-12-28

### 修复
- 🚫 彻底解决Unity Package Manager中DevTools目录警告问题
- 📋 从Git跟踪中完全移除DevTools目录
- 📁 添加.npmignore文件进行包管理优化
- 🔧 在package.json中明确指定包含的文件列表
- 📝 更新.gitignore以防止DevTools目录再次被跟踪

### 改进
- 📦 确保Unity Package完全干净，无任何开发工具文件
- 🛡️ 多层保护防止开发文件进入Unity Package分发

## [1.0.1] - 2024-12-28

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

## [1.0.0] - 2024-01-XX

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
