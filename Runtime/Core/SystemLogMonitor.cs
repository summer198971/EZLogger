using System;
using UnityEngine;

namespace EZLogger
{
    /// <summary>
    /// Unity系统日志监控器
    /// 监听Application.logMessageReceived回调，捕获系统级错误和异常
    /// </summary>
    public class SystemLogMonitor
    {
        private static SystemLogMonitor _instance;
        private bool _isRegistered;
        private bool _preventDuplicateError;
        private bool _preventDuplicateException;

        public static SystemLogMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SystemLogMonitor();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 系统错误/异常处理事件
        /// </summary>
        public event Action<string, string, LogLevel> OnSystemLogReceived;

        /// <summary>
        /// 启动系统日志监控
        /// </summary>
        public void StartMonitoring()
        {
            if (!_isRegistered)
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                Application.logMessageReceived += OnLogMessageReceived;
                _isRegistered = true;
            }
        }

        /// <summary>
        /// 停止系统日志监控
        /// </summary>
        public void StopMonitoring()
        {
            if (_isRegistered)
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                _isRegistered = false;
            }
        }

        /// <summary>
        /// 设置防重复标志 - 用于防止自定义API调用Unity Debug时造成重复日志
        /// </summary>
        /// <param name="logLevel">日志级别</param>
        /// <param name="prevent">是否防止重复</param>
        public void SetPreventDuplicate(LogLevel logLevel, bool prevent)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                    _preventDuplicateError = prevent;
                    break;
                case LogLevel.Exception:
                    _preventDuplicateException = prevent;
                    break;
            }
        }

        private void OnLogMessageReceived(string condition, string stackTrace, UnityEngine.LogType type)
        {
            // 检查是否需要防止重复处理
            if (ShouldPreventDuplicate(type))
            {
                return;
            }

            // 只处理错误和异常
            if (type == UnityEngine.LogType.Error || type == UnityEngine.LogType.Exception)
            {
                var ezLogLevel = LogLevelExtensions.FromUnityLogType(type);
                OnSystemLogReceived?.Invoke(condition, stackTrace, ezLogLevel);
            }
        }

        private bool ShouldPreventDuplicate(UnityEngine.LogType type)
        {
            return (type == UnityEngine.LogType.Error && _preventDuplicateError) ||
                   (type == UnityEngine.LogType.Exception && _preventDuplicateException);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release()
        {
            StopMonitoring();
            OnSystemLogReceived = null;
        }
    }
}
