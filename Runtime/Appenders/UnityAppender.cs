using System.Text;

namespace EZLogger.Appenders
{
    /// <summary>
    /// Unity控制台输出器
    /// </summary>
    public class UnityAppender : LogAppenderBase
    {
        public override string Name => "Unity Console";

        /// <summary>Unity控制台输出器必须同步，以保证与Unity原生Debug API的顺序一致</summary>
        public override bool SupportsAsyncWrite => false;

        /// <summary>Unity控制台输出器不需要Update驱动，因为它是同步的</summary>
        public override bool RequiresUpdate => false;

        private UnityConsoleConfig _config = new UnityConsoleConfig();

        /// <summary>专用StringBuilder缓存，避免重复分配</summary>
        private readonly StringBuilder _stringBuilder = new StringBuilder(256);

        protected override void InitializeCore(object config)
        {
            _config = config as UnityConsoleConfig ?? new UnityConsoleConfig();

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
                    // 零GC实现异常日志格式化
                    _stringBuilder.Clear();
                    _stringBuilder.Append("[Exception] ");
                    _stringBuilder.Append(formattedMessage);
                    UnityEngine.Debug.LogError(_stringBuilder.ToString());
                    break;
                case UnityEngine.LogType.Log:
                default:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
            }
        }

        /// <summary>
        /// 格式化消息 - 零GC实现，使用实例StringBuilder
        /// </summary>
        private string FormatMessage(LogMessage message)
        {
            _stringBuilder.Clear();
#if UNITY_EDITOR
            if (_config.EnableColors)
            {
                _stringBuilder.Append("<color=");
                _stringBuilder.Append(message.Level.GetUnityColor());
                _stringBuilder.Append(">");
            }
#endif
            _stringBuilder.Append("[F:");
            _stringBuilder.Append(message.FrameCount);
            _stringBuilder.Append("]");

            // 添加线程ID
            if (_config.ShowThreadId)
            {
                _stringBuilder.Append("[T:");
                _stringBuilder.Append(message.ThreadId);
                _stringBuilder.Append("]");
            }

            // 添加标签
            _stringBuilder.Append("[");
            _stringBuilder.Append(message.Tag);
            _stringBuilder.Append("]");
#if UNITY_EDITOR
            if (_config.EnableColors)
            {
                _stringBuilder.Append("</color>");
            }
#endif
            // 添加消息内容
            _stringBuilder.Append(" ");
            _stringBuilder.Append(message.Message);

            return _stringBuilder.ToString();
        }

        protected override void DisposeCore()
        {
            // 配置清理
        }
    }
}
