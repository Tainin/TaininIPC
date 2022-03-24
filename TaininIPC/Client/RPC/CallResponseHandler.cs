using System.Buffers.Binary;
using System.Diagnostics;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Client.RPC;


public sealed class CallResponseHandler : IRouter {
    private readonly CritBitTree<BasicKey, ResponseHandle> responseHandlers;

    private readonly Queue<int> indexRecycling;
    private readonly Queue<ResponseHandle> handleRecycling;

    private readonly SemaphoreSlim syncSemaphore;

    private int nextFreshIndex;

    public CallResponseHandler() {
        (responseHandlers, indexRecycling, handleRecycling) = (new(), new(), new());
        syncSemaphore = new(1, 1);
        nextFreshIndex = 0;
    }

    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (!Protocol.TryGetResponseKey(frame, out BasicKey? responseKey)) return;
            if (!responseHandlers.TryGet(responseKey, out ResponseHandle? responseHandle)) return;
            responseHandle!.Release(frame);
        } finally {
            syncSemaphore.Release();
        }
    }
    public async Task<MultiFrame> Call(EndpointTableEntry endpointTableEntry, MultiFrame frame) {
        (ResponseHandle handle, int index, BasicKey responseKey) = await SetupResponseHandler().ConfigureAwait(false);
        
        Protocol.SetResponseKey(frame, responseKey);
        await endpointTableEntry.FrameEndpoint.SendMultiFrame(frame).ConfigureAwait(false);
        MultiFrame response = await handle.WhenResponse().ConfigureAwait(false);

        await TearDownResponseHandler(handle, index, responseKey).ConfigureAwait(false);

        return response;
    }

    private async Task<(ResponseHandle handle, int index, BasicKey responseKey)> SetupResponseHandler() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        if (!handleRecycling.TryDequeue(out ResponseHandle? handle) || handle is null) handle = new();
        if (!indexRecycling.TryDequeue(out int index)) index = Interlocked.Increment(ref nextFreshIndex);

        byte[] keyBuffer = new byte[2 * sizeof(byte)];
        BinaryPrimitives.WriteInt32BigEndian(keyBuffer.AsSpan(0 * sizeof(int)), index);
        BinaryPrimitives.WriteInt32BigEndian(keyBuffer.AsSpan(1 * sizeof(int)), Environment.TickCount);
        BasicKey responseKey = new(keyBuffer);

        bool added = responseHandlers.TryAdd(responseKey, handle);

        syncSemaphore.Release();

        Debug.Assert(added, $"{nameof(CallResponseHandler)}.{nameof(Call)} should never fail " +
            $"to add {nameof(handle)} to {nameof(responseHandlers)}");
        
        return (handle, index, responseKey);
    }
    private async Task TearDownResponseHandler(ResponseHandle handle, int index, BasicKey responseKey) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);

        bool removed = responseHandlers.TryRemove(responseKey);

        handleRecycling.Enqueue(handle);
        indexRecycling.Enqueue(index);

        syncSemaphore.Release();

        Debug.Assert(removed, $"{nameof(CallResponseHandler)}.{nameof(Call)} should never fail " +
            $"to remove {nameof(handle)} from {nameof(responseHandlers)}");
    }
}