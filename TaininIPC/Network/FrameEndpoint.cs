using TaininIPC.Client;
using TaininIPC.Data.Protocol;
using TaininIPC.Data.Serialized;

namespace TaininIPC.Network;

/// <summary>
/// Provides a higher level endpoint for sending and processing received <see cref="MultiFrame"/> instances
/// </summary>
public class FrameEndpoint {

    /// <summary>
    /// Specifies the state of the incoming <see cref="MultiFrame"/> as of the last received <see cref="NetworkChunk"/>
    /// </summary>
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

    /// <summary>
    /// Constructs an instance of the <see cref="FrameEndpoint"/> class from the given handlers.
    /// </summary>
    /// <param name="outgoingChunkHandler">The handler to be used to process outgoing <see cref="NetworkChunk"/> instances.</param>
    /// <param name="incomingFrameHandler">The handler to be used to process completed incoming <see cref="MultiFrame"/> instances.</param>
    public FrameEndpoint(ChunkHandler outgoingChunkHandler, MultiFrameHandler incomingFrameHandler) {
        this.outgoingChunkHandler = outgoingChunkHandler;
        this.incomingFrameHandler = incomingFrameHandler;

        // initialize semaphore to not block the first call to Wait but block all subsequent calls pending the corrisponding Release calls
        sendSemaphore = new(1, 1);

        incomingFrameState = IncomingFrameState.None;
    }

    /// <summary>
    /// Processes the provided <see cref="MultiFrame"/> into <see cref="NetworkChunk"/> 
    /// instances and sends them using <see cref="ChunkHandler"/> provided at construction.
    /// </summary>
    /// <param name="multiFrame">The <see cref="MultiFrame"/> to send.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task SendMultiFrame(MultiFrame multiFrame) {
        try {
            // Use semaphore to ensure multiple franes don't get interlaced
            await sendSemaphore.WaitAsync().ConfigureAwait(false);
            // Loop through all NetworkChunk instances and forward them to the outgoingChunkHandler
            foreach (NetworkChunk chunk in SerializeMultiFrame(multiFrame))
                await outgoingChunkHandler(chunk).ConfigureAwait(false);
        } finally {
            sendSemaphore.Release();
        }
    }
    /// <summary>
    /// Applies the <see cref="NetworkChunk.Instruction"/> of the provided <see cref="NetworkChunk"/>
    /// to the current working <see cref="MultiFrame"/>.
    /// </summary>
    /// <param name="chunk">The <see cref="NetworkChunk"/> to apply.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    /// <exception cref="InvalidOperationException">If the application of the <paramref name="chunk"/> or any previously
    /// applied chunks resulted in <see cref="IncomingFrameState.Error"/></exception>
    public async Task ApplyChunk(NetworkChunk chunk) {
        // Choose application behavior based on previous incomingFrameState
        incomingFrameState = incomingFrameState switch {
            IncomingFrameState.None => ExpectStartMultiFrame(chunk),
            IncomingFrameState.MultiFrame => InMultiFrameHandler(chunk),
            IncomingFrameState.Frame => InFrameHandler(chunk),
            _ => IncomingFrameState.Error,
        };

        // If the application resulted in a Completed frame forward it to the incomingFrameHandler and reset the state
        if (incomingFrameState == IncomingFrameState.Complete) {
            await incomingFrameHandler(workingMultiFrame).ConfigureAwait(false);
            incomingFrameState = IncomingFrameState.None;
        }

        if (incomingFrameState == IncomingFrameState.Error)
            throw new InvalidOperationException();
    }

    /// <summary>
    /// Helper function which handles applying chunks when the <see cref="incomingFrameState"/> is <see cref="IncomingFrameState.None"/>
    /// </summary>
    /// <param name="chunk">Chunk to apply.</param>
    /// <returns><see cref="IncomingFrameState.MultiFrame"/> or <see cref="IncomingFrameState.Error"/></returns>
    private IncomingFrameState ExpectStartMultiFrame(NetworkChunk chunk) {
        // Return Error state if the instruction was not the expected StartMultiFrame
        if (chunk.Instruction != Instructions.StartMultiFrame)
            return IncomingFrameState.Error;

        // Otherwise initialize new workingMultiFrame and return MultiFrame status
        workingMultiFrame = new();
        return IncomingFrameState.MultiFrame;
    }
    /// <summary>
    /// Helper function which handles applying chunks when the <see cref="incomingFrameState"/> is <see cref="IncomingFrameState.MultiFrame"/>
    /// </summary>
    /// <param name="chunk">Chunk to apply.</param>
    /// <returns><see cref="IncomingFrameState.Complete"/>, <see cref="IncomingFrameState.Frame"/>
    /// or <see cref="IncomingFrameState.Error"/></returns>
    private IncomingFrameState InMultiFrameHandler(NetworkChunk chunk) {
        // If end of MultiFrame has been reached reutrn Complete status
        if (chunk.Instruction == Instructions.EndMultiFrame)
            return IncomingFrameState.Complete;

        // If start of sub Frame has been reached initialize new workingFrame and reutrn Frame status
        if (chunk.Instruction == Instructions.StartFrame) {
            workingFrame = workingMultiFrame.Create(chunk.Data);
            return IncomingFrameState.Frame;
        }

        // Otherwise return Error status
        return IncomingFrameState.Error;
    }
    /// <summary>
    /// Helper function which handles applying chunks when the <see cref="incomingFrameState"/> is <see cref="IncomingFrameState.Frame"/>
    /// </summary>
    /// <param name="chunk">Chunk to apply.</param>
    /// <returns><see cref="IncomingFrameState.MultiFrame"/>, <see cref="IncomingFrameState.Error"/>
    /// or <see cref="IncomingFrameState.Error"/></returns>
    private IncomingFrameState InFrameHandler(NetworkChunk chunk) {
        // If end of Frame has been reached return back up to MultiFrame status
        if (chunk.Instruction == Instructions.EndFrame)
            return IncomingFrameState.MultiFrame;

        // If a buffer has been reached insert the chunks data into the workingFrame and stay in Frame state
        if (chunk.Instruction == Instructions.AppendBuffer) {
            workingFrame.Insert(chunk.Data, ^1);
            return IncomingFrameState.Frame;
        }

        // Otherwise return Error state
        return IncomingFrameState.Error;
    }

    /// <summary>
    /// Helper method to transform a <see cref="MultiFrame"/> into an <see cref="IEnumerable{T}"/> of <see cref="NetworkChunk"/> instances.
    /// </summary>
    /// <param name="multiFrame">The <see cref="MultiFrame"/> to transform.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="NetworkChunk"/> instances
    /// which can be used to rebuild the provided <see cref="MultiFrame"/></returns>
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
