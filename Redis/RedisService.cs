using ConsoleApp_Redis.Redis.Serializer;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp_Redis.Redis.Extensions;
using System.Collections.Concurrent;

namespace ConsoleApp_Redis.Redis
{
    public class RedisService
    {
        #region 变量
        /// <summary>
        /// Redis连接缓存
        /// </summary>
        private static ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>> m_connectionMultiplexers = new ConcurrentDictionary<string, Lazy<ConnectionMultiplexer>>();

        /// <summary>
        /// Redis连接
        /// </summary>
        private readonly ConnectionMultiplexer m_connectionMultiplexer = null;
        
        /// <summary>
        /// Redis 数据库
        /// </summary>
        private IDatabase Database
        {
            get
            {
                return m_connectionMultiplexer.GetDatabase(DatabaseSerial);
            }
        }

        /// <summary>
        /// 序列化器
        /// </summary>
        public ISerializer Serializer { get; set; }
        /// <summary>
        /// Redis 数据库序号
        /// </summary>
        public int DatabaseSerial { get; set; }
        /// <summary>
        /// ReidsKey构建方法
        /// </summary>
        public Func<string,string> RedisKeyFunc { get; set; }

        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// 采用默认序列化器NewtonsoftSerializer
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="database"></param>
        public RedisService(string connStr, Func<string, string> keyFunc = null, int database = 0)
        {
            Serializer = new NewtonsoftSerializer();
            DatabaseSerial = database;
            RedisKeyFunc = keyFunc;
            m_connectionMultiplexer = GetConnection(connStr);
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="connStr">连接字符串</param>
        /// <param name="serializer">序列化器</param>
        /// <param name="database"></param>
        public RedisService(string connStr, ISerializer serializer, Func<string, string> keyFunc = null, int database = 0)
        {
            Serializer = serializer;
            DatabaseSerial = database;
            RedisKeyFunc = keyFunc;
            m_connectionMultiplexer = GetConnection(connStr);
        }

        /// <summary>
        /// 获得Redis连接
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private static ConnectionMultiplexer GetConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) throw new Exception("Redis 连接字符串为空");

            return m_connectionMultiplexers.GetOrAdd(connectionString,
                new Lazy<ConnectionMultiplexer>(() =>
                {
                    return ConnectionMultiplexer.Connect(connectionString);
                })).Value;
        }
        #endregion

        #region 全局命令

        #region RedisKey
        /// <summary>
        /// RedisKey构造方法
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private RedisKey GetRedisKey(string key)
        {
            return RedisKeyFunc == null ? key : RedisKeyFunc(key);
        }
        /// <summary>
        /// 设置过期时间段
        /// </summary>
        /// <param name="key"></param>
        /// <param name="span"></param>
        public void SetKeyExpire(string key, TimeSpan span)
        {
            string redisKey = GetRedisKey(key);
            Database.KeyExpire(redisKey, span);
        }
        #endregion

        #region Exists
        /// <summary>
        /// 检验指定的键是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(string key)
        {
            string redisKey = GetRedisKey(key);
            return Database.KeyExists(redisKey);
        }

        /// <summary>
        /// 检验指定的键是否存在（异步）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> ExistsAsync(string key)
        {
            string redisKey = GetRedisKey(key);
            return Database.KeyExistsAsync(redisKey);
        }
        #endregion

