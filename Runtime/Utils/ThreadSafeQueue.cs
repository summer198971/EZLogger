using System.Collections.Generic;
using System.Threading;

namespace EZLogger.Utils
{
    /// <summary>
    /// 线程安全的队列实现
    /// </summary>
    /// <typeparam name="T">队列元素类型</typeparam>
    public class ThreadSafeQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly object _lock = new object();
        private readonly int _maxCapacity;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxCapacity">最大容量，0表示无限制</param>
        public ThreadSafeQueue(int maxCapacity = 0)
        {
            _maxCapacity = maxCapacity;
        }
        
        /// <summary>
        /// 当前队列大小
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }
        
        /// <summary>
        /// 是否为空
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count == 0;
                }
            }
        }
        
        /// <summary>
        /// 入队
        /// </summary>
        /// <param name="item">要入队的元素</param>
        /// <returns>是否成功入队</returns>
        public bool Enqueue(T item)
        {
            lock (_lock)
            {
                // 检查容量限制
                if (_maxCapacity > 0 && _queue.Count >= _maxCapacity)
                {
                    return false; // 队列已满
                }
                
                _queue.Enqueue(item);
                return true;
            }
        }
        
        /// <summary>
        /// 强制入队（如果队列满了会移除最旧的元素）
        /// </summary>
        /// <param name="item">要入队的元素</param>
        public void ForceEnqueue(T item)
        {
            lock (_lock)
            {
                // 如果队列满了，移除最旧的元素
                if (_maxCapacity > 0 && _queue.Count >= _maxCapacity)
                {
                    _queue.Dequeue();
                }
                
                _queue.Enqueue(item);
            }
        }
        
        /// <summary>
        /// 出队
        /// </summary>
        /// <param name="item">出队的元素</param>
        /// <returns>是否成功出队</returns>
        public bool TryDequeue(out T item)
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }
                
                item = default(T);
                return false;
            }
        }
        
        /// <summary>
        /// 批量出队
        /// </summary>
        /// <param name="maxCount">最大出队数量</param>
        /// <returns>出队的元素列表</returns>
        public List<T> DequeueBatch(int maxCount)
        {
            var result = new List<T>();
            
            lock (_lock)
            {
                int count = System.Math.Min(maxCount, _queue.Count);
                for (int i = 0; i < count; i++)
                {
                    result.Add(_queue.Dequeue());
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 清空队列
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }
        
        /// <summary>
        /// 查看队首元素但不移除
        /// </summary>
        /// <param name="item">队首元素</param>
        /// <returns>是否有元素</returns>
        public bool TryPeek(out T item)
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Peek();
                    return true;
                }
                
                item = default(T);
                return false;
            }
        }
    }
}
