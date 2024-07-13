using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityProxy.Test;

public static class AsyncExtensions
{
    public static async IAsyncEnumerable<T> ExecuteAsync<T>(this IEnumerable<Task<T>> tasks)
    {
        foreach (var task in tasks)
        {
            yield return await task;
        }
    }
}