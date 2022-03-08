using System.Buffers.Binary;
using System.Diagnostics;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.Data.Serialized;
using TaininIPC.Utils;

namespace TaininIPC.Client.RPC;

public sealed class CallResponseHandler : IRouter {
    private sealed class ResponseHandle {
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

    private readonly CritBitTree<ResponseHandle> responseHandlers;
    private readonly Queue<int> indexRecycling;
    private readonly Queue<ResponseHandle> handleRecycling;
    private readonly SemaphoreSlim syncSemaphore;

    private int nextFreshIndex;

    public CallResponseHandler() {
        (responseHandlers, indexRecycling, handleRecycling) = (new(), new(), new());
        syncSemaphore = new(1, 1);
        nextFreshIndex = 0;
    }

    public Task RouteFrame(MultiFrame frame, EndpointTableEntry _) {
        if (responseHandlers.TryGet(Protocol.GetResponseKey(frame).Span, out ResponseHandle? handle))
            if (handle is not null) handle.Release(frame);
        return Task.CompletedTask;
    }
    public async Task<MultiFrame> Call(EndpointTableEntry endpointTableEntry, MultiFrame frame) {
        (ResponseHandle handle, int index, ReadOnlyMemory<byte> responseKey) = 
            await SetupResponseHandler().ConfigureAwait(false);
        
        Protocol.SetResponseKey(frame, responseKey);
        await endpointTableEntry.FrameEndpoint.SendMultiFrame(frame).ConfigureAwait(false);
        MultiFrame response = await handle.WhenResponse().ConfigureAwait(false);

        await TearDownResponseHandler(handle, index, responseKey).ConfigureAwait(false);

        return response;
    }

    private async Task<(ResponseHandle handle, int index, ReadOnlyMemory<byte> responseKey)> SetupResponseHandler() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        if (!handleRecycling.TryDequeue(out ResponseHandle? handle) || handle is null) handle = new();
        if (!indexRecycling.TryDequeue(out int index)) index = Interlocked.Increment(ref nextFreshIndex);

        byte[] responseKey = new byte[2 * sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(responseKey.AsSpan(0 * sizeof(int)), index);
        BinaryPrimitives.WriteInt32BigEndian(responseKey.AsSpan(1 * sizeof(int)), Environment.TickCount);

        bool added = responseHandlers.TryAdd(responseKey, handle);

        syncSemaphore.Release();

        Debug.Assert(added, $"{nameof(CallResponseHandler)}.{nameof(Call)} should never fail " +
            $"to add {nameof(handle)} to {nameof(responseHandlers)}");
        
        return (handle, index, responseKey);
    }
    private async Task TearDownResponseHandler(ResponseHandle handle, int index, ReadOnlyMemory<byte> responseKey) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);

        bool removed = responseHandlers.TryRemove(responseKey.Span);

        handleRecycling.Enqueue(handle);
        indexRecycling.Enqueue(index);

        syncSemaphore.Release();

        Debug.Assert(removed, $"{nameof(CallResponseHandler)}.{nameof(Call)} should never fail " +
            $"to remove {nameof(handle)} from {nameof(responseHandlers)}");
    }
}
