using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using EZLogger.Utils;

namespace EZLogger.Appenders
{
    /// <summary>
    /// 文件输出器，支持异步写入和文件轮转
    /// </summary>
    public class FileAppender : LogAppenderBase
    {
        public override string Name => "File";

        /// <summary>文件输出器支持异步写入，避免IO阻塞主线程</summary>
        public override bool SupportsAsyncWrite => true;

        private FileOutputConfig _config;
        private FileStream _fileStream;
        private StreamWriter _streamWriter;
        private string _currentFilePath;
        private readonly object _fileLock = new object();
        private Timer _sizeCheckTimer;
        private ThreadSafeQueue<string> _writeQueue;
        private Thread _writeThread;
        private volatile bool _isWriteThreadRunning;

        protected override void InitializeCore(object config)
        {
            _config = config as FileOutputConfig ?? new FileOutputConfig();

            // 创建日志目录
            CreateLogDirectory();

            // 打开日志文件
            OpenLogFile();

            // 启动异步写入线程
            if (_config.Enabled)
            {
                _writeQueue = new ThreadSafeQueue<string>(1000);
                _isWriteThreadRunning = true;
                _writeThread = new Thread(WriteThreadProc)
                {
                    Name = "FileAppender-Writer",
                    IsBackground = true
                };
                _writeThread.Start();

                // 启动文件大小检查定时器
                if (_config.EnableSizeCheck)
                {
                    var interval = TimeSpan.FromSeconds(_config.SizeCheckInterval);
                    _sizeCheckTimer = new Timer(CheckFileSize, null, interval, interval);
                }
            }
        }

        protected override void WriteLogCore(LogMessage message)
        {
            if (!_config.Enabled || _writeQueue == null)
                return;

            string formattedMessage = FormatMessage(message);

            // 异步写入队列
            if (!_writeQueue.Enqueue(formattedMessage))
            {
                // 队列满了，强制入队
                _writeQueue.ForceEnqueue(formattedMessage);
            }
        }

        private string FormatMessage(LogMessage message)
        {
            return StringBuilderPool.Build(sb =>
            {
                sb.Append("[!@#]");
                sb.Append(message.Timestamp.ToString("HH:mm:ss:fff"));
                sb.Append(" [");
                sb.Append(message.Level.ToString());
                sb.Append("] [");
                sb.Append(message.Tag);
                sb.Append("] ");
                sb.Append(message.Message);

                // 添加堆栈跟踪信息
                if (!string.IsNullOrEmpty(message.StackTrace))
                {
                    sb.AppendLine();
                    sb.Append(message.StackTrace);
                }
            });
        }

        private void CreateLogDirectory()
        {
            string logPath = GetLogDirectoryPath();
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
        }

        private string GetLogDirectoryPath()
        {
#if UNITY_2018_1_OR_NEWER
            return Path.Combine(UnityEngine.Application.persistentDataPath, _config.LogDirectory);
#else
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Logs");
#endif
        }

        private void OpenLogFile()
        {
            try
            {
                string logDir = GetLogDirectoryPath();
                string fileName = string.Format(_config.FileNameTemplate, DateTime.Now);
                _currentFilePath = Path.Combine(logDir, fileName);

                _fileStream = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);

                // 写入启动标记
                _streamWriter.WriteLine($"[!@#]{DateTime.Now:HH:mm:ss:fff} [INFO] [FileAppender] Log started");
                _streamWriter.Flush();
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        private void WriteThreadProc()
        {
            while (_isWriteThreadRunning)
            {
                try
                {
                    if (_writeQueue.TryDequeue(out string message))
                    {
                        WriteToFile(message);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }

            // 写入剩余的消息
            FlushRemainingMessages();
        }

        private void WriteToFile(string message)
        {
            lock (_fileLock)
            {
                try
                {
                    _streamWriter?.WriteLine(message);
                    _streamWriter?.Flush();
                }
                catch (ObjectDisposedException)
                {
                    // 文件已关闭，停止写入
                    _isWriteThreadRunning = false;
                }
                catch (Exception ex)
                {
                    HandleInternalError(ex);
                }
            }
        }

        private void FlushRemainingMessages()
        {
            var remaining = _writeQueue.DequeueBatch(1000);
            foreach (var message in remaining)
            {
                WriteToFile(message);
            }
        }

        private void CheckFileSize(object state)
        {
            if (_currentFilePath == null || !File.Exists(_currentFilePath))
                return;

            try
            {
                var fileInfo = new FileInfo(_currentFilePath);
                if (fileInfo.Length > _config.MaxFileSize)
                {
                    TrimLogFile();
                }
            }
            catch (Exception ex)
            {
                HandleInternalError(ex);
            }
        }

        private void TrimLogFile()
        {
            lock (_fileLock)
            {
                try
                {
                    // 读取文件内容
                    _streamWriter?.Flush();

                    byte[] fileContent;
                    using (var readStream = new FileStream(_currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        fileContent = new byte[readStream.Length];
                        readStream.Read(fileContent, 0, fileContent.Length);
                    }

                    if (fileContent.Length > _config.KeepSize)
                    {
                        // 保留后面的部分
                        long trimSize = fileContent.Length - _config.KeepSize;
                        byte[] newContent = new byte[_config.KeepSize];
                        Array.Copy(fileContent, trimSize, newContent, 0, _config.KeepSize);

                        // 关闭当前流
                        _streamWriter?.Close();
                        _fileStream?.Close();

                        // 写入新内容
                        File.WriteAllBytes(_currentFilePath, newContent);

                        // 重新打开文件
                        _fileStream = new FileStream(_currentFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        _streamWriter = new StreamWriter(_fileStream, Encoding.UTF8);

                        // 记录裁剪操作
                        string trimMessage = $"[!@#]{DateTime.Now:HH:mm:ss:fff} [INFO] [FileAppender] File trimmed, removed {trimSize} bytes";
                        _streamWriter.WriteLine(trimMessage);
                        _streamWriter.Flush();
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
        }

        protected override void FlushCore()
        {
            // 等待写入队列清空
            int waitCount = 0;
            while (_writeQueue != null && !_writeQueue.IsEmpty && waitCount < 100)
            {
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
            _writeThread?.Join(1000);

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
                finally
                {
                    _streamWriter = null;
                    _fileStream = null;
                }
            }

            _writeQueue?.Clear();
            _writeQueue = null;
        }
    }
}
