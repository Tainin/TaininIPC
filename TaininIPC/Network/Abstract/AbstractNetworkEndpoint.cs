using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;
using TaininIPC.Data.Frames.Serialization;
using TaininIPC.Data.Network;

namespace TaininIPC.Network.Abstract;

/// <summary>
/// Provides the <see langword="abstract"/> base class for network endpoint implementations.
/// </summary>
public abstract class AbstractNetworkEndpoint {

    /// <summary>
    /// Occurs when the status of the endpoint changes
    /// </summary>
    public event EventHandler<EndpointStatusChangedEventArgs>? EndpointStatusChanged;

    private readonly SemaphoreSlim sendFrameSempaphore;
    private readonly IRouter incomingFrameRouter;
    private readonly FrameDeserializer frameDeserializer;

    /// <summary>
    /// Represents the current status of the endpoint
    /// </summary>
    public abstract EndpointStatus Status { get; }

    /// <summary>
    /// Initializes a new <see cref="AbstractNetworkEndpoint"/> given an <see cref="IRouter"/> to route frames received by the endpoint.
    /// </summary>
    /// <param name="incomingFrameRouter">The <see cref="IRouter"/> used to route received frames.</param>
    public AbstractNetworkEndpoint(IRouter incomingFrameRouter) {
        this.incomingFrameRouter = incomingFrameRouter;
        sendFrameSempaphore = new(1, 1);
        frameDeserializer = new();
    }

    /// <summary>
    /// Override to run any lifetime services the decendant class requires.
    /// </summary>
    /// <returns>An asyncronous task which represents the operation.</returns>
    public abstract Task Run();
    /// <summary>
    /// Override to stop the lifetime services of the decendant class.
    /// </summary>
    public abstract void Stop();

    /// <summary>
    /// Sends the specified <paramref name="multiFrame"/> over the network.
    /// </summary>
    /// <param name="multiFrame">The frame to send.</param>
    /// <returns>An asyncronous task which represents the operation.</returns>
    public async Task SendMultiFrame(MultiFrame multiFrame) {
        await sendFrameSempaphore.WaitAsync().ConfigureAwait(false);
        try {
            foreach (NetworkChunk chunk in FrameSerializer.SerializeMultiFrame(multiFrame))
                await SendChunk(chunk).ConfigureAwait(false);
        } finally {
            sendFrameSempaphore.Release();
        }
    }

    /// <summary>
    /// Sends the specified <paramref name="chunk"/> over the network.
    /// </summary>
    /// <param name="chunk">The chunk to send.</param>
    /// <returns>An asyncronous task which represents the operation.</returns>
    protected abstract Task SendChunk(NetworkChunk chunk);

    /// <summary>
    /// Applies the specified <paramref name="chunk"/> to the deserialization of the current <see cref="MultiFrame"/>,
    /// and if it's application completes the frame, routes the frame using the <see cref="IRouter"/> provided at construction.
    /// </summary>
    /// <param name="chunk">The chunk to applu.</param>
    /// <returns>An asyncronous task which represents the operation.</returns>
    protected async Task ApplyChunk(NetworkChunk chunk) {
        if (frameDeserializer.ApplyChunk(chunk, out MultiFrame? frame))
            await incomingFrameRouter.RouteFrame(frame, null).ConfigureAwait(false);
    }

    /// <summary>
    /// Raises the <see cref="EndpointStatusChanged"/> event.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The arguments object for the event.</param>
    protected void OnEndpointStatusChanged(object? sender, EndpointStatusChangedEventArgs e) => EndpointStatusChanged?.Invoke(sender, e);
} 