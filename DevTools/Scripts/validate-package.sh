#!/bin/bash

# EZ Logger Package 验证脚本
set -e

PACKAGE_DIR="Build/EZLogger"

echo "🔍 验证 EZ Logger Package..."

# 1. 检查必需文件
echo "📄 检查必需文件..."
required_files=(
    "package.json"
    "README.md" 
    "LICENSE.md"
    "CHANGELOG.md"
    "Runtime/EZLogger.Runtime.asmdef"
    "Runtime/Core/LogLevel.cs"
    "Runtime/Core/EZLoggerManager.cs"
    "Runtime/Core/ILogger.cs"
    "Runtime/Core/ConditionalLogger.cs"
    "Runtime/Core/LoggerConfiguration.cs"
    "Runtime/Core/LogMessage.cs"
)

for file in "${required_files[@]}"; do
    if [ ! -f "$PACKAGE_DIR/$file" ]; then
        echo "❌ 缺失文件: $file"
        exit 1
    else
        echo "✅ $file"
    fi
done

# 2. 检查关键API定义
echo ""
echo "🔧 检查关键API..."

# 检查LogLevel定义
if grep -q "Log = 1 << 0" "$PACKAGE_DIR/Runtime/Core/LogLevel.cs"; then
    echo "✅ LogLevel.Log 定义正确"
else
    echo "❌ LogLevel.Log 定义错误"
    exit 1
fi

if grep -q "ErrorAndWarning = Warning | Error | Exception" "$PACKAGE_DIR/Runtime/Core/LogLevel.cs"; then
    echo "✅ LogLevel.ErrorAndWarning 定义正确"
else
    echo "❌ LogLevel.ErrorAndWarning 定义缺失"
    exit 1
fi

# 检查零开销API
if grep -q "EZLog.Log?.Log" "$PACKAGE_DIR/Samples~/Basic Usage/BasicUsageExample.cs"; then
    echo "✅ 零开销API示例正确"
else
    echo "❌ 零开销API示例错误"
    exit 1
fi

# 3. 检查Unity对齐
echo ""
echo "🎯 检查Unity LogType对齐..."

# 检查ToUnityLogType方法
if grep -q "ToUnityLogType" "$PACKAGE_DIR/Runtime/Core/LogLevel.cs"; then
    echo "✅ Unity LogType转换方法存在"
else
    echo "❌ Unity LogType转换方法缺失"
    exit 1
fi

# 4. 检查package.json格式
echo ""
echo "📋 验证package.json..."
if python3 -m json.tool "$PACKAGE_DIR/package.json" > /dev/null 2>&1; then
    echo "✅ package.json 格式正确"
else
    echo "❌ package.json 格式错误"
    exit 1
fi

# 5. 统计信息
echo ""
echo "📊 Package统计信息:"
echo "- 总文件数: $(find "$PACKAGE_DIR" -type f | wc -l)"
echo "- C#文件数: $(find "$PACKAGE_DIR" -name "*.cs" | wc -l)"
echo "- 示例数量: $(find "$PACKAGE_DIR/Samples~" -name "*.cs" | wc -l)"
echo "- 文档文件: $(find "$PACKAGE_DIR" -name "*.md" | wc -l)"

echo ""
echo "🎉 Package验证通过！"
echo "📦 可以安全分发: Build/EZLogger-v1.0.0.zip"
