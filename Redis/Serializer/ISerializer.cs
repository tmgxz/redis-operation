using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_Redis.Redis.Serializer
{
    /// <summary>
    /// 序列化器接口
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 序列化指定元素
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        byte[] Serialize(object item);

        /// <summary>
        /// 序列化指定元素（异步）
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        Task<byte[]> SerializeAsync(object item);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        object Deserialize(byte[] serializedObject);

        /// <summary>
        /// 反序列化（异步）
        /// </summary>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        Task<object> DeserializeAsync(byte[] serializedObject);

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        T Deserialize<T>(byte[] serializedObject);

        /// <summary>
        /// 反序列化（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        Task<T> DeserializeAsync<T>(byte[] serializedObject);
    }
}
