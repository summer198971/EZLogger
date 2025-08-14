using UnityEngine;

namespace EZLogger
{
    /// <summary>
    /// 平台能力检测工具 - 用于判断当前平台是否支持多线程等功能
    /// </summary>
    public static class PlatformCapabilities
    {
        /// <summary>
        /// 当前平台是否支持多线程
        /// WebGL平台不支持System.Threading.Thread，需要使用Update驱动
        /// </summary>
        public static bool SupportsThreading
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// 当前平台是否支持System.Threading.Timer
        /// WebGL平台不支持Timer，需要使用协程或Update替代
        /// </summary>
        public static bool SupportsTimer
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// 是否需要使用Update驱动的输出器
        /// 等同于!SupportsThreading，提供更语义化的接口
        /// </summary>
        public static bool RequiresUpdateDriven => !SupportsThreading;

        /// <summary>
        /// 获取当前平台的描述信息（用于调试）
        /// </summary>
        public static string GetPlatformDescription()
        {
            var threading = SupportsThreading ? "支持多线程" : "不支持多线程";
            var timer = SupportsTimer ? "支持Timer" : "不支持Timer";
            var updateRequired = RequiresUpdateDriven ? "需要Update驱动" : "不需要Update驱动";

            return $"平台: {Application.platform}, {threading}, {timer}, {updateRequired}";
        }

        /// <summary>
        /// 在开发期间输出平台能力信息（仅在Editor或Development Build中生效）
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogPlatformCapabilities()
        {
            Debug.Log($"[EZLogger] {GetPlatformDescription()}");
        }
    }
}
