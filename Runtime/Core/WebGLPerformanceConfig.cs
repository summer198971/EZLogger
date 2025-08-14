using System;
using UnityEngine;

namespace EZLogger
{
    /// <summary>
    /// WebGL平台性能配置类 - 控制分帧写入参数以保证帧率稳定
    /// </summary>
    [Serializable]
    public class WebGLPerformanceConfig
    {
        [Tooltip("每帧最大处理时间(毫秒) - 超过此时间会暂停到下一帧")]
        [Range(1f, 16f)]
        public float MaxUpdateTimePerFrame = 5.0f;

        [Tooltip("每次批处理的消息数量 - 控制单次处理的消息上限")]
        [Range(1, 50)]
        public int BatchSize = 10;

        [Tooltip("WebGL队列最大长度 - 防止内存无限增长")]
        [Range(100, 5000)]
        public int MaxQueueSize = 1000;

        [Tooltip("队列溢出时的处理策略")]
        public QueueOverflowStrategy OverflowStrategy = QueueOverflowStrategy.DropOldest;



        /// <summary>
        /// 创建默认的WebGL性能配置
        /// </summary>
        public static WebGLPerformanceConfig CreateDefault()
        {
            return new WebGLPerformanceConfig
            {
                MaxUpdateTimePerFrame = 5.0f,
                BatchSize = 10,
                MaxQueueSize = 1000,
                OverflowStrategy = QueueOverflowStrategy.DropOldest
            };
        }

        /// <summary>
        /// 创建高性能配置（减少每帧处理时间，提高流畅度）
        /// </summary>
        public static WebGLPerformanceConfig CreateHighPerformance()
        {
            return new WebGLPerformanceConfig
            {
                MaxUpdateTimePerFrame = 2.0f,
                BatchSize = 5,
                MaxQueueSize = 500,
                OverflowStrategy = QueueOverflowStrategy.DropOldest
            };
        }

        /// <summary>
        /// 创建高吞吐量配置（增加处理能力，可能影响帧率）
        /// </summary>
        public static WebGLPerformanceConfig CreateHighThroughput()
        {
            return new WebGLPerformanceConfig
            {
                MaxUpdateTimePerFrame = 8.0f,
                BatchSize = 20,
                MaxQueueSize = 2000,
                OverflowStrategy = QueueOverflowStrategy.DropOldest
            };
        }

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        public bool Validate()
        {
            if (MaxUpdateTimePerFrame <= 0f || MaxUpdateTimePerFrame > 16f)
                return false;

            if (BatchSize <= 0 || BatchSize > 100)
                return false;

            if (MaxQueueSize <= 0 || MaxQueueSize > 10000)
                return false;



            return true;
        }

        /// <summary>
        /// 获取配置描述信息
        /// </summary>
        public override string ToString()
        {
            return $"WebGL性能配置: 每帧{MaxUpdateTimePerFrame}ms, 批次{BatchSize}, 队列{MaxQueueSize}, 策略{OverflowStrategy}";
        }
    }

    /// <summary>
    /// 队列溢出处理策略
    /// </summary>
    public enum QueueOverflowStrategy
    {
        /// <summary>丢弃最老的消息</summary>
        DropOldest,

        /// <summary>丢弃最新的消息</summary>
        DropNewest,

        /// <summary>阻塞新消息（不推荐，可能影响性能）</summary>
        Block
    }


}
