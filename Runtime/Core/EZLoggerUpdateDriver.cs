using System;
using System.Collections.Generic;
using UnityEngine;
using EZLogger.Appenders;

namespace EZLogger
{
    /// <summary>
    /// EZ Logger Update驱动器 - 为WebGL平台提供分帧日志处理
    /// 此组件会被自动创建并管理，不应手动添加到场景中
    /// </summary>
    internal class EZLoggerUpdateDriver : MonoBehaviour
    {
        private readonly List<ILogAppender> _updateRequiredAppenders = new List<ILogAppender>();

        // 性能配置
        private WebGLPerformanceConfig? _performanceConfig;

        // 运行时状态
        private bool _isInitialized;

        /// <summary>
        /// 初始化Update驱动器
        /// </summary>
        /// <param name="config">WebGL性能配置</param>
        public void Initialize(WebGLPerformanceConfig? config = null)
        {
            if (_isInitialized)
                return;

            _performanceConfig = config ?? WebGLPerformanceConfig.CreateDefault();

            // 输出平台信息（仅在开发环境）
            PlatformCapabilities.LogPlatformCapabilities();

            _isInitialized = true;

            Debug.Log($"[EZLogger] Update驱动器已启动 - {_performanceConfig}");
        }

        /// <summary>
        /// 注册需要Update驱动的输出器
        /// </summary>
        /// <param name="appender">输出器实例</param>
        public void RegisterAppender(ILogAppender appender)
        {
            if (appender?.RequiresUpdate != true)
                return;

            if (!_updateRequiredAppenders.Contains(appender))
            {
                _updateRequiredAppenders.Add(appender);
                Debug.Log($"[EZLogger] 注册Update驱动输出器: {appender.Name}");
            }
        }

        /// <summary>
        /// 注销输出器
        /// </summary>
        /// <param name="appender">输出器实例</param>
        public void UnregisterAppender(ILogAppender appender)
        {
            if (appender == null)
                return;

            if (_updateRequiredAppenders.Remove(appender))
            {
                Debug.Log($"[EZLogger] 注销Update驱动输出器: {appender.Name}");
            }
        }

        /// <summary>
        /// 获取当前注册的输出器数量
        /// </summary>
        public int RegisteredAppendersCount => _updateRequiredAppenders.Count;

        /// <summary>
        /// 更新性能配置
        /// </summary>
        /// <param name="config">新的性能配置</param>
        public void UpdatePerformanceConfig(WebGLPerformanceConfig? config)
        {
            if (config == null || !config.Validate())
            {
                Debug.LogWarning("[EZLogger] 无效的WebGL性能配置，使用默认配置");
                config = WebGLPerformanceConfig.CreateDefault();
            }

            _performanceConfig = config;

            Debug.Log($"[EZLogger] 更新性能配置: {config}");
        }



        private void Update()
        {
            if (!_isInitialized || _updateRequiredAppenders.Count == 0)
                return;
            float totalElapsedTime = 0f;

            // 处理每个输出器，直到超出时间预算
            for (int i = 0; i < _updateRequiredAppenders.Count; i++)
            {
                var appender = _updateRequiredAppenders[i];
                if (!appender.IsEnabled)
                    continue;

                try
                {
                    float processTime = appender.Update();
                    totalElapsedTime += processTime;

                    // 超过时间预算，下一帧继续处理
                    if (totalElapsedTime >= _performanceConfig?.MaxUpdateTimePerFrame)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EZLogger] Update驱动器处理 {appender.Name} 时发生错误: {ex.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            _updateRequiredAppenders.Clear();
            Debug.Log("[EZLogger] Update驱动器已销毁");
        }




    }
}
