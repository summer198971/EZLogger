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
        /// 将对象返回到池中
        /// </summary>
        /// <param name="item">要返回的对象</param>
        public void Return(T item)
        {
            if (item == null)
                return;
                
            // 检查池大小限制
            if (_currentSize >= _maxSize)
                return;
                
            // 重置对象状态
            _resetAction?.Invoke(item);
            
            _objects.Enqueue(item);
            System.Threading.Interlocked.Increment(ref _currentSize);
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
    
    /// <summary>
    /// StringBuilder对象池
    /// </summary>
    public static class StringBuilderPool
    {
        private static readonly ObjectPool<System.Text.StringBuilder> _pool = 
            new ObjectPool<System.Text.StringBuilder>(
                () => new System.Text.StringBuilder(256),
                sb => sb.Clear(),
                50
            );
        
        /// <summary>
        /// 获取StringBuilder实例
        /// </summary>
        public static System.Text.StringBuilder Get()
        {
            return _pool.Get();
        }
        
        /// <summary>
        /// 返回StringBuilder实例
        /// </summary>
        public static void Return(System.Text.StringBuilder sb)
        {
            if (sb != null && sb.Capacity <= 4096) // 避免缓存过大的StringBuilder
            {
                _pool.Return(sb);
            }
        }
        
        /// <summary>
        /// 使用StringBuilder并自动返回池中
        /// </summary>
        public static string Build(System.Action<System.Text.StringBuilder> buildAction)
        {
            var sb = Get();
            try
            {
                buildAction(sb);
                return sb.ToString();
            }
            finally
            {
                Return(sb);
            }
        }
    }
}
