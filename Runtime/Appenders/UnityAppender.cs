using System.Text;

namespace EZLogger.Appenders
{
    /// <summary>
    /// Unity控制台输出器
    /// </summary>
    public class UnityAppender : LogAppenderBase
    {
        public override string Name => "Unity Console";
        
        private UnityConsoleConfig _config;
        private StringBuilder _stringBuilder;
        
        protected override void InitializeCore(object config)
        {
            _config = config as UnityConsoleConfig ?? new UnityConsoleConfig();
            _stringBuilder = new StringBuilder(256);
            
            // 设置支持的级别
            SupportedLevels = LogLevel.All;
            if (_config.MinLevel != LogLevel.None)
            {
                // 只显示大于等于最小级别的日志
                SupportedLevels = LogLevel.None;
                foreach (LogLevel level in System.Enum.GetValues(typeof(LogLevel)))
                {
                    if (level != LogLevel.None && level != LogLevel.All && level >= _config.MinLevel)
                    {
                        SupportedLevels |= level;
                    }
                }
            }
        }
        
        protected override void WriteLogCore(LogMessage message)
        {
#if UNITY_2018_1_OR_NEWER
            string formattedMessage = FormatMessage(message);
            
            // 直接使用Unity LogType，完全对齐
            var unityLogType = message.Level.ToUnityLogType();
            
            switch (unityLogType)
            {
                case UnityEngine.LogType.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
                case UnityEngine.LogType.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case UnityEngine.LogType.Assert:
                    UnityEngine.Debug.LogAssertion(formattedMessage);
                    break;
                case UnityEngine.LogType.Exception:
                    UnityEngine.Debug.LogError($"[Exception] {formattedMessage}");
                    break;
                case UnityEngine.LogType.Log:
                default:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
            }
#endif
        }
        
        private string FormatMessage(LogMessage message)
        {
            _stringBuilder.Clear();
            
            if (_config.EnableColors)
            {
                _stringBuilder.Append($"<color={message.Level.GetUnityColor()}>");
            }
            
            // 添加帧数信息
            if (_config.ShowFrameCount && message.FrameCount > 0)
            {
                _stringBuilder.Append($"[FRAME:{message.FrameCount}]");
            }
            
            // 添加线程ID
            if (_config.ShowThreadId)
            {
                _stringBuilder.Append($"[T:{message.ThreadId}]");
            }
            
            // 添加标签
            _stringBuilder.Append($"[{message.Tag}]");
            
            if (_config.EnableColors)
            {
                _stringBuilder.Append("</color>");
            }
            
            // 添加消息内容
            _stringBuilder.Append(" ");
            _stringBuilder.Append(message.Message);
            
            return _stringBuilder.ToString();
        }
        
        protected override void DisposeCore()
        {
            _stringBuilder = null;
            _config = null;
        }
    }
}
