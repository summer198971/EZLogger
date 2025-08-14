
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine;
namespace EZLogger.Utils
{
    /// <summary>
    /// WebGL平台文件系统工具类
    /// 提供文件同步、下载、访问等功能
    /// </summary>
    public static class WebGLFileSyncUtil
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void SyncFiles_Internal();
        
        [DllImport("__Internal")]
        private static extern void DownloadLogFolder_Internal(string folderPath);
        
        [DllImport("__Internal")]
        private static extern void ShowLogFilesList_Internal(string folderPath);
#endif

        /// <summary>
        /// 手动同步 Application.persistentDataPath 到 IndexedDB
        /// </summary>
        public static void SyncFiles()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try 
            {
                SyncFiles_Internal();
                Debug.Log("[WebGLFileSyncUtil] Files synced to IndexedDB");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebGLFileSyncUtil] Sync failed: {ex.Message}");
            }
#else
            Debug.Log("[WebGLFileSyncUtil] Sync skipped (Editor or non-WebGL build)");
#endif
        }

        /// <summary>
        /// 下载日志文件夹 - WebGL平台专用
        /// 将整个日志文件夹打包为ZIP文件下载，如果JSZip不可用则单独下载每个文件
        /// </summary>
        /// <param name="folderPath">要下载的文件夹路径</param>
        public static void DownloadLogFolder(string folderPath)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // 先确保文件已同步到IndexedDB
                SyncFiles();
                
                // 调用JavaScript下载功能
                DownloadLogFolder_Internal(folderPath);
                Debug.Log($"[WebGLFileSyncUtil] Downloading log folder: {folderPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebGLFileSyncUtil] Download failed: {ex.Message}");
                // 备用方案：显示文件列表让用户手动下载
                ShowLogFilesList(folderPath);
            }
#else
            Debug.LogWarning("[WebGLFileSyncUtil] DownloadLogFolder is only available on WebGL platform");
#endif
        }

        /// <summary>
        /// 在新标签页显示日志文件列表 - WebGL平台专用
        /// 提供美观的Web界面让用户查看和下载日志文件
        /// </summary>
        /// <param name="folderPath">要显示的文件夹路径</param>
        public static void ShowLogFilesList(string folderPath)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // 先确保文件已同步到IndexedDB
                SyncFiles();
                
                // 显示文件列表界面
                ShowLogFilesList_Internal(folderPath);
                Debug.Log($"[WebGLFileSyncUtil] Showing log files list for: {folderPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WebGLFileSyncUtil] Show files list failed: {ex.Message}");
            }
#else
            Debug.LogWarning("[WebGLFileSyncUtil] ShowLogFilesList is only available on WebGL platform");
#endif
        }

        /// <summary>
        /// 检查WebGL平台支持情况
        /// </summary>
        /// <returns>是否支持WebGL文件操作</returns>
        public static bool IsWebGLSupported()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// 获取WebGL平台状态信息
        /// </summary>
        /// <returns>状态描述字符串</returns>
        public static string GetWebGLStatus()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return "WebGL平台 - 支持文件下载和在线查看";
#else
            return "非WebGL平台 - 使用标准文件系统操作";
#endif
        }

        /// <summary>
        /// WebGL平台的安全提示
        /// </summary>
        /// <returns>安全提示信息</returns>
        public static string GetWebGLSecurityInfo()
        {
            return @"WebGL平台文件系统说明:
• 日志文件存储在浏览器的IndexedDB中
• 无法直接打开系统文件管理器
• 提供下载和在线查看功能
• 关闭浏览器标签页会保留文件
• 清除浏览器数据会删除日志文件";
        }
    }
}
