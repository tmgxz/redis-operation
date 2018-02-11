using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_Redis.Redis.Extensions
{
    public static class RedisDatabaseExtensions
    {
        /// <summary>
        /// 根据前缀删除
        /// </summary>
        /// <param name="database"></param>
        /// <param name="prefix"></param>
        public static void KeyDeleteWithPrefix(this IDatabase database, string prefix)
        {
            if (database == null)
            {
                throw new ArgumentException("参数不能为null", "database");
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("参数不能为空", "prefix");
            }

            database.ScriptEvaluate(@"
                local keys = redis.call('keys', ARGV[1]) 
                for i=1,#keys,5000 do 
                redis.call('del', unpack(keys, i, math.min(i+4999, #keys)))
                end", values: new RedisValue[] { prefix });
        }

        /// <summary>
        /// 根据前缀获取Key的数量
        /// </summary>
        /// <param name="database"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static int KeyCount(this IDatabase database, string prefix)
        {
            if (database == null)
            {
                throw new ArgumentException("参数不能为null", "database");
            }

            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException("参数不能为空", "prefix");
            }

            var retVal = database.ScriptEvaluate("return table.getn(redis.call('keys', ARGV[1]))", values: new RedisValue[] { prefix });

            if (retVal.IsNull)
            {
                return 0;
            }

            return (int)retVal;
        }
    }
}
