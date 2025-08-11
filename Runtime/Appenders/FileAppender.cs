using System;
using System.IO;
using System.Text;
using System.Threading;
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

        // 文件大小检查相关
        private Timer? _sizeCheckTimer;
        private readonly object _sizeCheckLock = new object();

        // 字符串构建缓存 - 每个FileAppender实例独享，线程安全由WriteThread保证
        private readonly StringBuilder _stringBuilder = new StringBuilder(512);

        public override string Name => "FileAppender";
        public override bool SupportsAsyncWrite => true;

        /// <summary>
        /// 核心初始化逻辑
        /// </summary>
        protected override void InitializeCore(object config)
        {
            _config = config as FileOutputConfig ?? new FileOutputConfig();

            if (_config.Enabled)
            {
                OpenLogFile();
                StartWriteThread();
                StartSizeCheckTimer();
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
        /// 核心写入逻辑
        /// </summary>
        protected override void WriteLogCore(LogMessage message)
        {
            if (_config?.Enabled != true)
                return;

            lock (_queueLock)
            {
                _messageQueue.Enqueue(message);
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
        /// 启动文件大小检查定时器
        /// </summary>
        private void StartSizeCheckTimer()
        {
            if (_config?.EnableSizeCheck != true || _config.SizeCheckInterval <= 0)
                return;

            _sizeCheckTimer = new Timer(CheckFileSize, null,
                TimeSpan.FromSeconds(_config.SizeCheckInterval),
                TimeSpan.FromSeconds(_config.SizeCheckInterval));
        }

        /// <summary>
        /// 检查文件大小
        /// </summary>
        private void CheckFileSize(object? state)
        {
            if (_config?.EnableSizeCheck != true)
                return;

            lock (_sizeCheckLock)
            {
                try
                {
                    if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
                        return;

                    var fileInfo = new FileInfo(_currentFilePath);
                    long fileSizeBytes = fileInfo.Length;
                    long maxSizeBytes = (long)(_config?.MaxFileSize ?? 0);

                    if (fileSizeBytes > maxSizeBytes)
                    {
                        TrimLogFile(fileInfo);
                    }
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        /// <summary>
        /// 裁剪日志文件
        /// </summary>
        private void TrimLogFile(FileInfo fileInfo)
        {
            try
            {
                lock (_fileLock)
                {
                    // 关闭当前流
                    _streamWriter?.Close();
                    _streamWriter?.Dispose();
                    _fileStream?.Close();
                    _fileStream?.Dispose();

                    // 读取文件后半部分内容
                    byte[] fileBytes = File.ReadAllBytes(fileInfo.FullName);
                    long keepBytes = (long)(_config?.KeepSize ?? 0);
                    long trimSize = fileBytes.Length - keepBytes;

                    if (keepBytes > 0 && keepBytes < fileBytes.Length)
                    {
                        byte[] keepData = new byte[keepBytes];
                        Array.Copy(fileBytes, trimSize, keepData, 0, keepBytes);

                        // 重写文件
                        File.WriteAllBytes(fileInfo.FullName, keepData);

                        // 重新打开文件
                        _fileStream = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);

                        // 记录裁剪操作
                        _streamWriter.WriteLine(BuildTrimMessage(trimSize));
                        _streamWriter.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
                // 尝试重新打开文件
                try
                {
                    OpenLogFile();
                }
                catch
                {
                    // 忽略重新打开失败
                }
            }
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
            // 停止大小检查定时器
            _sizeCheckTimer?.Dispose();
            _sizeCheckTimer = null;

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
            var currentTime = GetConfiguredTime();

            // 解析文件名模板 - 替换{0:yyyyMMdd}格式
            if (template.Contains("{0:yyyyMMdd}"))
            {
                sb.Append("log_");
                // 手动格式化日期避免ToString分配
                var year = currentTime.Year;
                sb.Append((char)('0' + year / 1000));
                sb.Append((char)('0' + (year / 100) % 10));
                sb.Append((char)('0' + (year / 10) % 10));
                sb.Append((char)('0' + year % 10));

                var month = currentTime.Month;
                if (month < 10) sb.Append('0');
                sb.Append(month);

                var day = currentTime.Day;
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

            sb.Append(" [INFO] [FileAppender] Log started");
            return sb.ToString();
        }

        /// <summary>
        /// 构建文件裁剪消息 - 零GC实现
        /// </summary>
        private string BuildTrimMessage(long trimSize)
        {
            var sb = new StringBuilder(128);
            var trimTime = GetConfiguredTime();
            sb.Append(_config.LogEntryPrefix);

            // 时间格式 HH:mm:ss:fff
            if (trimTime.Hour < 10) sb.Append('0');
            sb.Append(trimTime.Hour);
            sb.Append(':');

            if (trimTime.Minute < 10) sb.Append('0');
            sb.Append(trimTime.Minute);
            sb.Append(':');

            if (trimTime.Second < 10) sb.Append('0');
            sb.Append(trimTime.Second);
            sb.Append(':');

            var ms = trimTime.Millisecond;
            if (ms < 100) sb.Append('0');
            if (ms < 10) sb.Append('0');
            sb.Append(ms);

            sb.Append(" [INFO] [FileAppender] File trimmed, removed ");
            sb.Append(trimSize);
            sb.Append(" bytes");
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
    }
}
