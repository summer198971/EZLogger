using UnityEngine;
using EZLogger;

namespace EZLogger.Samples
{
    /// <summary>
    /// 时区配置示例
    /// 演示如何使用简化的UTC偏移时区配置
    /// </summary>
    public class TimezoneConfigExample : MonoBehaviour
    {
        void Start()
        {
            LogCurrentTimezoneSettings();
            DemonstrateTimezoneSettings();
        }
        
        /// <summary>
        /// 记录当前时区设置
        /// </summary>
        void LogCurrentTimezoneSettings()
        {
            var config = EZLoggerManager.Instance.Configuration.Timezone;
            
            EZLog.Log?.Log("Timezone", "=== 当前时区配置 ===");
            EZLog.Log?.Log("Timezone", $"使用UTC时间: {config.UseUtc}");
            EZLog.Log?.Log("Timezone", $"UTC偏移小时数: {config.UtcOffsetHours}");
            EZLog.Log?.Log("Timezone", $"时区显示名: {config.GetTimezoneDisplayName()}");
            EZLog.Log?.Log("Timezone", $"当前时间: {config.FormatTime()}");
            EZLog.Log?.Log("Timezone", $"时间格式: yyyy-MM-dd HH:mm:ss.fff (固定)");
        }
        
        /// <summary>
        /// 演示不同时区设置
        /// </summary>
        void DemonstrateTimezoneSettings()
        {
            EZLog.Log?.Log("Timezone", "=== 时区设置演示 ===");
            
            // 1. UTC时间
            var utcConfig = new TimezoneConfig
            {
                UseUtc = true
            };
            EZLog.Log?.Log("Timezone", $"UTC时间: {utcConfig.FormatTime()} [{utcConfig.GetTimezoneDisplayName()}]");
            
            // 2. 中国标准时间 (UTC+8)
            var chinaConfig = new TimezoneConfig
            {
                UseUtc = false,
                UtcOffsetHours = 8
            };
            EZLog.Log?.Log("Timezone", $"中国标准时间: {chinaConfig.FormatTime()} [{chinaConfig.GetTimezoneDisplayName()}]");
            
            // 3. 东部标准时间 (UTC-5)
            var eastConfig = new TimezoneConfig
            {
                UseUtc = false,
                UtcOffsetHours = -5
            };
            EZLog.Log?.Log("Timezone", $"美国东部时间: {eastConfig.FormatTime()} [{eastConfig.GetTimezoneDisplayName()}]");
            
            // 4. 日本标准时间 (UTC+9)
            var japanConfig = new TimezoneConfig
            {
                UseUtc = false,
                UtcOffsetHours = 9
            };
            EZLog.Log?.Log("Timezone", $"日本标准时间: {japanConfig.FormatTime()} [{japanConfig.GetTimezoneDisplayName()}]");
            
            // 5. 演示偏移范围验证
            var invalidConfig = new TimezoneConfig
            {
                UseUtc = false,
                UtcOffsetHours = 20 // 超出范围
            };
            invalidConfig.ClampUtcOffset(); // 自动修正到有效范围
            EZLog.Log?.Log("Timezone", $"自动修正后: {invalidConfig.FormatTime()} [{invalidConfig.GetTimezoneDisplayName()}] (原值20被修正为14)");
        }
        
        void Update()
        {
            // 每5秒显示一次当前时间
            if (Time.frameCount % (60 * 5) == 0)
            {
                var config = EZLoggerManager.Instance.Configuration.Timezone;
                EZLog.Log?.Log("Timezone", $"定时显示 - 当前时间: {config.FormatTime()} [{config.GetTimezoneDisplayName()}]");
            }
            
            // 演示快捷键切换时区
            HandleTimezoneHotkeys();
        }
        
        /// <summary>
        /// 处理时区切换热键
        /// </summary>
        void HandleTimezoneHotkeys()
        {
            var config = EZLoggerManager.Instance.Configuration.Timezone;
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                config.UseUtc = true;
                EZLog.Log?.Log("Timezone", $"切换到UTC时间: {config.FormatTime()}");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                config.UseUtc = false;
                config.UtcOffsetHours = 8;
                EZLog.Log?.Log("Timezone", $"切换到UTC+8 (中国): {config.FormatTime()}");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                config.UseUtc = false;
                config.UtcOffsetHours = 9;
                EZLog.Log?.Log("Timezone", $"切换到UTC+9 (日本): {config.FormatTime()}");
            }
            else if (Input.GetKeyDown(KeyCode.Minus))
            {
                config.UseUtc = false;
                config.UtcOffsetHours = -5;
                EZLog.Log?.Log("Timezone", $"切换到UTC-5 (美东): {config.FormatTime()}");
            }
        }
        
        void OnGUI()
        {
            var config = EZLoggerManager.Instance.Configuration.Timezone;
            
            GUI.Label(new Rect(10, 10, 400, 20), $"当前时区: {config.GetTimezoneDisplayName()}");
            GUI.Label(new Rect(10, 40, 400, 20), $"当前时间: {config.FormatTime()}");
            GUI.Label(new Rect(10, 70, 400, 120), 
                "简化时区配置说明:\n" +
                "• 在Project Settings → EZ Logger → 时区配置中修改\n" +
                "• 支持UTC时间或UTC偏移小时数设置\n" +
                "• 偏移范围: -12到+14小时\n" +
                "• 时间格式固定: yyyy-MM-dd HH:mm:ss.fff\n" +
                "\n" +
                "快捷键演示:\n" +
                "U: UTC时间  8: UTC+8  9: UTC+9  -: UTC-5");
        }
    }
}