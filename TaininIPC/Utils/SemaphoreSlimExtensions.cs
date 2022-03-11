namespace TaininIPC.Utils;

public static class SemaphoreSlimExtensions {
    public static async Task<TResult> AquireAndRun<T, TResult>(this SemaphoreSlim semaphore, Func<T, Task<TResult>> function, T t) {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try {
            return await function(t).ConfigureAwait(false);
        } finally {
            semaphore.Release();
        }
    }

    public static async Task AquireAndRun<T1, T2, T3>(this SemaphoreSlim semaphore, Func<T1, T2, T3, Task> function, T1 t1, T2 t2, T3 t3) {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try {
            await function(t1, t2, t3).ConfigureAwait(false);
        } finally {
            semaphore.Release();
        }
    }

    public static async Task AquireAndRun<T1, T2>(this SemaphoreSlim semaphore, Func<T1, T2, Task> function, T1 t1, T2 t2) {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try {
            await function(t1, t2).ConfigureAwait(false);
        } finally {
            semaphore.Release();
        }
    }

    public static async Task AquireAndRun<T>(this SemaphoreSlim semaphore, Func<T, Task> function, T t) {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try {
            await function(t).ConfigureAwait(false);
        } finally {
            semaphore.Release();
        }
    }

    public static async Task AquireAndRun(this SemaphoreSlim semaphore, Func<Task> function) {
        await semaphore.WaitAsync().ConfigureAwait(false);
        try {
            await function().ConfigureAwait(false);
        } finally {
            semaphore.Release();
        }
    }
}
