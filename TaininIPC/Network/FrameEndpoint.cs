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
    private readonly MultiFrameHandler incomingMultiFrameHandler;
    private readonly SemaphoreSlim sendSemaphore;

    private MultiFrame workingMultiFrame = null!;
    private Frame workingFrame = null!;
    private IncomingFrameState incomingFrameState;

    public FrameEndpoint(ChunkHandler outgoingChunkHandler, MultiFrameHandler incomingMultiFrameHandler) {
        this.outgoingChunkHandler = outgoingChunkHandler;
        this.incomingMultiFrameHandler = incomingMultiFrameHandler;
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
            await incomingMultiFrameHandler(workingMultiFrame).ConfigureAwait(false);
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
            await outgoingChunkHandler(NetworkChunk.StartMultiFrame).ConfigureAwait(false);
            foreach ((ReadOnlyMemory<byte> key, Frame frame) in multiFrame.Serialized) {
                await outgoingChunkHandler(NetworkChunk.StartFrame(key)).ConfigureAwait(false);
                foreach (ReadOnlyMemory<byte> buffer in frame.Serialized)
                    await outgoingChunkHandler(NetworkChunk.AppendBuffer(buffer)).ConfigureAwait(false);
                await outgoingChunkHandler(NetworkChunk.EndFrame).ConfigureAwait(false);
            }
            await outgoingChunkHandler(NetworkChunk.EndMultiFrame).ConfigureAwait(false);
        } finally {
            sendSemaphore.Release();
        }
    }
}
