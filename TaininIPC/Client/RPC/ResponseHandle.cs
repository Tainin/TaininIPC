using TaininIPC.Data.Serialized;

namespace TaininIPC.Client.RPC;

public sealed class ResponseHandle {
    private readonly SemaphoreSlim whenSemaphore;
    private MultiFrame response = null!;

    public ResponseHandle() => whenSemaphore = new(0, 1);

    public void Release(MultiFrame frame) {
        response = frame;
        whenSemaphore.Release();
    }

    public async Task<MultiFrame> WhenResponse() {
        await whenSemaphore.WaitAsync().ConfigureAwait(false);
        return response;
    }
}
