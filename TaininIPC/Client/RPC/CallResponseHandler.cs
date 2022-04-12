using System.Buffers.Binary;
using System.Diagnostics;
using TaininIPC.Client.Routing.Endpoints;
using TaininIPC.Client.Routing.Interface;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Protocol;

namespace TaininIPC.Client.RPC;

/// <summary>
/// Represnets a channel for RPC style calls over the network.
/// </summary>
public sealed class CallResponseHandler : IRouter {

    private sealed class CallInfo {
        public ResponseHandle ResponseHandle { get; }
        public int Index { get; }
        public BasicKey ResponseIdentifier { get; }

        public CallInfo(ResponseHandle responseHandle, int index, BasicKey responseIdentifier) =>
            (ResponseHandle, Index, ResponseIdentifier) = (responseHandle, index, responseIdentifier);
    }

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
            if (!frame.TryGetResponseIdentifier(out BasicKey? responseIdentifier)) return;
            if (!responseHandlers.TryGet(responseIdentifier, out ResponseHandle? responseHandle)) return;

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
        CallInfo callInfo = await SetupCallHandler().ConfigureAwait(false);

        frame.SetResponseIdentifier(callInfo.ResponseIdentifier);
        await endpointTableEntry.NetworkEndpoint.SendMultiFrame(frame).ConfigureAwait(false);
        MultiFrame response = await callInfo.ResponseHandle.WhenResponse().ConfigureAwait(false);

        await TeardownCallHandler(callInfo).ConfigureAwait(false);

        return response;
    }

    private async Task<CallInfo> SetupCallHandler() {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        if (!handleRecycling.TryDequeue(out ResponseHandle? handle) || handle is null) handle = new();
        if (!indexRecycling.TryDequeue(out int index)) index = Interlocked.Increment(ref nextFreshIndex);

        byte[] keyBuffer = new byte[2 * sizeof(byte)];
        BinaryPrimitives.WriteInt32BigEndian(keyBuffer.AsSpan(0 * sizeof(int)), index);
        BinaryPrimitives.WriteInt32BigEndian(keyBuffer.AsSpan(1 * sizeof(int)), Environment.TickCount);
        BasicKey responseIdentifier = new(keyBuffer);

        bool added = responseHandlers.TryAdd(responseIdentifier, handle);

        syncSemaphore.Release();

        Debug.Assert(added, $"{nameof(CallResponseHandler)}.{nameof(Call)} should never fail " +
            $"to add {nameof(handle)} to {nameof(responseHandlers)}");
        
        return new(handle, index, responseIdentifier);
    }
    private async Task TeardownCallHandler(CallInfo callInfo) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);

        bool removed = responseHandlers.TryRemove(callInfo.ResponseIdentifier);

        handleRecycling.Enqueue(callInfo.ResponseHandle);
        indexRecycling.Enqueue(callInfo.Index);

        syncSemaphore.Release();

        Debug.Assert(removed, $"{nameof(CallResponseHandler)}.{nameof(Call)} should never fail " +
            $"to remove {nameof(callInfo.ResponseHandle)} from {nameof(responseHandlers)}");
    }
}