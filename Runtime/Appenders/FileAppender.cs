using System;
using System.IO;
using System.Text;
using System.Threading;

namespace EZLogger.Appenders
{
    /// <summary>
    /// 文件日志输出器 - 将日志写入文件
    /// </summary>
    public class FileAppender : LogAppenderBase
    {
        private FileOutputConfig? _config;
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
        /// 格式化日志消息
        /// </summary>
        private string FormatLogMessage(LogMessage message)
        {
            var timestamp = message.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return $"[{timestamp}] [{message.Level}] [{message.Tag}] {message.Message}";
        }

        /// <summary>
        /// 打开日志文件
        /// </summary>
        private void OpenLogFile()
        {
            try
            {
                string logDir = GetLogDirectoryPath();
                string fileName = string.Format(_config?.FileNameTemplate ?? "log_{0:yyyyMMdd}.txt", GetConfiguredTime());
                _currentFilePath = Path.Combine(logDir, fileName);

                _fileStream = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);

                // 写入启动标记
                _streamWriter.WriteLine($"[!@#]{GetConfiguredTime():HH:mm:ss:fff} [INFO] [FileAppender] Log started");
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

            if (string.IsNullOrEmpty(logDir))
            {
                logDir = Path.Combine(UnityEngine.Application.persistentDataPath, "Logs");
            }

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
                        string trimMessage = $"[!@#]{GetConfiguredTime():HH:mm:ss:fff} [INFO] [FileAppender] File trimmed, removed {trimSize} bytes";
                        _streamWriter.WriteLine(trimMessage);
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
            // 🚨 关键修复：避免在初始化期间调用单例，防止死循环
            // 直接使用UTC时间，避免递归调用EZLoggerManager.Instance
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

        /// <summary>处理内部错误</summary>
        protected override void HandleInternalError(Exception ex)
        {
            // 避免无限递归，直接输出到Unity控制台
            UnityEngine.Debug.LogError($"[FileAppender] Error: {ex.Message}");
        }
    }
}
