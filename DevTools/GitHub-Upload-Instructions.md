# EZ Logger GitHub 上传指南

## 当前状态 ✅
- [x] Git仓库已初始化
- [x] 所有文件已添加到Git
- [x] 初始提交已完成
- [x] 版本标签 v1.0.0 已创建

## 下一步：创建GitHub仓库

### 步骤1: 在GitHub网站创建仓库
1. 访问 https://github.com 并登录
2. 点击右上角的 "+" 按钮 → "New repository"
3. 填写信息：
   - **Repository name**: `EZLogger` 或 `Unity-EZ-Logger`
   - **Description**: `高性能零开销Unity日志库，与Unity LogType完全对齐`
   - **Visibility**: Public（推荐，便于分享）
   - **重要**: 不要勾选 README、.gitignore、License（我们已经有了）
4. 点击 "Create repository"

### 步骤2: 连接本地仓库到GitHub
创建仓库后，GitHub会显示类似这样的命令：

```bash
# 进入项目目录
cd /Volumes/pfwork2T/work/LogService

# 添加远程仓库（替换成您的实际URL）
git remote add origin https://github.com/summer198971/EZLogger.git

# 推送代码
git push -u origin master

# 推送标签
git push origin v1.0.0
```

### 步骤3: 验证上传成功
- 访问您的GitHub仓库页面
- 确认所有文件都已上传
- 检查 v1.0.0 标签是否存在
- 确认README.md正确显示

## Unity Package安装说明

上传成功后，用户可以通过以下方式安装：

### 方式1: Git URL安装
1. 打开Unity Package Manager
2. 点击 "+" → "Add package from git URL"
3. 输入: `https://github.com/YOUR_USERNAME/EZLogger.git`

### 方式2: 特定版本安装
```
https://github.com/YOUR_USERNAME/EZLogger.git#v1.0.0
```

### 方式3: 下载ZIP安装
1. 在GitHub仓库页面点击 "Code" → "Download ZIP"
2. 解压到Unity项目的 Packages 目录

## 项目文件结构
```
EZLogger/
├── README.md                       # 主要文档
├── package.json                    # Unity Package定义
├── LICENSE.md                      # MIT许可证
├── CHANGELOG.md                    # 更新记录
├── Runtime/                        # 运行时代码
│   ├── Core/                       # 核心功能
│   ├── Appenders/                  # 输出器
│   └── Utils/                      # 工具类
├── Editor/                         # 编辑器代码
├── Tests/                          # 测试代码
├── Samples~/                       # 示例代码
├── Documentation~/                 # 文档
├── Scripts/                        # 构建脚本
└── .cursor/                        # AI开发规则
```

## 后续维护

### 发布新版本
1. 更新代码
2. 修改 package.json 中的版本号
3. 更新 CHANGELOG.md
4. 提交并创建新标签：
   ```bash
   git add .
   git commit -m "Release v1.1.0"
   git tag -a v1.1.0 -m "版本更新说明"
   git push origin master
   git push origin v1.1.0
   ```

### 生成发布包
```bash
# 生成Unity Package zip文件
./Scripts/build-package.sh

# 在GitHub上创建Release，上传生成的zip文件
```

## 推广和分享

### 添加徽章到README.md
```markdown
![Unity](https://img.shields.io/badge/Unity-2019.4+-black.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)
![GitHub release](https://img.shields.io/github/v/release/YOUR_USERNAME/EZLogger)
```

### 社区分享
- Unity论坛
- Unity Discord社区
- Reddit r/Unity3D
- Unity Asset Store（如果适用）

---

💡 **提示**: 完成GitHub仓库创建后，请将仓库URL提供给AI，以完成最终的推送操作。
