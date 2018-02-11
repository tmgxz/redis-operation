using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_Redis.Redis.Serializer
{
    /// <summary>
    /// 使用Newtonsoft.Json实现序列化器
    /// StackExchange.Redis使用Encoding.UTF8将strings转换为bytes
    /// </summary>
    public class NewtonsoftSerializer : ISerializer
    {
        private static readonly Encoding encoding = Encoding.UTF8;

        private readonly JsonSerializerSettings settings;

        public NewtonsoftSerializer(JsonSerializerSettings settings = null)
        {
            this.settings = settings ?? new JsonSerializerSettings();
        }

        /// <summary>
        /// 序列化指定元素
        /// </summary>
        /// <param name="item">确保为对象类型而非字符串</param>
        /// <returns></returns>
        public byte[] Serialize(object item)
        {
            var jsonString = JsonConvert.SerializeObject(item, settings);
            return encoding.GetBytes(jsonString);
        }

        /// <summary>
        /// 序列化指定元素（异步）
        /// </summary>
        /// <param name="item">确保为对象类型而非字符串</param>
        /// <returns></returns>
        public async Task<byte[]> SerializeAsync(object item)
        {
            var jsonString = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(item, settings));
            return encoding.GetBytes(jsonString);
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        public object Deserialize(byte[] serializedObject)
        {
            var jsonString = encoding.GetString(serializedObject);
            return JsonConvert.DeserializeObject(jsonString, typeof(object));
        }

        /// <summary>
        /// 反序列化（异步）
        /// </summary>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        public Task<object> DeserializeAsync(byte[] serializedObject)
        {
            return Task.Factory.StartNew(() => Deserialize(serializedObject));
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        public T Deserialize<T>(byte[] serializedObject)
        {
            var jsonString = encoding.GetString(serializedObject);
            return JsonConvert.DeserializeObject<T>(jsonString, settings);
        }

        /// <summary>
        /// 反序列化（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serializedObject"></param>
        /// <returns></returns>
        public Task<T> DeserializeAsync<T>(byte[] serializedObject)
        {
            return Task.Factory.StartNew(() => Deserialize<T>(serializedObject));
        }
    }
}
