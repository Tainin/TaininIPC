using System.Net.Sockets;

namespace TaininIPC.Network.Sockets;

/// <summary>
/// Provides extension methods for sending / receiving buffers of known sizes to / from sockets.
/// </summary>
public static class SocketExtensions {
    /// <summary>
    /// Sends the given <paramref name="buffer"/> to the provided <paramref name="socket"/>.
    /// </summary>
    /// <param name="socket">The socket to which the buffer should be sent.</param>
    /// <param name="buffer">The buffer to send to the socket.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asyncronous task that represents the send operation.</returns>
    public static async Task SendBuffer(this Socket socket, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        int offset = 0;
        int length = buffer.Length;
        while (offset < length) 
            offset += await socket.SendAsync(buffer[offset..^0], SocketFlags.None, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Receive into the given <paramref name="buffer"/> from the provided <paramref name="socket"/> until the buffer is filled.
    /// </summary>
    /// <param name="socket">The socket from which the buffer should be filled.</param>
    /// <param name="buffer">The buffer to be filled from the socket.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asyncronous task that represents the receive operation.</returns>
    public static async Task ReceiveBuffer(this Socket socket, Memory<byte> buffer, CancellationToken cancellationToken = default) {
        int offset = 0;
        int length = buffer.Length;
        while (offset < length)
            offset += await socket.ReceiveAsync(buffer[offset..^0], SocketFlags.None, cancellationToken).ConfigureAwait(false);
    }
}
