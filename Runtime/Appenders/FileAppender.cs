using System;
using System.IO;
using System.Text;
using System.Threading;
using EZLogger.Utils;
using UnityEngine;

namespace EZLogger.Appenders
{
    /// <summary>
    /// 文件日志输出器 - 将日志写入文件
    /// </summary>
    public class FileAppender : LogAppenderBase
    {
        private FileOutputConfig? _config;
        private TimezoneConfig? _timezoneConfig;
        private FileStream? _fileStream;
        private StreamWriter? _streamWriter;
        private string? _currentFilePath;

        // 线程安全相关
        private readonly object _fileLock = new object();
        private readonly object _queueLock = new object();

        // 写入线程相关
        private Thread? _writeThread;
        private volatile bool _isWriteThreadRunning;
        private readonly System.Collections.Generic.Queue<LogMessage> _messageQueue = new System.Collections.Generic.Queue<LogMessage>();

        // 文件日期（在实例化时确定，不再变更）
        private readonly DateTime _fileDate;

        // 字符串构建缓存 - 每个FileAppender实例独享，线程安全由WriteThread保证
        private readonly StringBuilder _stringBuilder = new StringBuilder(512);

        // WebGL模式专用字段
        private readonly System.Collections.Generic.List<LogMessage> _webglQueue = new System.Collections.Generic.List<LogMessage>();
        private WebGLPerformanceConfig? _webglConfig;

        public override string Name => "FileAppender";
        public override bool SupportsAsyncWrite => PlatformCapabilities.SupportsThreading;

        /// <summary>WebGL平台需要Update驱动</summary>
        public override bool RequiresUpdate => !PlatformCapabilities.SupportsThreading;

        /// <summary>
        /// 构造函数 - 在实例化时确定文件日期
        /// </summary>
        public FileAppender()
        {
            // 使用默认时区配置确定文件日期
            var defaultTimezone = new TimezoneConfig();
            _fileDate = defaultTimezone.GetCurrentTime().Date;
        }

        /// <summary>
        /// 构造函数 - 使用指定时区配置确定文件日期
        /// </summary>
        public FileAppender(TimezoneConfig timezoneConfig)
        {
            _fileDate = timezoneConfig.GetCurrentTime().Date;
        }

        /// <summary>
        /// 核心初始化逻辑
        /// </summary>
        protected override void InitializeCore(object config)
        {
            _config = config as FileOutputConfig ?? new FileOutputConfig();

            if (_config.Enabled)
            {
                Debug.Log($"[EZLogger] 初始化文件输出器: {_config.LogDirectory}, 文件日期: {_fileDate:yyyy-MM-dd}");
                OpenLogFile();

                if (PlatformCapabilities.SupportsThreading)
                {
                    // 多线程平台：使用原有逻辑
                    StartWriteThread();
                }
                else
                {
                    // WebGL平台：初始化WebGL配置和监控
                    InitializeWebGLMode();
                }
            }
        }

        /// <summary>
        /// 初始化文件输出器，支持传入时区配置
        /// </summary>
        public void Initialize(FileOutputConfig config, TimezoneConfig timezoneConfig)
        {
            _timezoneConfig = timezoneConfig;
            Initialize(config);
        }

        /// <summary>
        /// 核心写入逻辑（根据平台选择处理方式）
        /// </summary>
        protected override void WriteLogCore(LogMessage message)
        {
            if (_config?.Enabled != true)
                return;

            if (PlatformCapabilities.SupportsThreading)
            {
                // 多线程平台：使用原有队列逻辑
                lock (_queueLock)
                {
                    _messageQueue.Enqueue(message);
                }
            }
            else
            {
                // WebGL平台：加入WebGL队列等待Update处理
                EnqueueForWebGL(message);
            }
        }

        /// <summary>
        /// 启动写入线程
        /// </summary>
        private void StartWriteThread()
        {
            if (_writeThread != null)
                return;

            _isWriteThreadRunning = true;
            _writeThread = new Thread(WriteThreadProc)
            {
                Name = "FileAppender-Writer",
                IsBackground = true
            };
            _writeThread.Start();
        }

