using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_Redis.Redis.Extensions
{

    /// <summary>
    /// 扩展Linq
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// 对集合中的每一个元素执行Action委托方法
        /// </summary>
        /// <typeparam name = "T"></typeparam>
        /// <param name = "source"></param>
        /// <param name = "action"></param>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }

        /// <summary>
        /// 对集合中的每一个元素执行Action委托方法(异步)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="body"></param>
        /// <returns></returns>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body)
        {
            return Task.WhenAll(
                from item in source
                select Task.Run(() => body(item)));
        }
    }
}
