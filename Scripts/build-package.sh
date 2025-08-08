#!/bin/bash

# EZ Logger Unity Package 打包脚本
set -e

# 配置
PACKAGE_NAME="EZLogger"
VERSION="1.0.0"
BUILD_DIR="Build"
PACKAGE_DIR="$BUILD_DIR/$PACKAGE_NAME"

echo "🚀 开始打包 EZ Logger v$VERSION..."

# 1. 清理旧的构建
echo "🧹 清理旧构建..."
rm -rf "$BUILD_DIR"
mkdir -p "$PACKAGE_DIR"

# 2. 复制核心文件
echo "📁 复制核心文件..."
cp -r Runtime "$PACKAGE_DIR/"
cp -r Editor "$PACKAGE_DIR/"
cp -r Tests "$PACKAGE_DIR/"
cp -r Samples~ "$PACKAGE_DIR/"
cp -r Documentation~ "$PACKAGE_DIR/"

# 3. 复制Package文件
echo "📄 复制Package文件..."
cp package.json "$PACKAGE_DIR/"
cp README.md "$PACKAGE_DIR/"
cp CHANGELOG.md "$PACKAGE_DIR/"
cp LICENSE.md "$PACKAGE_DIR/"

# 4. 清理不需要的文件
echo "🧹 清理不需要的文件..."
find "$PACKAGE_DIR" -name "*.meta" -delete
find "$PACKAGE_DIR" -name ".DS_Store" -delete
find "$PACKAGE_DIR" -name "Thumbs.db" -delete

# 5. 验证Package结构
echo "✅ 验证Package结构..."
if [ ! -f "$PACKAGE_DIR/package.json" ]; then
    echo "❌ 错误: package.json 缺失"
    exit 1
fi

if [ ! -d "$PACKAGE_DIR/Runtime" ]; then
    echo "❌ 错误: Runtime目录缺失"
    exit 1
fi

echo "✅ Package结构验证通过"

# 6. 创建压缩包
echo "📦 创建压缩包..."
cd "$BUILD_DIR"
zip -r "${PACKAGE_NAME}-v${VERSION}.zip" "$PACKAGE_NAME/"
cd ..

# 7. 创建Git Tag（如果在Git仓库中）
if [ -d ".git" ]; then
    echo "🏷️ 创建Git标签..."
    git add .
    git commit -m "Release v$VERSION" || echo "无新更改需要提交"
    git tag -a "v$VERSION" -m "EZ Logger v$VERSION" || echo "标签已存在"
    echo "📤 推送到远程仓库..."
    git push origin main || echo "推送失败或无远程仓库"
    git push origin "v$VERSION" || echo "标签推送失败"
fi

# 8. 生成安装说明
echo "📖 生成安装说明..."
cat > "$BUILD_DIR/INSTALL.md" << EOF
# EZ Logger v$VERSION 安装指南

## 方法1: 本地Package安装
1. 解压 ${PACKAGE_NAME}-v${VERSION}.zip
2. 将解压后的 $PACKAGE_NAME 文件夹复制到项目的 Packages 目录下
3. Unity会自动识别并导入Package

## 方法2: Git URL安装（如果已推送到Git）
1. 打开Unity Package Manager
2. 点击"+"按钮，选择"Add package from git URL"
3. 输入: https://github.com/your-username/EZLogger.git
4. 点击Add

## 方法3: 本地路径安装
1. 打开Unity Package Manager
2. 点击"+"按钮，选择"Add package from disk"
3. 选择 $PACKAGE_NAME/package.json 文件

## 快速开始
\`\`\`csharp
using EZLogger;

// 零开销日志记录
EZLog.Log?.Log("MyGame", "游戏开始");
EZLog.Warning?.Log("MyGame", "这是警告");
EZLog.Error?.Log("MyGame", "这是错误");

// 运行时级别控制
EZLog.EnableAll();           // 启用所有级别
EZLog.SetWarningAndAbove();  // 仅警告及以上
\`\`\`

更多示例请查看Samples文件夹。
EOF

echo "🎉 打包完成!"
echo "📦 输出文件: $BUILD_DIR/${PACKAGE_NAME}-v${VERSION}.zip"
echo "📖 安装说明: $BUILD_DIR/INSTALL.md"
echo ""
echo "🔗 分发方式:"
echo "1. 直接分发zip文件"
echo "2. 从Git仓库安装: https://github.com/your-username/EZLogger.git"
echo "3. 本地路径安装Package"
