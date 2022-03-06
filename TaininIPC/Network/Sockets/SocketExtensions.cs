using System.Net.Sockets;

namespace TaininIPC.Network.Sockets;

public static class SocketExtensions {
    public static async Task SendBuffer(this Socket socket, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
        int offset = 0;
        int length = buffer.Length;
        while (offset < length) 
            offset += await socket.SendAsync(buffer[offset..^0], SocketFlags.None, cancellationToken).ConfigureAwait(false);
    }

    public static async Task ReceiveBuffer(this Socket socket, Memory<byte> buffer, CancellationToken cancellationToken = default) {
        int offset = 0;
        int length = buffer.Length;
        while (offset < length)
            await socket.ReceiveAsync(buffer[offset..^0], SocketFlags.None, cancellationToken).ConfigureAwait(false);
    }
}
