using System;

namespace EZLogger
{
    /// <summary>
    /// 日志级别枚举，与Unity LogType对齐，支持位运算组合
    /// 级别从低到高：Log < Warning < Assert < Error < Exception
    /// </summary>
    [Flags]
    public enum LogLevel
    {
        /// <summary>无日志输出</summary>
        None = 0,
        
        // 与Unity LogType完全对齐的级别
        /// <summary>普通日志消息 (对应Unity LogType.Log)</summary>
        Log = 1 << 0,
        /// <summary>警告消息 (对应Unity LogType.Warning)</summary>
        Warning = 1 << 1,
        /// <summary>断言消息 (对应Unity LogType.Assert)</summary>
        Assert = 1 << 2,
        /// <summary>错误消息 (对应Unity LogType.Error)</summary>
        Error = 1 << 3,
        /// <summary>异常消息 (对应Unity LogType.Exception)</summary>
        Exception = 1 << 4,
        
        // 组合级别
        /// <summary>所有级别</summary>
        All = Log | Warning | Assert | Error | Exception,
        /// <summary>警告及以上级别</summary>
        WarningAndAbove = Warning | Assert | Error | Exception,
        /// <summary>错误及以上级别</summary>
        ErrorAndAbove = Error | Exception,
        /// <summary>错误和警告级别（向下兼容）</summary>
        ErrorAndWarning = Warning | Error | Exception
    }
    
    /// <summary>
    /// 日志级别扩展方法
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// 检查指定级别是否包含在当前级别中
        /// </summary>
        public static bool Contains(this LogLevel current, LogLevel level)
        {
            return (current & level) == level;
        }
        
        /// <summary>
        /// 获取日志级别的字符串表示
        /// </summary>
        public static string ToShortString(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Log => "L",
                LogLevel.Warning => "W",
                LogLevel.Assert => "A",
                LogLevel.Error => "E",
                LogLevel.Exception => "X",
                _ => "U"
            };
        }
        
        /// <summary>
        /// 获取日志级别的颜色代码（用于Unity Console）
        /// </summary>
        public static string GetUnityColor(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Log => "white",
                LogLevel.Warning => "yellow",
                LogLevel.Assert => "cyan",
                LogLevel.Error => "red",
                LogLevel.Exception => "magenta",
                _ => "white"
            };
        }
        
        /// <summary>
        /// 转换为Unity LogType
        /// </summary>
        public static UnityEngine.LogType ToUnityLogType(this LogLevel level)
        {
            return level switch
            {
                LogLevel.Log => UnityEngine.LogType.Log,
                LogLevel.Warning => UnityEngine.LogType.Warning,
                LogLevel.Assert => UnityEngine.LogType.Assert,
                LogLevel.Error => UnityEngine.LogType.Error,
                LogLevel.Exception => UnityEngine.LogType.Exception,
                _ => UnityEngine.LogType.Log
            };
        }
        
        /// <summary>
        /// 从Unity LogType转换
        /// </summary>
        public static LogLevel FromUnityLogType(UnityEngine.LogType unityLogType)
        {
            return unityLogType switch
            {
                UnityEngine.LogType.Log => LogLevel.Log,
                UnityEngine.LogType.Warning => LogLevel.Warning,
                UnityEngine.LogType.Assert => LogLevel.Assert,
                UnityEngine.LogType.Error => LogLevel.Error,
                UnityEngine.LogType.Exception => LogLevel.Exception,
                _ => LogLevel.Log
            };
        }
    }
}
