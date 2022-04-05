using System.Net.Sockets;
using TaininIPC.Client.Connections.Interface;
using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Interface;
using TaininIPC.Network.Sockets;

namespace TaininIPC.Client.Connections;

/// <summary>
/// Represents a source of new connection attempts through a <see cref="Socket"/>.
/// </summary>
public sealed class SocketConnectionSource : IConnectionSource {
    private sealed class Connection : IConnection {
        private readonly Socket socket;
        private readonly TimeoutOptions timeoutOptions;

        public Connection(Socket socket, TimeoutOptions timeoutOptions) =>
            (this.socket, this.timeoutOptions) = (socket, timeoutOptions);

        public INetworkEndpoint ToEndpoint(IRouter incomingFrameRouter) =>
            new SocketNetworkEndpoint(socket, incomingFrameRouter, timeoutOptions);
    }

    /// <inheritdoc cref="IConnectionSource.ConnectionHandler"/>
    public IConnectionHandler ConnectionHandler { get; }

    private readonly Socket listenerSocket;
    private readonly TimeoutOptions timeoutOptions;

    /// <summary>
    /// Initializes a new <see cref="SocketConnectionSource"/> from the given <paramref name="listenerSocket"/>, 
    /// <paramref name="timeoutOptions"/>, and <paramref name="connectionHandler"/>.
    /// </summary>
    /// <param name="listenerSocket">The listener socket which new connection attempts will be made through.</param>
    /// <param name="timeoutOptions">The timeout options to use when constructing <see cref="SocketNetworkEndpoint"/> instances.</param>
    /// <param name="connectionHandler">The connection handler used to handle connection attempts made by the new source.</param>
    public SocketConnectionSource(Socket listenerSocket, TimeoutOptions timeoutOptions, IConnectionHandler connectionHandler) {
        ConnectionHandler = connectionHandler;
        this.listenerSocket = listenerSocket;
        this.timeoutOptions = timeoutOptions;
    }

    public Task CompleteConnectionRequest(MultiFrame frame) => throw new NotImplementedException();
    public Frame GetConnectionInfoAsFrame() => throw new NotImplementedException();
}