        /// <summary>
        /// 写入线程处理过程
        /// </summary>
        private void WriteThreadProc()
        {
            while (_isWriteThreadRunning)
            {
                try
                {
                    LogMessage? message = null;

                    lock (_queueLock)
                    {
                        if (_messageQueue.Count > 0)
                        {
                            message = _messageQueue.Dequeue();
                        }
                    }

                    if (message.HasValue)
                    {
                        WriteToFile(message.Value);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (ThreadAbortException)
                {
                    // 线程被主动终止，正常行为
                    break;
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 写入消息到文件
        /// </summary>
        private void WriteToFile(LogMessage message)
        {
            if (_streamWriter == null)
                return;

            lock (_fileLock)
            {
                try
                {
                    string logEntry = FormatLogMessage(message);
                    _streamWriter.WriteLine(logEntry);
                    _streamWriter.Flush();
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 格式化日志消息 - 零GC实现，包含帧率和线程信息
        /// 在写入线程中调用，线程安全由调用上下文保证
        /// </summary>
        private string FormatLogMessage(LogMessage message)
        {
            _stringBuilder.Clear();

            // 构建时间戳部分 - 手动格式化避免ToString分配
            var dt = message.Timestamp;
            // 日志条目开始标记 - 便于解析和区分多行日志
            _stringBuilder.Append(_config.LogEntryPrefix);
            // 小时
            if (dt.Hour < 10) _stringBuilder.Append('0');
            _stringBuilder.Append(dt.Hour);
            _stringBuilder.Append(':');

            // 分钟
            if (dt.Minute < 10) _stringBuilder.Append('0');
            _stringBuilder.Append(dt.Minute);
            _stringBuilder.Append(':');

            // 秒
            if (dt.Second < 10) _stringBuilder.Append('0');
            _stringBuilder.Append(dt.Second);
            _stringBuilder.Append('.');

            // 毫秒
            var ms = dt.Millisecond;
            if (ms < 100) _stringBuilder.Append('0');
            if (ms < 10) _stringBuilder.Append('0');
            _stringBuilder.Append(ms);
            _stringBuilder.Append("[");
            _stringBuilder.Append(message.Level.ToLevelString());
            _stringBuilder.Append("]");
            _stringBuilder.Append("[F:");
            _stringBuilder.Append(message.FrameCount);
            _stringBuilder.Append("]");

            // 添加线程ID（如果启用）
            if (_config?.ShowThreadId == true)
            {
                _stringBuilder.Append("[T:");
                _stringBuilder.Append(message.ThreadId);
                _stringBuilder.Append("]");
            }

            _stringBuilder.Append("[");
            _stringBuilder.Append(message.Tag);
            _stringBuilder.Append("] ");
            _stringBuilder.Append(message.Message);

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// 打开日志文件
        /// </summary>
        private void OpenLogFile()
        {
            try
            {
                string logDir = GetLogDirectoryPath();
                string fileName = BuildFileName();
                _currentFilePath = Path.Combine(logDir, fileName);

                _fileStream = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);

                // 写入启动标记
                _streamWriter.WriteLine(BuildStartMessage());
                _streamWriter.Flush();
                Debug.Log($"[EZLogger] 打开日志文件: {_currentFilePath}");
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        /// <summary>
        /// 获取日志目录路径
        /// </summary>
        private string GetLogDirectoryPath()
        {
            string logDir = _config?.LogDirectory ?? "";
            logDir = Path.Combine(UnityEngine.Application.persistentDataPath, logDir);

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            return logDir;
        }





        /// <summary>
        /// 获取配置的时间
        /// </summary>
        private DateTime GetConfiguredTime()
        {
            // 🎯 智能时区处理：使用存储的时区配置，避免循环调用

            // 如果有时区配置，使用它
            if (_timezoneConfig != null)
            {
                try
                {
                    return _timezoneConfig.GetCurrentTime();
                }
                catch
                {
                    // 配置的时区有问题，回退到UTC
                }
            }

            // 默认使用UTC时间（初始化时或配置无效时）
            return DateTime.UtcNow;
        }

        protected override void FlushCore()
        {
            // 等待写入队列清空
            int waitCount = 0;
            while (waitCount < 100)
            {
                bool isEmpty;
                lock (_queueLock)
                {
                    isEmpty = _messageQueue.Count == 0;
                }

                if (isEmpty)
                    break;

                Thread.Sleep(10);
                waitCount++;
            }

            // 刷新文件流
            lock (_fileLock)
            {
                try
                {
                    _streamWriter?.Flush();
                    _fileStream?.Flush();
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        protected override void DisposeCore()
        {
            // 日期轮转无需特殊清理

            // 停止写入线程
            _isWriteThreadRunning = false;
            if (_writeThread != null && _writeThread.IsAlive)
            {
                if (!_writeThread.Join(1000))
                {
                    // 如果线程在1秒内没有正常结束，强制终止
                    try
                    {
                        _writeThread.Abort();
                    }
                    catch (ThreadAbortException)
                    {
                        // 忽略线程终止异常
                    }
                }
            }

            // 关闭文件流
            lock (_fileLock)
            {
                try
                {
                    _streamWriter?.Close();
                    _streamWriter?.Dispose();
                    _fileStream?.Close();
                    _fileStream?.Dispose();
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 构建文件名 - 零GC实现
        /// </summary>
        private string BuildFileName()
        {
            var sb = new StringBuilder(32); // 临时StringBuilder，局部作用域
            var template = _config?.FileNameTemplate ?? "log_{0:yyyyMMdd}.txt";
            // 使用实例化时确定的文件日期，而不是动态获取时间

            // 解析文件名模板 - 替换{0:yyyyMMdd}格式
            if (template.Contains("{0:yyyyMMdd}"))
            {
                sb.Append("log_");
                // 手动格式化日期避免ToString分配
                var year = _fileDate.Year;
                sb.Append((char)('0' + year / 1000));
                sb.Append((char)('0' + (year / 100) % 10));
                sb.Append((char)('0' + (year / 10) % 10));
                sb.Append((char)('0' + year % 10));

                var month = _fileDate.Month;
                if (month < 10) sb.Append('0');
                sb.Append(month);

                var day = _fileDate.Day;
                if (day < 10) sb.Append('0');
                sb.Append(day);

                sb.Append(".txt");
            }
            else
            {
                // 如果模板不匹配，直接使用模板
                sb.Append(template);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 构建启动消息 - 零GC实现
        /// </summary>
        private string BuildStartMessage()
        {
            var sb = new StringBuilder(64);
            var startTime = GetConfiguredTime();
            sb.Append(_config.LogEntryPrefix);

            // 时间格式 HH:mm:ss:fff
            if (startTime.Hour < 10) sb.Append('0');
            sb.Append(startTime.Hour);
            sb.Append(':');

            if (startTime.Minute < 10) sb.Append('0');
            sb.Append(startTime.Minute);
            sb.Append(':');

            if (startTime.Second < 10) sb.Append('0');
            sb.Append(startTime.Second);
            sb.Append(':');

            var ms = startTime.Millisecond;
            if (ms < 100) sb.Append('0');
            if (ms < 10) sb.Append('0');
            sb.Append(ms);

            sb.Append(" [Log] [FileAppender] Log started");
            return sb.ToString();
        }

        /// <summary>
        /// 构建错误消息 - 零GC实现
        /// </summary>
        private string BuildErrorMessage(string errorMessage)
        {
            var sb = new StringBuilder(256);
            sb.Append("[FileAppender] Error: ");
            sb.Append(errorMessage);
            return sb.ToString();
        }

        /// <summary>处理内部错误</summary>
        protected override void HandleInternalError(Exception ex)
        {
            // 避免无限递归，直接输出到Unity控制台
            UnityEngine.Debug.LogError(BuildErrorMessage(ex.Message));
        }

        #region WebGL平台专用方法

        /// <summary>
        /// 初始化WebGL模式
        /// </summary>
        private void InitializeWebGLMode()
        {
            _webglConfig = WebGLPerformanceConfig.CreateDefault();
            UnityEngine.Debug.Log($"[FileAppender] WebGL模式已启用 - {_webglConfig}");
        }

        /// <summary>
        /// WebGL平台的消息入队（WebGL是单线程，不需要锁）
        /// </summary>
        private void EnqueueForWebGL(LogMessage message)
        {
            _webglQueue.Add(message);

            // 队列溢出保护
            if (_webglQueue.Count > _webglConfig?.MaxQueueSize)
            {
                HandleQueueOverflow();
            }
        }

        /// <summary>
        /// 处理队列溢出
        /// </summary>
        private void HandleQueueOverflow()
        {
            if (_webglConfig == null) return;

            int removeCount = _webglQueue.Count - _webglConfig.MaxQueueSize + _webglConfig.BatchSize;

            switch (_webglConfig.OverflowStrategy)
            {
                case QueueOverflowStrategy.DropOldest:
                    for (int i = 0; i < removeCount && _webglQueue.Count > 0; i++)
                    {
                        _webglQueue.RemoveAt(0);
                    }
                    break;

                case QueueOverflowStrategy.DropNewest:
                    for (int i = 0; i < removeCount && _webglQueue.Count > 0; i++)
                    {
                        _webglQueue.RemoveAt(_webglQueue.Count - 1);
                    }
                    break;

                case QueueOverflowStrategy.Block:
                    // 不移除，但这可能导致内存问题
                    break;
            }
        }

        /// <summary>
        /// WebGL平台的Update处理 - 分帧写入（单线程，无需锁）
        /// </summary>
        public override float Update()
        {
            if (!RequiresUpdate || _config?.Enabled != true || _webglConfig == null)
                return 0f;

            float startTime = UnityEngine.Time.realtimeSinceStartup * 1000f; // 转换为毫秒
            int processedCount = 0;

            // 批量处理队列中的消息
            while (processedCount < _webglConfig.BatchSize && _webglQueue.Count > 0)
            {
                LogMessage message = _webglQueue[0];
                _webglQueue.RemoveAt(0);

                // 写入单条消息
                WriteToFileSync(message);
                processedCount++;

                // 检查耗时，避免超出预算
                float elapsedTime = (UnityEngine.Time.realtimeSinceStartup * 1000f) - startTime;
                if (elapsedTime >= _webglConfig.MaxUpdateTimePerFrame - 1.0f) // 留1ms缓冲
                    break;
            }

            return (UnityEngine.Time.realtimeSinceStartup * 1000f) - startTime;
        }

        /// <summary>
        /// 同步写入文件（WebGL平台使用）
        /// </summary>
        private void WriteToFileSync(LogMessage message)
        {
            if (_streamWriter == null)
                return;

            lock (_fileLock)
            {
                try
                {
                    string logEntry = FormatLogMessage(message);
                    _streamWriter.WriteLine(logEntry);
                    _streamWriter.Flush();
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 更新WebGL性能配置
        /// </summary>
        public void UpdateWebGLConfig(WebGLPerformanceConfig? config)
        {
            if (config != null && config.Validate())
            {
                _webglConfig = config;
            }
        }

        #endregion

    }
}
