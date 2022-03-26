using TaininIPC.Data.Serialized;

namespace TaininIPC.Client.RPC;

/// <summary>
/// Represents an RPC call waiting for it's response.
/// </summary>
public sealed class ResponseHandle {
    private readonly SemaphoreSlim whenSemaphore;
    private MultiFrame response = null!;

    /// <summary>
    /// Initializes a new <see cref="ResponseHandle"/>
    /// </summary>
    public ResponseHandle() => whenSemaphore = new(0, 1);

    /// <summary>
    /// Completes the RPC call with the given <paramref name="frame"/> as the result.
    /// </summary>
    /// <param name="frame">The result of the RPC call.</param>
    public void Release(MultiFrame frame) {
        response = frame;
        whenSemaphore.Release();
    }

    /// <summary>
    /// Asyncronously waits for the RPC call to complete.
    /// </summary>
    /// <returns>An asyncronous task which completes with the result of the RPC call.</returns>
    public async Task<MultiFrame> WhenResponse() {
        await whenSemaphore.WaitAsync().ConfigureAwait(false);
        return response;
    }
}
