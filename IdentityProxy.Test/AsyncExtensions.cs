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
