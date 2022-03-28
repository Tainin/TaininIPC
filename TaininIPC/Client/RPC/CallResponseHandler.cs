using System.Buffers.Binary;
using System.Diagnostics;
using TaininIPC.Client.Endpoints;
using TaininIPC.Client.Interface;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;

namespace TaininIPC.Client.RPC;

/// <summary>
/// Represnets a channel for RPC style calls over the network.
/// </summary>
public sealed class CallResponseHandler : IRouter {
    private readonly CritBitTree<BasicKey, ResponseHandle> responseHandlers;

    private readonly Queue<int> indexRecycling;
    private readonly Queue<ResponseHandle> handleRecycling;

    private readonly SemaphoreSlim syncSemaphore;

    private int nextFreshIndex;

    /// <summary>
    /// Initializes a new <see cref="CallResponseHandler"/>
    /// </summary>
    public CallResponseHandler() {
        (responseHandlers, indexRecycling, handleRecycling) = (new(), new(), new());
        syncSemaphore = new(1, 1);
        nextFreshIndex = 0;
    }

    /// <summary>
    /// If the specified <paramref name="frame"/> contains a routing key and the key maps to a <see cref="ResponseHandle"/>
    /// held by the handler, releases the <see cref="ResponseHandle"/> with the <paramref name="frame"/>
    /// </summary>
    /// <param name="frame">The frame use as a response.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        try {
            if (!ProtocolHelper.TryGetResponseKey(frame, out BasicKey? responseKey)) return;
            if (!responseHandlers.TryGet(responseKey, out ResponseHandle? responseHandle)) return;

            responseHandle!.Release(frame);
        } finally {
            syncSemaphore.Release();
        }
    }

    /// <summary>
    /// Sends the specified <paramref name="frame"/> through the specified <paramref name="endpointTableEntry"/> and 
    /// asyncronously waits for a repsonse.
    /// </summary>
    /// <param name="endpointTableEntry">The <see cref="EndpointTableEntry"/> to send the <paramref name="frame"/> through.</param>
    /// <param name="frame">The frame to send as the call data.</param>
    /// <returns>An asyncronous task which completes with a <see cref="MultiFrame"/> represening the results of the call.</returns>
    public async Task<MultiFrame> Call(EndpointTableEntry endpointTableEntry, MultiFrame frame) {
        (ResponseHandle handle, int index, BasicKey responseKey) = await SetupResponseHandler().ConfigureAwait(false);

        ProtocolHelper.SetResponseKey(frame, responseKey);
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