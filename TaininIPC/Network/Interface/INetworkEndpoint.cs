using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;

namespace TaininIPC.Network.Interface;

/// <summary>
/// Represents one endpoint of a two way one-to-one connection.
/// </summary>
public interface INetworkEndpoint {
    /// <summary>
    /// The router used to route received frames.
    /// </summary>
    public IRouter IncomingFrameRouter { get; }
    /// <summary>
    /// Represents the current status of the endpoint
    /// </summary>
    public EndpointStatus Status { get; }

    /// <summary>
    /// Occurs when the status of the endpoint changes
    /// </summary>
    public event EventHandler<EndpointStatusChangedEventArgs>? EndpointStatusChanged;

    /// <summary>
    /// Runs lifetime services of the endpoint.
    /// </summary>
    /// <returns>An asyncronous task which represents the operation.</returns>
    public Task Run();
    /// <summary>
    /// Sends the specified <paramref name="multiFrame"/> over the network.
    /// </summary>
    /// <param name="multiFrame">The frame to send.</param>
    /// <returns>An asyncronous task which represents the operation.</returns>
    public Task SendMultiFrame(MultiFrame multiFrame);
    /// <summary>
    /// Stops running lifetime services.
    /// </summary>
    public void Stop();
}