# EZLogger WebGL平台支持说明

## 概述
EZLogger针对Unity WebGL平台进行了特殊优化，提供了完整的日志文件访问和下载功能。

## WebGL平台限制
由于浏览器安全策略的限制，WebGL平台无法直接访问本地文件系统，因此EZLogger提供了以下解决方案：

### 文件存储机制
- 日志文件存储在浏览器的IndexedDB中
- 通过Unity的`Application.persistentDataPath`访问
- 文件在浏览器会话间持久保存
- 清除浏览器数据会删除日志文件

## 日志文件访问方式

### 1. 列表模式（默认）
```csharp
// 在新标签页显示美观的日志文件列表
EZLog.OpenLogFolder(); // 默认为list模式
EZLog.OpenLogFolder("list");
EZLog.ShowLogFilesList(); // 直接调用
```

功能特点：
- 📋 美观的Web界面显示所有日志文件
- 📊 显示文件大小、修改时间等信息
- 📥 支持单独下载每个文件
- 📦 支持批量下载所有文件

### 2. 下载模式
```csharp
// 将整个日志文件夹打包为ZIP下载
EZLog.OpenLogFolder("download");
EZLog.DownloadLogFolder(); // 直接调用
```

功能特点：
- 🗜️ 自动打包为ZIP文件
- 📅 文件名包含时间戳
- 🔄 备用方案：如果JSZip不可用，自动切换为单独下载
- ⚡ 一键下载所有日志文件

## 技术实现

### JavaScript桥接
```javascript
// 文件: FileSync.jslib
mergeInto(LibraryManager.library, {
    DownloadLogFolder_Internal: function(folderPathPtr) {
        // ZIP打包下载实现
    },
    ShowLogFilesList_Internal: function(folderPathPtr) {
        // 文件列表界面实现
    }
});
```

### C#调用接口
```csharp
// 文件: WebGLFileSyncUtil.cs
public static class WebGLFileSyncUtil
{
    public static void DownloadLogFolder(string folderPath);
    public static void ShowLogFilesList(string folderPath);
    public static void SyncFiles();
}
```

## 使用示例

### 基础使用
```csharp
public class LoggerTest : MonoBehaviour
{
    void Start()
    {
        // 记录一些日志
        EZLog.Log?.Log("Test", "这是一条测试日志");
        EZLog.Warning?.Log("Test", "这是一条警告日志");
        EZLog.Error?.Log("Test", "这是一条错误日志");
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("显示日志列表"))
        {
            EZLog.ShowLogFilesList();
        }
        
        if (GUILayout.Button("下载日志文件"))
        {
            EZLog.DownloadLogFolder();
        }
        
        if (GUILayout.Button("获取访问说明"))
        {
            Debug.Log(EZLog.GetLogAccessInfo());
        }
    }
}
```

### 检查平台支持
```csharp
if (WebGLFileSyncUtil.IsWebGLSupported())
{
    Debug.Log("WebGL平台支持已启用");
    Debug.Log(WebGLFileSyncUtil.GetWebGLStatus());
}
```

## 注意事项

### 浏览器兼容性
- ✅ Chrome 80+ (推荐)
- ✅ Firefox 75+
- ✅ Safari 13+
- ✅ Edge 80+

### JSZip依赖（可选）
为了获得最佳体验，建议在HTML页面中包含JSZip库：

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/jszip/3.10.1/jszip.min.js"></script>
```

如果没有JSZip，系统会自动使用备用方案（单独下载每个文件）。

### 性能建议
- 🚀 WebGL模式下使用分帧写入避免卡顿
- 💾 定期清理旧日志文件释放空间
- 🔄 使用文件同步功能确保数据持久化

### 安全考虑
- 🔒 日志文件仅存储在当前域的IndexedDB中
- 🚫 无法访问其他网站的日志数据
- 🧹 清除浏览器数据会删除所有日志文件

## 常见问题

### Q: 为什么无法直接打开文件夹？
A: 浏览器安全策略禁止WebGL应用直接访问本地文件系统，但我们提供了更好的替代方案。

### Q: 如何在服务器端查看日志？
A: 可以配置服务器上报功能，将错误日志自动发送到服务器。

### Q: 日志文件会占用多少浏览器存储空间？
A: 这取决于你的日志配置，建议定期清理或设置文件大小限制。

## 更新日志

### v1.0.0
- ✨ 首次发布WebGL平台支持
- 🎨 美观的文件列表界面
- 📦 ZIP打包下载功能
- 🔄 自动备用方案
