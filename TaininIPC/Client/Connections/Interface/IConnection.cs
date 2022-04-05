using TaininIPC.Client.Routing.Interface;
using TaininIPC.Network.Interface;

namespace TaininIPC.Client.Connections.Interface;

/// <summary>
/// Represents a single connection attempt to another <see cref="Node"/> which an <see cref="INetworkEndpoint"/> can be built from.
/// </summary>
public interface IConnection {
    /// <summary>
    /// Builds an <see cref="INetworkEndpoint"/> from the connection and the specified <paramref name="incomingFrameRouter"/>.
    /// </summary>
    /// <param name="incomingFrameRouter">The router to route frames received via the endpoint through.</param>
    /// <returns>The built endpoint.</returns>
    public INetworkEndpoint ToEndpoint(IRouter incomingFrameRouter);
}
