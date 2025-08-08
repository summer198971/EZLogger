@echo off
setlocal enabledelayedexpansion

REM EZ Logger Unity Package 打包脚本 (Windows)
echo 🚀 开始打包 EZ Logger...

REM 配置
set PACKAGE_NAME=EZLogger
set VERSION=1.0.0
set BUILD_DIR=Build
set PACKAGE_DIR=%BUILD_DIR%\%PACKAGE_NAME%

REM 1. 清理旧的构建
echo 🧹 清理旧构建...
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%PACKAGE_DIR%"

REM 2. 复制核心文件
echo 📁 复制核心文件...
xcopy /e /i /y Runtime "%PACKAGE_DIR%\Runtime"
xcopy /e /i /y Editor "%PACKAGE_DIR%\Editor"
xcopy /e /i /y Tests "%PACKAGE_DIR%\Tests"
xcopy /e /i /y Samples~ "%PACKAGE_DIR%\Samples~"
xcopy /e /i /y Documentation~ "%PACKAGE_DIR%\Documentation~"

REM 3. 复制Package文件
echo 📄 复制Package文件...
copy package.json "%PACKAGE_DIR%\"
copy README.md "%PACKAGE_DIR%\"
copy CHANGELOG.md "%PACKAGE_DIR%\"
copy LICENSE.md "%PACKAGE_DIR%\"

REM 4. 清理不需要的文件
echo 🧹 清理不需要的文件...
for /r "%PACKAGE_DIR%" %%f in (*.meta) do del "%%f"
for /r "%PACKAGE_DIR%" %%f in (.DS_Store) do del "%%f"
for /r "%PACKAGE_DIR%" %%f in (Thumbs.db) do del "%%f"

REM 5. 验证Package结构
echo ✅ 验证Package结构...
if not exist "%PACKAGE_DIR%\package.json" (
    echo ❌ 错误: package.json 缺失
    exit /b 1
)

if not exist "%PACKAGE_DIR%\Runtime" (
    echo ❌ 错误: Runtime目录缺失
    exit /b 1
)

echo ✅ Package结构验证通过

REM 6. 创建压缩包（需要安装7-Zip或WinRAR）
echo 📦 创建压缩包...
cd "%BUILD_DIR%"
if exist "C:\Program Files\7-Zip\7z.exe" (
    "C:\Program Files\7-Zip\7z.exe" a -tzip "%PACKAGE_NAME%-v%VERSION%.zip" "%PACKAGE_NAME%\"
) else if exist "C:\Program Files\WinRAR\WinRAR.exe" (
    "C:\Program Files\WinRAR\WinRAR.exe" a -afzip "%PACKAGE_NAME%-v%VERSION%.zip" "%PACKAGE_NAME%\"
) else (
    echo ⚠️ 警告: 未找到7-Zip或WinRAR，请手动压缩 %PACKAGE_NAME% 文件夹
)
cd ..

REM 7. 生成安装说明
echo 📖 生成安装说明...
(
echo # EZ Logger v%VERSION% 安装指南
echo.
echo ## 方法1: 本地Package安装
echo 1. 解压 %PACKAGE_NAME%-v%VERSION%.zip
echo 2. 将解压后的 %PACKAGE_NAME% 文件夹复制到项目的 Packages 目录下
echo 3. Unity会自动识别并导入Package
echo.
echo ## 方法2: Git URL安装^(如果已推送到Git^)
echo 1. 打开Unity Package Manager
echo 2. 点击"+"按钮，选择"Add package from git URL"
echo 3. 输入: https://github.com/your-username/EZLogger.git
echo 4. 点击Add
echo.
echo ## 快速开始
echo ```csharp
echo using EZLogger;
echo.
echo // 零开销日志记录
echo EZLog.Log?.Log^("MyGame", "游戏开始"^);
echo EZLog.Warning?.Log^("MyGame", "这是警告"^);
echo EZLog.Error?.Log^("MyGame", "这是错误"^);
echo.
echo // 运行时级别控制
echo EZLog.EnableAll^(^);           // 启用所有级别
echo EZLog.SetWarningAndAbove^(^);  // 仅警告及以上
echo ```
echo.
echo 更多示例请查看Samples文件夹。
) > "%BUILD_DIR%\INSTALL.md"

echo 🎉 打包完成!
echo 📦 输出文件: %BUILD_DIR%\%PACKAGE_NAME%-v%VERSION%.zip
echo 📖 安装说明: %BUILD_DIR%\INSTALL.md
echo.
echo 🔗 分发方式:
echo 1. 直接分发zip文件
echo 2. 从Git仓库安装: https://github.com/your-username/EZLogger.git
echo 3. 本地路径安装Package

pause
