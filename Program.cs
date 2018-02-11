using ConsoleApp_Redis.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_Redis
{
    class Program
    {
        static void Main(string[] args)
        {
            RedisService rs = new RedisService("192.168.20.104:6379,,connectTimeout=15000,syncTimeout=15000");
            rs = new RedisService("192.168.20.104:6379,,connectTimeout=15000,syncTimeout=15000", key => string.Format("Cache:{0}", key));
            var v = rs.GetInfo();
        }
    }
}
