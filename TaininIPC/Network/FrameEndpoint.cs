using TaininIPC.Client;
using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Network;

public class FrameEndpoint {

    private enum IncomingFrameState {
        None,
        MultiFrame,
        Frame,
        Complete,
        Error,
    }

    private readonly ChunkHandler outgoingChunkHandler;
    private readonly MultiFrameHandler incomingFrameHandler;
    private readonly SemaphoreSlim sendSemaphore;

    private MultiFrame workingMultiFrame = null!;
    private Frame workingFrame = null!;
    private IncomingFrameState incomingFrameState;

    public FrameEndpoint(ChunkHandler outgoingChunkHandler, MultiFrameHandler incomingFrameHandler) {
        this.outgoingChunkHandler = outgoingChunkHandler;
        this.incomingFrameHandler = incomingFrameHandler;
        sendSemaphore = new(1, 1);

        incomingFrameState = IncomingFrameState.None;
    }

    public async Task ApplyChunk(NetworkChunk chunk) {
        incomingFrameState = incomingFrameState switch {
            IncomingFrameState.None => ExpectStartMultiFrame(chunk),
            IncomingFrameState.MultiFrame => InMultiFrameHandler(chunk),
            IncomingFrameState.Frame => InFrameHandler(chunk),
            _ => IncomingFrameState.Error,
        };

        if (incomingFrameState == IncomingFrameState.Complete) {
            await incomingFrameHandler(workingMultiFrame).ConfigureAwait(false);
            incomingFrameState = IncomingFrameState.None;
        }

        if (incomingFrameState == IncomingFrameState.Error)
            throw new InvalidOperationException();
    }

    private IncomingFrameState ExpectStartMultiFrame(NetworkChunk chunk) {
        if (chunk.Instruction != Instructions.StartMultiFrame)
            return IncomingFrameState.Error;

        workingMultiFrame = new();
        return IncomingFrameState.MultiFrame;
    }

    private IncomingFrameState InMultiFrameHandler(NetworkChunk chunk) {
        if (chunk.Instruction == Instructions.EndMultiFrame)
            return IncomingFrameState.Complete;

        if (chunk.Instruction == Instructions.StartFrame) {
            workingFrame = workingMultiFrame.Create(chunk.Data);
            return IncomingFrameState.Frame;
        }

        return IncomingFrameState.Error;
    }

    private IncomingFrameState InFrameHandler(NetworkChunk chunk) {
        if (chunk.Instruction == Instructions.EndFrame)
            return IncomingFrameState.MultiFrame;

        if (chunk.Instruction == Instructions.AppendBuffer) {
            workingFrame.Insert(chunk.Data, ^1);
            return IncomingFrameState.Frame;
        }

        return IncomingFrameState.Error;
    }

    public async Task SendMultiFrame(MultiFrame multiFrame) {
        try {
            await sendSemaphore.WaitAsync().ConfigureAwait(false);
            foreach (NetworkChunk chunk in SerializeMultiFrame(multiFrame))
                await outgoingChunkHandler(chunk).ConfigureAwait(false);
        } finally {
            sendSemaphore.Release();
        }
    }

    private static IEnumerable<NetworkChunk> SerializeMultiFrame(MultiFrame multiFrame) {
        yield return NetworkChunk.StartMultiFrame;
        foreach ((ReadOnlyMemory<byte> key, Frame frame) in multiFrame.Serialized) {
            yield return NetworkChunk.StartFrame(key);
            foreach (ReadOnlyMemory<byte> buffer in frame.Serialized)
                yield return NetworkChunk.AppendBuffer(buffer);
            yield return NetworkChunk.EndFrame;
        }
        yield return NetworkChunk.EndMultiFrame;
    }
}
