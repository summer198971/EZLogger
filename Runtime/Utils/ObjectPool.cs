using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EZLogger.Utils
{
    /// <summary>
    /// 简单的对象池实现，用于减少GC分配
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public class ObjectPool<T> where T : class
    {
        private readonly ConcurrentQueue<T> _objects = new ConcurrentQueue<T>();
        private readonly Func<T> _objectFactory;
        private readonly Action<T> _resetAction;
        private readonly int _maxSize;
        private int _currentSize;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="objectFactory">对象创建工厂</param>
        /// <param name="resetAction">对象重置操作</param>
        /// <param name="maxSize">最大池大小</param>
        public ObjectPool(Func<T> objectFactory, Action<T> resetAction = null, int maxSize = 100)
        {
            _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
            _resetAction = resetAction;
            _maxSize = maxSize;
        }

        /// <summary>
        /// 从池中获取对象
        /// </summary>
        /// <returns>对象实例</returns>
        public T Get()
        {
            if (_objects.TryDequeue(out T item))
            {
                System.Threading.Interlocked.Decrement(ref _currentSize);
                return item;
            }

            return _objectFactory();
        }

        /// <summary>
        /// 将对象返回到池中（线程安全）
        /// </summary>
        /// <param name="item">要返回的对象</param>
        public void Return(T item)
        {
            if (item == null)
                return;

            // 重置对象状态
            _resetAction?.Invoke(item);

            // 线程安全的大小检查：使用原子操作尝试增加计数
            int newSize = System.Threading.Interlocked.Increment(ref _currentSize);

            // 如果超过最大大小，回退计数并退出
            if (newSize > _maxSize)
            {
                System.Threading.Interlocked.Decrement(ref _currentSize);
                return;
            }

            // 安全地入队
            _objects.Enqueue(item);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            while (_objects.TryDequeue(out _))
            {
                System.Threading.Interlocked.Decrement(ref _currentSize);
            }
        }

        /// <summary>
        /// 当前池中对象数量
        /// </summary>
        public int Count => _currentSize;
    }

    // 注意: StringBuilderPool 已被移除
    // 每个 Appender 类现在独立管理自己的 StringBuilder 实例
    // 这样避免了线程安全的复杂性，架构更清晰
}