        #region Remove
        /// <summary>
        /// 移除指定的键
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            string redisKey = GetRedisKey(key);
            return Database.KeyDelete(redisKey);
        }

        /// <summary>
        /// 移除指定的键(异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<bool> RemoveAsync(string key)
        {
            string redisKey = GetRedisKey(key);
            return Database.KeyDeleteAsync(redisKey);
        }

        /// <summary>
        /// 移除指定的键
        /// </summary>
        /// <param name="keys">键的集合</param>
        public void RemoveAll(IEnumerable<string> keys)
        {
            keys.ForEach(x => Remove(x));
        }

        /// <summary>
        /// 移除指定的键（异步）
        /// </summary>
        /// <param name="keys">键的集合</param>
        /// <returns></returns>
        public Task RemoveAllAsync(IEnumerable<string> keys)
        {
            return keys.ForEachAsync(RemoveAsync);
        }

        /// <summary>
        /// 根据前缀删除键值
        /// </summary>
        /// <param name="prefix"></param>
        public void RemovePrefix(string prefix)
        {
            Database.KeyDeleteWithPrefix(prefix);
        }
        /// <summary>
        /// 清空数据库
        /// </summary>
        public void Clear()
        {
            Database.KeyDeleteWithPrefix(("*"));
        }
        #endregion

        #endregion

        #region String

        #region 添加（键存在则替换）
        /// <summary>
        /// 添加实例
        /// 如果键值存在则替换旧值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <param name="when"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public bool StringSet(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            string redisKey = GetRedisKey(key);
            return Database.StringSet(redisKey, value, expiry, when, flags);
        }

        /// <summary>
        /// 添加实例
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
        /// <param name="when"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public Task<bool> StringSetAsync(RedisKey key, RedisValue value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None)
        {
            string redisKey = GetRedisKey(key);
            return Database.StringSetAsync(redisKey, value, expiry, when, flags);
        }

        /// <summary>
        /// 添加给定实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool StringAdd<T>(string key, T value,TimeSpan? expiresIn=null) where T:class 
        {
            var entryBytes = Serializer.Serialize(value);
            return StringSet(key, entryBytes, expiresIn);
        }

        /// <summary>
        /// 添加给定实例（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<bool> StringAddAsync<T>(string key, T value, TimeSpan? expiresIn = null) where T : class 
        {
            var entryBytes = await Serializer.SerializeAsync(value);
            return await StringSetAsync(key, entryBytes, expiresIn);
        }

        /// <summary>
        /// 添加给定实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresAt">超时时间</param>
        /// <returns></returns>
        public bool StringAdd<T>(string key, T value, DateTimeOffset expiresAt) where T : class
        {
            var entryBytes = Serializer.Serialize(value);
            var expiration = expiresAt.Subtract(DateTimeOffset.Now);
            return StringSet(key, entryBytes, expiration);
        }

        /// <summary>
        /// 添加给定实例（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresAt"></param>
        /// <returns></returns>
        public async Task<bool> StringAddAsync<T>(string key, T value, DateTimeOffset expiresAt) where T : class
        {
            var entryBytes = await Serializer.SerializeAsync(value);
            var expiration = expiresAt.Subtract(DateTimeOffset.Now);
            return await StringSetAsync(key, entryBytes, expiration);
        }

        /// <summary>
        /// 添加键值对
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public bool StringAddAll<T>(IList<Tuple<string, T>> items) where T : class
        {
            var values = items
                .Select(item => new KeyValuePair<RedisKey, RedisValue>(GetRedisKey(item.Item1), Serializer.Serialize(item.Item2)))
                .ToArray();

            return Database.StringSet(values);
        }

        /// <summary>
        /// 添加键值对（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<bool> StringAddAllAsync<T>(IList<Tuple<string, T>> items) where T : class
        {
            var values = items
                .Select(item => new KeyValuePair<RedisKey, RedisValue>(GetRedisKey(item.Item1), Serializer.Serialize(item.Item2)))
                .ToArray();

            return await Database.StringSetAsync(values);
        }
        #endregion

        #region 查询
        /// <summary>
        /// 获得指定键对应的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="flags"></param>
        /// <returns>
        ///   不存在，返回null
        /// </returns>
        public RedisValue StringGet(string key, CommandFlags flags = CommandFlags.None)
        {
            string redisKey = GetRedisKey(key);
            return Database.StringGet(redisKey, flags);
        }

        /// <summary>
        /// 获得指定键对应的值（异步）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="flags"></param>
        /// <returns>
        ///  不存在，返回Task.Result为null
        /// </returns>
        public Task<RedisValue> StringGetAsync(string key, CommandFlags flags = CommandFlags.None)
        {
            string redisKey = GetRedisKey(key);
            return Database.StringGetAsync(redisKey, flags);
        }

        /// <summary>
        /// 获得指定键对应的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns>
        ///  不存在返回null,存在返回T的实例.
        /// </returns>
        public T StringGet<T>(string key)
        {
            var valueBytes = StringGet(key);

            if (!valueBytes.HasValue)
            {
                return default(T);
            }

            return Serializer.Deserialize<T>(valueBytes);
        }

        /// <summary>
        /// 获得指定键对应的值（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns>
        ///  不存在返回null,存在返回T的实例.
        /// </returns>
        public async Task<T> StringGetAsync<T>(string key)
        {
            var valueBytes = await StringGetAsync(key);

            if (!valueBytes.HasValue)
            {
                return default(T);
            }

            return await Serializer.DeserializeAsync<T>(valueBytes);
        }


        /// <summary>
        /// 获得所有键对应的值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns>
        /// 如果键不存在，则返回null
        /// </returns>
        public IDictionary<string, T> StringGetAll<T>(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(x => GetRedisKey(x)).ToArray();
            var result = Database.StringGet(redisKeys);

            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var index = 0; index < redisKeys.Length; index++)
            {
                var value = result[index];
                dict.Add(redisKeys[index], value == RedisValue.Null ? default(T) : Serializer.Deserialize<T>(value));
            }

            return dict;
        }

        /// <summary>
        /// 获得所有键对应的值（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="keys"></param>
        /// <returns></returns>
        public async Task<IDictionary<string, T>> StringGetAllAsync<T>(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(x => GetRedisKey(x)).ToArray();
            var result = await Database.StringGetAsync(redisKeys);
            var dict = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var index = 0; index < redisKeys.Length; index++)
            {
                var value = result[index];
                dict.Add(redisKeys[index], value == RedisValue.Null ? default(T) : Serializer.Deserialize<T>(value));
            }
            return dict;
        }
        #endregion

        #endregion

        #region Set
        /// <summary>
        /// 向集合中添加元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetAdd<T>(string key, T item) where T : class
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            var redisKey = GetRedisKey(key);
            var serializedObject = Serializer.Serialize(item);
            return Database.SetAdd(redisKey, serializedObject);
        }

        /// <summary>
        /// 检测元素是否在集合中
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetContains<T>(string key, T item) where T : class
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            var redisKey = GetRedisKey(key);
            var serializedObject = Serializer.Serialize(item);
            return Database.SetContains(redisKey, serializedObject);
        }

        /// <summary>
        ///  添加指定元素
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool SetAdd(string key, string item)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            var redisKey = GetRedisKey(key);
            return Database.SetAdd(redisKey, item);
        }

        /// <summary>
        /// 添加指定元素（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<bool> SetAddAsync<T>(string key, T item) where T : class
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            var redisKey = GetRedisKey(key);
            var serializedObject = await Serializer.SerializeAsync(item);
            return await Database.SetAddAsync(redisKey, serializedObject);
        }

        /// <summary>
        /// 获得键对应的所有值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string[] SetMembers(string key)
        {
            var redisKey = GetRedisKey(key);
            return Database.SetMembers(redisKey).Select(x => x.ToString()).ToArray();
        }

        /// <summary>
        /// 获得键对应的所有值（异步）
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string[]> SetMembersAsync(string key)
        {
            var redisKey = GetRedisKey(key);
            return (await Database.SetMembersAsync(redisKey)).Select(x => x.ToString()).ToArray();
        }

        /// <summary>
        /// 获得键对应的所有值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public IEnumerable<T> SetMembers<T>(string key)
        {
            var redisKey = GetRedisKey(key);
            var members = Database.SetMembers(redisKey);

            return members.Select(m => m == RedisValue.Null ? default(T) : Serializer.Deserialize<T>(m));
        }

        /// <summary>
        /// 获得键对应的所有值（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> SetMembersAsync<T>(string key)
        {
            var redisKey = GetRedisKey(key);
            var members = await Database.SetMembersAsync(redisKey);

            return members.Select(m => m == RedisValue.Null ? default(T) : Serializer.Deserialize<T>(m));
        }
        #endregion

        #region List
        /// <summary>
        /// 在List头插入指定值。
        /// 如果键对应的队列不存在，则创建。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns>
        ///  插入操作执行成功后的列表长度
        /// </returns>
        public long ListLeftPush<T>(string key, T item) where T : class
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            string redisKey = GetRedisKey(key);
            var serializedItem = Serializer.Serialize(item);

            return Database.ListLeftPush(redisKey, serializedItem);
        }
        /// <summary>
        /// 在List头插入指定值。
        /// 如果键对应的队列不存在，则创建。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public long ListLeftPush(string key, string item)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            string redisKey = GetRedisKey(key);

            return Database.ListLeftPush(redisKey, item);
        }
        /// <summary>
        /// 在List头插入指定值（异步）。
        /// 如果键对应的队列不存在，则创建。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public async Task<long> ListLeftPushAsync<T>(string key, T item) where T : class
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            var redisKey = GetRedisKey(key);
            var serializedItem = await Serializer.SerializeAsync(item);

            return await Database.ListLeftPushAsync(redisKey, serializedItem);
        }

        /// <summary>
        ///  返回并删除List最后一个元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">.</param>
        /// <returns></returns>
        public T ListRightPop<T>(string key) where T : class
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key cannot be empty.", "" + key + "");
            }

            var redisKey = GetRedisKey(key);
            var item = Database.ListRightPop(redisKey);

            return item == RedisValue.Null ? null : Serializer.Deserialize<T>(item);
        }
        /// <summary>
        /// 返回并删除List最后一个元素
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ListRightPop(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key cannot be empty.", "" + key + "");
            }

            var redisKey = GetRedisKey(key);
            var item = Database.ListRightPop(redisKey);

            return item == RedisValue.Null ? null : item.ToString();
        }

        /// <summary>
        /// 返回并删除List最后一个元素（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<T> ListRightPopAsync<T>(string key) where T : class
        {
            
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");

            string redisKey = GetRedisKey(key);
            var item = await Database.ListRightPopAsync(redisKey);
            if (item == RedisValue.Null) return null;
            return item == RedisValue.Null ? null : await Serializer.DeserializeAsync<T>(item);
        }

        /// <summary>
        /// 返回List的长度
        /// </summary>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 如果键不存在，被认为是空列表并返回0
        /// </returns>
        public long ListLength(string key, CommandFlags commandFlags = CommandFlags.None)
        {
            string redisKey = GetRedisKey(key);
            return Database.ListLength(redisKey);
        }

        /// <summary>
        /// 返回List的长度（异步）
        /// </summary>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 如果键不存在，被认为是空列表并返回0
        /// </returns>
        public async Task<long> ListLengthAsync(string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(key);
            return await Database.ListLengthAsync(redisKey, commandFlags);
        }
        /// <summary>
        /// 获得list中的所有元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="redisKey"></param>
        /// <returns></returns>
        public List<T> ListRange<T>(string redisKey)
        {
            List<T> list = new List<T>();
            var redisValues = Database.ListRange(redisKey);
            redisValues.ForEach(v=>
            {
                list.Add(Serializer.Deserialize<T>(v));
            });
            return list;
        }
        /// <summary>
        /// 移除list中的指定元素
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public Task<long> ListRemove<T>(string key,T item)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key cannot be empty.", "" + key + "");
            if (item == null) throw new ArgumentNullException("" + item + "", "item cannot be null.");

            var redisKey = GetRedisKey(key);
            var serializedItem = Serializer.Serialize(item);

            return Database.ListRemoveAsync(redisKey, serializedItem);
        }
        #endregion

        #region Hash

        #region Hash Delete
        /// <summary>
        /// 移除键对应的值
        /// </summary>
        /// <remarks>
        ///    时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey">hash的键</param>
        /// <param name="key">实体键</param>
        /// <param name="commandFlags"></param>
        /// <returns>
        ///     如果键不存在返回false
        /// </returns>
        public bool HashDelete(string hashKey, string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashDelete(redisKey, key, commandFlags);
        }

        /// <summary>
        /// 移除所有键对应的值，忽略不存在的键
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <param name="hashKey">hash的键</param>
        /// <param name="keys">实体键集合</param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 返回被删除的实体数量
        /// </returns>
        public long HashDelete(string hashKey, IEnumerable<string> keys, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashDelete(redisKey, keys.Select(x => (RedisValue)x).ToArray(), commandFlags);
        }

        /// <summary>
        /// 移除键对应的值（异步）
        /// </summary>
        /// <param name="hashKey">hash的键</param>
        /// <param name="key">实体键</param>
        /// <param name="commandFlags"></param>
        /// <returns>
        ///     如果键不存在返回false
        /// </returns>
        public async Task<bool> HashDeleteAsync(string hashKey, string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashDeleteAsync(redisKey, key, commandFlags);
        }

        /// <summary>
        /// 移除所有键对应的值，忽略不存在的键（异步）
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <param name="hashKey">hash的键</param>
        /// <param name="keys">实体键集合</param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 返回被删除的实体数量
        /// </returns>
        public async Task<long> HashDeleteAsync(string hashKey, IEnumerable<string> keys, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashDeleteAsync(redisKey, keys.Select(x => (RedisValue)x).ToArray(), commandFlags);
        }
        #endregion

        #region Hash Exists
        /// <summary>
        /// 判断实体是否存在
        /// </summary>
        /// <remarks>
        ///   时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <returns></returns>
        public bool HashExists(string hashKey, string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashExists(redisKey, key, commandFlags);
        }

        /// <summary>
        /// 判断实体是否存在（异步）
        /// </summary>
        /// <remarks>
        ///   时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <returns></returns>
        public async Task<bool> HashExistsAsync(string hashKey, string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashExistsAsync(redisKey, key, commandFlags);
        }
        #endregion

        #region Hash Get
        /// <summary>
        ///  获得实体
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 键不存在返回null
        /// </returns>
        public T HashGet<T>(string hashKey, string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            var redisValue = Database.HashGet(redisKey, key, commandFlags);
            return redisValue.HasValue ? Serializer.Deserialize<T>(redisValue) : default(T);
        }

        /// <summary>
        /// 获得实体
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="keys"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 键值对词典，键不存在返null
        /// </returns>
        public Dictionary<string, T> HashGet<T>(string hashKey, IEnumerable<string> keys, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return keys.Select(x => new { key = x, value = HashGet<T>(redisKey, x, commandFlags) })
                        .ToDictionary(kv => kv.key, kv => kv.value, StringComparer.Ordinal);
        }

        /// <summary>
        /// 获得实体
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 返回键值对词典，键不存在返null
        /// </returns>
        public Dictionary<string, T> HashGetAll<T>(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database
                        .HashGetAll(redisKey, commandFlags)
                        .ToDictionary(
                            x => x.Name.ToString(),
                            x => Serializer.Deserialize<T>(x.Value),
                            StringComparer.Ordinal);
        }

        /// <summary>
        ///  获得实体
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 键不存在返回null
        /// </returns>
        public async Task<T> HashGetAsync<T>(string hashKey, string key, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            var redisValue = await Database.HashGetAsync(redisKey, key, commandFlags);
            return redisValue.HasValue ? Serializer.Deserialize<T>(redisValue) : default(T);
        }

        /// <summary>
        ///  获得实体（异步）
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="keys"></param>
        /// <param name="commandFlags"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, T>> HashGetAsync<T>(string hashKey, IEnumerable<string> keys, CommandFlags commandFlags = CommandFlags.None)
        {
            var result = new Dictionary<string, T>();
            var redisKey = GetRedisKey(hashKey);
            foreach (var key in keys)
            {
                var value = await HashGetAsync<T>(redisKey, key, commandFlags);
                result.Add(key, value);
            }
            return result;
        }

        /// <summary>
        /// 获得实体（异步）
        /// </summary>
        /// <remarks>
        ///      时间复杂度: O(N)
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, T>> HashGetAllAsync<T>(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return (await Database
                        .HashGetAllAsync(redisKey, commandFlags))
                        .ToDictionary(
                            x => x.Name.ToString(),
                            x => Serializer.Deserialize<T>(x.Value),
                            StringComparer.Ordinal);
        }
        #endregion

        #region Hash Increments
        /// <summary>
        /// 自增指定值
        /// Hash键不存在则创建之
        /// 如果实体key不存在则创建之，对应的值就是指定的自增值
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <param name="value"></param>
        /// <returns>
        /// 自增后的值
        /// </returns>
        public long HashIncrement(string hashKey, string key, long value, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashIncrement(redisKey, key, value, commandFlags);
        }

        /// <summary>
        /// 自增指定值
        /// Hash键不存在则创建之
        /// 如果实体key不存在则创建之，对应的值就是指定的自增值
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <param name="value"></param>
        /// <returns>
        /// 自增后的值
        /// </returns>
        public double HashIncrement(string hashKey, string key, double value, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashIncrement(redisKey, key, value, commandFlags);
        }

        /// <summary>
        /// 异步方法
        /// 自增指定值
        /// Hash键不存在则创建之
        /// 如果实体key不存在则创建之，对应的值就是指定的自增值
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <param name="value"></param>
        /// <returns>
        /// 自增后的值
        /// </returns>
        public async Task<long> HashIncerementByAsync(string hashKey, string key, long value, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashIncrementAsync(redisKey, key, value, commandFlags);
        }

        /// <summary>
        /// 异步方法
        /// 自增指定值
        /// Hash键不存在则创建之
        /// 如果实体key不存在则创建之，对应的值就是指定的自增值
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="commandFlags"></param>
        /// <param name="value"></param>
        /// <returns>
        /// 自增后的值
        /// </returns>
        public async Task<double> HashIncrementAsync(string hashKey, string key, double value, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashIncrementAsync(redisKey, key, value, commandFlags);
        }
        #endregion

        #region Hash Other
        /// <summary>
        /// 获得Hash键对应的所有实体键值
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 实体键值集合，如果Hash键不存在返回空集合。
        /// </returns>
        public IEnumerable<string> HashKeys(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashKeys(redisKey, commandFlags).Select(x => x.ToString());
        }

        /// <summary>
        ///  获得实体的数据量
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// Hash键不存在时返回0
        /// </returns>
        public long HashLength(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashLength(redisKey, commandFlags);
        }

        /// <summary>
        ///  获得所有的值
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N) 
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// Hash键不存在时返回空集合
        /// </returns>
        public IEnumerable<T> HashValues<T>(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashValues(redisKey, commandFlags).Select(x => Serializer.Deserialize<T>(x));
        }

        /// <summary>
        /// 获得所有值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="pattern">GLOB检索模式</param>
        /// <param name="pageSize"></param>
        /// <param name="commandFlags"></param>
        /// <returns></returns>
        public Dictionary<string, T> HashScan<T>(string hashKey, string pattern, int pageSize = 10, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashScan(redisKey, pattern, pageSize, commandFlags)
                        .ToDictionary(x => x.Name.ToString(),
                                      x => Serializer.Deserialize<T>(x.Value),
                                      StringComparer.Ordinal);
        }

        /// <summary>
        /// 获得Hash键对应的所有实体键值（异步的）
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 实体键值集合，如果Hash键不存在返回空集合。
        /// </returns>
        public async Task<IEnumerable<string>> HashKeysAsync(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return (await Database.HashKeysAsync(redisKey, commandFlags)).Select(x => x.ToString());
        }

        /// <summary>
        ///  获得实体的数据量（异步的）
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(1)
        /// </remarks>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// Hash键不存在时返回0
        /// </returns>
        public async Task<long> HashLengthAsync(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashLengthAsync(redisKey, commandFlags);
        }

        /// <summary>
        ///  获得所有的值（异步的）
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N) 
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// Hash键不存在时返回空集合
        /// </returns>
        public async Task<IEnumerable<T>> HashValuesAsync<T>(string hashKey, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return (await Database.HashValuesAsync(redisKey, commandFlags)).Select(x => Serializer.Deserialize<T>(x));
        }

        /// <summary>
        /// 获得所有值（异步的）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="pattern">GLOB检索模式</param>
        /// <param name="pageSize"></param>
        /// <param name="commandFlags"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, T>> HashScanAsync<T>(string hashKey, string pattern, int pageSize = 10, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return (await Task.Run(() => Database.HashScan(redisKey, pattern, pageSize, commandFlags)))
                .ToDictionary(x => x.Name.ToString(), x => Serializer.Deserialize<T>(x.Value), StringComparer.Ordinal);
        }
        #endregion

        #region Hash Set
        /// <summary>
        /// 设置值
        /// 键存在则重写，不存在则创建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="nx">
        /// true:键不存在时执行操作
        /// false:不论键是否存在都执行操作
        /// </param>
        /// <param name="value"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 键不存在返回true
        /// 键存在返回false
        /// </returns>
        public bool HashSet<T>(string hashKey, string key, T value, bool nx = false, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return Database.HashSet(redisKey, key, Serializer.Serialize(value), nx ? When.NotExists : When.Always, commandFlags);
        }

        /// <summary>
        /// 设置指定字段对应的值
        /// 覆盖任何已存在键对应的值，没有则创建
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N) 
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="values"></param>
        /// <param name="commandFlags"></param>
        public void HashSet<T>(string hashKey, Dictionary<string, T> values, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            var entries = values.Select(kv => new HashEntry(kv.Key, Serializer.Serialize(kv.Value)));
            Database.HashSet(redisKey, entries.ToArray(), commandFlags);
        }
        /// <summary>
        /// 异步方法
        /// 设置值
        /// 键存在则重写，不存在则创建
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="key"></param>
        /// <param name="nx">
        /// true:键不存在时执行操作
        /// false:不论键是否存在都执行操作
        /// </param>
        /// <param name="value"></param>
        /// <param name="commandFlags"></param>
        /// <returns>
        /// 键不存在返回true
        /// 键存在返回false
        /// </returns>
        public async Task<bool> HashSetAsync<T>(string hashKey, string key, T value, bool nx = false, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            return await Database.HashSetAsync(redisKey, key, Serializer.Serialize(value), nx ? When.NotExists : When.Always, commandFlags);
        }

        /// <summary>
        /// 异步方法
        /// 设置指定字段对应的值
        /// 覆盖任何已存在键对应的值，没有则创建
        /// </summary>
        /// <remarks>
        ///     时间复杂度: O(N) 
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="hashKey"></param>
        /// <param name="values"></param>
        /// <param name="commandFlags"></param>
        public async Task HashSetAsync<T>(string hashKey, IDictionary<string, T> values, CommandFlags commandFlags = CommandFlags.None)
        {
            var redisKey = GetRedisKey(hashKey);
            var entries = values.Select(kv => new HashEntry(kv.Key, Serializer.Serialize(kv.Value)));
            await Database.HashSetAsync(redisKey, entries.ToArray(), commandFlags);
        }
        #endregion
        
        #endregion

        #region Subscriber
        /// <summary>
        /// 发布一条信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public long Publish<T>(RedisChannel channel, T message, CommandFlags flags = CommandFlags.None)
        {
            var sub = m_connectionMultiplexer.GetSubscriber();
            return sub.Publish(channel, Serializer.Serialize(message), flags);
        }

        /// <summary>
        ///  发布一条信息（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public async Task<long> PublishAsync<T>(RedisChannel channel, T message, CommandFlags flags = CommandFlags.None)
        {
            var sub = m_connectionMultiplexer.GetSubscriber();
            return await sub.PublishAsync(channel, await Serializer.SerializeAsync(message), flags);
        }

        /// <summary>
        ///  注册一个回调方法来处理发布的信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        /// <param name="flags"></param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void Subscribe<T>(RedisChannel channel, Action<T> handler, CommandFlags flags = CommandFlags.None)
        {
            if (handler == null) throw new ArgumentNullException("" + handler + "");

            var sub = m_connectionMultiplexer.GetSubscriber();
            sub.Subscribe(channel, (redisChannel, value) => handler(Serializer.Deserialize<T>(value)), flags);
        }

        /// <summary>
        ///   注册一个回调方法来处理发布的信息（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task SubscribeAsync<T>(RedisChannel channel, Func<T, Task> handler, CommandFlags flags = CommandFlags.None)
        {
            if (handler == null) throw new ArgumentNullException("" + handler + "");

            var sub = m_connectionMultiplexer.GetSubscriber();
            await sub.SubscribeAsync(channel, async (redisChannel, value) => await handler(Serializer.Deserialize<T>(value)), flags);
        }

        /// <summary>
        ///   注销回调方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        /// <param name="flags"></param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void Unsubscribe<T>(RedisChannel channel, Action<T> handler, CommandFlags flags = CommandFlags.None)
        {
            if (handler == null) throw new ArgumentNullException("" + handler + "");

            var sub = m_connectionMultiplexer.GetSubscriber();
            sub.Unsubscribe(channel, (redisChannel, value) => handler(Serializer.Deserialize<T>(value)), flags);
        }

        /// <summary>
        ///  注销回调方法（异步）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="handler"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task UnsubscribeAsync<T>(RedisChannel channel, Func<T, Task> handler, CommandFlags flags = CommandFlags.None)
        {
            if (handler == null) throw new ArgumentNullException("" + handler + "");

            var sub = m_connectionMultiplexer.GetSubscriber();
            await sub.UnsubscribeAsync(channel, (redisChannel, value) => handler(Serializer.Deserialize<T>(value)), flags);
        }

        /// <summary>
        ///  注销管道中的所有回调方法
        /// </summary>
        /// <param name="flags"></param>
        public void UnsubscribeAll(CommandFlags flags = CommandFlags.None)
        {
            var sub = m_connectionMultiplexer.GetSubscriber();
            sub.UnsubscribeAll(flags);
        }

        /// <summary>
        ///  注销管道中的所有回调方法（异步）
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public async Task UnsubscribeAllAsync(CommandFlags flags = CommandFlags.None)
        {
            var sub = m_connectionMultiplexer.GetSubscriber();
            await sub.UnsubscribeAllAsync(flags);
        }
        #endregion

        #region Redis System
        /// <summary>
        ///  清理数据库
        /// </summary>
        public void FlushDb()
        {
            var endPoints = Database.Multiplexer.GetEndPoints();

            foreach (var endpoint in endPoints)
            {
                Database.Multiplexer.GetServer(endpoint).FlushDatabase(Database.Database);
            }
        }

        /// <summary>
        /// 清理数据库（异步）
        /// </summary>
        /// <returns></returns>
        public async Task FlushDbAsync()
        {
            var endPoints = Database.Multiplexer.GetEndPoints();

            foreach (var endpoint in endPoints)
            {
                await Database.Multiplexer.GetServer(endpoint).FlushDatabaseAsync(Database.Database);
            }
        }

        /// <summary>
        /// 保存数据库
        /// </summary>
        /// <param name="saveType"></param>
        public void Save(SaveType saveType)
        {
            var endPoints = Database.Multiplexer.GetEndPoints();

            foreach (var endpoint in endPoints)
            {
                Database.Multiplexer.GetServer(endpoint).Save(saveType);
            }
        }

        /// <summary>
        /// 保存数据库（异步）
        /// </summary>
        /// <param name="saveType"></param>
        public async void SaveAsync(SaveType saveType)
        {
            var endPoints = Database.Multiplexer.GetEndPoints();

            foreach (var endpoint in endPoints)
            {
                await Database.Multiplexer.GetServer(endpoint).SaveAsync(saveType);
            }
        }

        /// <summary>
        ///  获得服务器信息
        ///  http://redis.io/commands/INFO
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetInfo()
        {
            var info = Database.ScriptEvaluate("return redis.call('INFO')").ToString();

            return ParseInfo(info);
        }

        /// <summary>
        ///  获得服务器信息（异步）
        ///  http://redis.io/commands/INFO
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> GetInfoAsync()
        {
            var info = (await Database.ScriptEvaluateAsync("return redis.call('INFO')")).ToString();

            return ParseInfo(info);
        }

        private Dictionary<string, string> ParseInfo(string info)
        {
            var lines = info.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var data = new Dictionary<string, string>();
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line) || line[0] == '#')
                {
                    continue;
                }

                var idx = line.IndexOf(':');
                if (idx > 0)
                {
                    var key = line.Substring(0, idx);
                    var infoValue = line.Substring(idx + 1).Trim();

                    data.Add(key, infoValue);
                }
            }

            return data;
        }
        #endregion
    }
}
