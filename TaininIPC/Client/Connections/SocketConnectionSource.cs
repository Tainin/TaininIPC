using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using TaininIPC.Client.Connections.Interface;
using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;
using TaininIPC.Network.Interface;
using TaininIPC.Network.Sockets;
using TaininIPC.Utils;

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

    /// <summary>
    /// Runs a background service which continually accepts incoming socket connections
    /// and passes them off to <see cref="ConnectionHandler"/>.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task Run(CancellationToken cancellationToken) {
        listenerSocket.Listen();
        while (true) {
            try {
                Socket socket = await listenerSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                await ConnectionHandler.HandleConnection(new Connection(socket, timeoutOptions)).ConfigureAwait(false);
            } catch (OperationCanceledException) {
                break;
            }
        }
        listenerSocket.Close();
    }
    /// <inheritdoc cref="IConnectionSource.CompleteConnectionRequest(Frame)"/>
    public async Task CompleteConnectionRequest(Frame frame) {
        if (!TryDeserializeSocketAddress(frame, out SocketAddress? address)) return;

        IPEndPoint endpoint = new(0, 0);
        endpoint = (IPEndPoint)endpoint.Create(address);

        Socket socket = new(address.Family, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(endpoint).ConfigureAwait(false);

        await ConnectionHandler.HandleConnection(new Connection(socket, timeoutOptions)).ConfigureAwait(false);
    }
    /// <summary>
    /// Gets the <see cref="SocketAddress"/> of the listening socket encoded in a <see cref="Frame"/>.
    /// </summary>
    /// <returns>The <see cref="Frame"/> encoded <see cref="SocketAddress"/>.</returns>
    public Frame GetConnectionInfoAsFrame() {
        Frame frame = new();
        EndPoint? endpoint = listenerSocket.LocalEndPoint;
        if (endpoint is null) return frame;
        if (endpoint is not IPEndPoint ipEndpoint) return frame;

        SocketAddress address = ipEndpoint.Serialize();
        SerializeSocketAddress(address, frame);
        return frame;
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="SocketAddress"/> from the given <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">The frame to attempt to deserialize the <see cref="SocketAddress"/> from.</param>
    /// <param name="address">Contains the deserialized <see cref="SocketAddress"/> on return if successful.</param>
    /// <returns><see langword="true"/> if the given <paramref name="frame"/> was able to be deserialized to a <see cref="SocketAddress"/>,
    /// <see langword="false"/> otherwise.</returns>
    public static bool TryDeserializeSocketAddress(Frame frame, [NotNullWhen(true)] out SocketAddress? address) {
        int addressSize = BinaryPrimitives.ReadInt32BigEndian(frame.Get(0).Span);
        ReadOnlySpan<byte> addressSpan = frame.Get(1).Span;

        if (addressSpan.Length != addressSize) return UtilityFunctions.DefaultAndFalse(out address);

        // First 2 bytes of the address buffer contain it's family
        AddressFamily family = (AddressFamily)BinaryPrimitives.ReadInt16LittleEndian(addressSpan);

        address = new(family, addressSize);

        int start = sizeof(short); // Skip over the bytes used as the family
        for (int i = start; i < addressSize; i++) address[i] = addressSpan[i];
        return true;
    }
    /// <summary>
    /// Serializes the given <paramref name="socketAddress"/> to the given <paramref name="frame"/>.
    /// </summary>
    /// <param name="socketAddress">The <see cref="SocketAddress"/> to serialize.</param>
    /// <param name="frame">The frame to serialize the given <paramref name="socketAddress"/> into.</param>
    /// <remarks>The given <paramref name="frame"/> is cleared prior to serialization.</remarks>
    public static void SerializeSocketAddress(SocketAddress socketAddress, Frame frame) {
        frame.Clear();
        int addressSize = socketAddress.Size;
        Memory<byte> sizeBuffer = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(sizeBuffer.Span, addressSize);
        frame.Append(sizeBuffer);

        Memory<byte> addressBuffer = Enumerable.Range(0, addressSize)
            .Select(i => socketAddress[i]).ToArray();
        frame.Append(addressBuffer);
    }
}
