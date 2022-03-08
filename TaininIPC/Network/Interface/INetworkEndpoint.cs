using TaininIPC.Data.Protocol;

namespace TaininIPC.Network.Interface;

/// <summary>
/// Represents one of two endpoints of a connection
/// </summary>
public interface INetworkEndpoint {
    /// <summary>
    /// Occurs when the status of the endpoint changes
    /// </summary>
    public event EventHandler<EndpointStatusChangedEventArgs> EndpointStatusChanged;
    /// <summary>
    /// Represents the current status of the endpoint
    /// </summary>
    public EndpointStatus Status { get; }
    /// <summary>
    /// Sends the provided <see cref="NetworkChunk"/> to the remote end of the connection
    /// </summary>
    /// <param name="chunk"></param>
    /// <returns>An asyncronous task that represents the send operation.</returns>
    public Task SendChunk(NetworkChunk chunk);
    /// <summary>
    /// Run any lifetime service(s) the endpoint requires (e.g. a receive loop)
    /// </summary>
    /// <returns>An asyncronous task that completes once the underlying service(s) are stopped or fault.</returns>
    public Task Run();
    /// <summary>
    /// Stops any lifetime service(s) that were started by the <see cref="Run"/> method.
    /// </summary>
    public void Stop();
}
