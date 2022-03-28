using TaininIPC.Client;
using TaininIPC.Data.Frames;
using TaininIPC.Data.Frames.Serialization;
using TaininIPC.Data.Protocol;

namespace TaininIPC.Network;

/// <summary>
/// Provides a higher level endpoint for sending and processing received <see cref="MultiFrame"/> instances
/// </summary>
public class FrameEndpoint {
    private readonly ChunkHandler outgoingChunkHandler;
    private readonly MultiFrameHandler incomingFrameHandler;
    private readonly SemaphoreSlim sendSemaphore;
    private readonly FrameDeserializer frameDeserializer;

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

        frameDeserializer = new();
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
            foreach (NetworkChunk chunk in FrameSerializer.SerializeMultiFrame(multiFrame))
                await outgoingChunkHandler(chunk).ConfigureAwait(false);
        } finally {
            sendSemaphore.Release();
        }
    }
    /// <summary>
    /// Applies the specified <paramref name="chunk"/> to the deserialization of the current <see cref="MultiFrame"/>
    /// and calls the handler for incoming frames if the frame was completed.
    /// </summary>
    /// <param name="chunk">The <see cref="NetworkChunk"/> to apply.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task ApplyChunk(NetworkChunk chunk) {
        if (frameDeserializer.ApplyChunk(chunk, out MultiFrame? frame))
            await incomingFrameHandler(frame).ConfigureAwait(false);
    }
}
