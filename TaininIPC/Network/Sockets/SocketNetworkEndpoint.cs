using System.Buffers.Binary;
using System.Net.Sockets;
using TaininIPC.Data.Protocol;
using TaininIPC.Network.Interface;

namespace TaininIPC.Network.Sockets;

public sealed class SocketNetworkEndpoint : INetworkEndpoint {

    #region Transport instruction
    private static readonly int INSTRUCTION_BUFFER_LENGTH = 2;

    private static readonly byte EXTERNAL_FLAG = (1 << 7);
    private static readonly byte LONG_DATA_FLAG = (1 << 6);

    private static readonly int SHORT_DATA_LENGTH_MASK = LONG_DATA_FLAG - 1;
    private static readonly int SHORT_DATA_MAX_LENGTH = (1 << 5);
    #endregion

    #region Endpoint instructions
    private static readonly byte INITIAL_FLAG = (1 << 0);
    private static readonly byte KEEP_ALIVE_FLAG = (1 << 1);
    private static readonly byte DISCONNECT_FLAG = (1 << 2);
    #endregion

    public event EventHandler<EndpointStatusChangedEventArgs>? EndpointStatusChanged;

    private readonly Socket connection;
    private readonly TimeoutOptions timeoutOptions;
    private readonly ChunkHandler incomingChunkHandler;

    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly SemaphoreSlim keepAliveBeginSemaphore;
    private readonly SemaphoreSlim disconnectSemaphore;
    private readonly SemaphoreSlim sendSemaphore;

    private int status;
    private long keepAliveExpiresAt;

    public EndpointStatus Status => (EndpointStatus)Interlocked.CompareExchange(ref status, 0, 0);

    public SocketNetworkEndpoint(Socket socket, ChunkHandler incomingChunkHandler, TimeoutOptions timeoutOptions) {
        connection = socket;
        this.timeoutOptions = timeoutOptions;
        this.incomingChunkHandler = incomingChunkHandler;

        cancellationTokenSource = new CancellationTokenSource();
        keepAliveBeginSemaphore = new(0, 1);
        disconnectSemaphore = new(0, 1);
        sendSemaphore = new(1, 1);
        UpdateStatus(EndpointStatus.Unstarted, Status);
    }

    public async Task RunSocketServices() {
        if (!UpdateStatus(EndpointStatus.Starting, EndpointStatus.Unstarted))
            throw new InvalidOperationException($"Cannot start a {nameof(SocketNetworkEndpoint)} which is already running.");

        await InitializeConncetion().ConfigureAwait(false);

        Task[] lifetimeTasks = new Task[] { KeepAliveService(), ReceiveService(), TimeoutService(), DisconnectService() };
        Task firstToFinish = await Task.WhenAny(lifetimeTasks).ConfigureAwait(false);
        if (firstToFinish.IsFaulted) UpdateStatus(EndpointStatus.Faulted, EndpointStatus.Running);

        disconnectSemaphore.Release();
        cancellationTokenSource.Cancel();

        Exception? exception = null;

        try {
            await Task.WhenAll(lifetimeTasks).ConfigureAwait(false);
        } catch (Exception ex) {
            if (Status is EndpointStatus.Faulted) exception = ex;
        }

        UpdateStatus(EndpointStatus.Stopped, EndpointStatus.Running);

        try {
            connection.Shutdown(SocketShutdown.Both);
        } catch (Exception ex) {
            exception = exception is null ? ex : new AggregateException(exception, ex);
        }

        connection.Close();

        if (exception is not null) throw exception;
    }
    public void StopSocketServices() {
        UpdateStatus(EndpointStatus.Stopped, EndpointStatus.Running);
        cancellationTokenSource.Cancel();
    }
    public async Task SendChunk(NetworkChunk chunk) {
        if (Status is not EndpointStatus.Running) throw new InvalidOperationException("Cannot send throug an endpoint which isn't running.");
        await SendChunkInternal(chunk, external: true).ConfigureAwait(false);
    }

    private async Task InitializeConncetion() {
        await SendChunkInternal(new(INITIAL_FLAG, ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);
        (NetworkChunk receivedInitialization, bool isExternal) = await ReceiveChunk().ConfigureAwait(false);

        string? message = null;

        if (isExternal)
            message = "Failed to start. Chunk received during initialization should have originated internally";

        if (receivedInitialization.Instruction != INITIAL_FLAG)
            message = "Failed to start. Chunk received during initialization should contain initialization instruction.";

        if (receivedInitialization.Data.Length > 0)
            message = "Failed to start. Chunk received during initialization should not contain data.";

        if (message is not null) {
            UpdateStatus(EndpointStatus.Faulted, EndpointStatus.Starting);
            throw new IOException(message);
        }

        UpdateStatus(EndpointStatus.Running, EndpointStatus.Starting);
    }
    private async Task KeepAliveService() {
        await SendChunkInternal(new((byte)(KEEP_ALIVE_FLAG | INITIAL_FLAG), ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);
        while (Status is EndpointStatus.Running) {
            await SendChunkInternal(new(KEEP_ALIVE_FLAG, ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);
            await Task.Delay(timeoutOptions.Period, cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }
    private async Task ReceiveService() {
        while (Status is EndpointStatus.Running) {
            (NetworkChunk chunk, bool isExternal) = await ReceiveChunk().ConfigureAwait(false);

            if (isExternal) await incomingChunkHandler(chunk).ConfigureAwait(false);
            else HandleInternalChunk(chunk);
        }
    }
    private async Task TimeoutService() {
        await keepAliveBeginSemaphore.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        while (Status is EndpointStatus.Running) {
            long now = DateTime.UtcNow.Ticks;
            long expiresAt = Interlocked.Read(ref keepAliveExpiresAt);
            TimeSpan delay = TimeSpan.FromTicks(expiresAt - now);

            if (now > expiresAt) throw new TimeoutException("Keep alive timed out.");

            await Task.Delay(delay, cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }
    private async Task DisconnectService() {
        await disconnectSemaphore.WaitAsync().ConfigureAwait(false);
        await SendChunkInternal(new(DISCONNECT_FLAG, ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);
    }
    private void HandleInternalChunk(NetworkChunk chunk) {
        if ((chunk.Instruction & KEEP_ALIVE_FLAG) != 0) {
            long expiresAt = (DateTime.UtcNow + timeoutOptions.Timeout).Ticks;
            Interlocked.Exchange(ref keepAliveExpiresAt, expiresAt);

            if ((chunk.Instruction & INITIAL_FLAG) != 0) keepAliveBeginSemaphore.Release();
        } else if (chunk.Instruction == DISCONNECT_FLAG) StopSocketServices();
    }
    private async Task SendChunkInternal(NetworkChunk chunk, bool external = false) {
        static async Task SendBuffer(Socket socket, ReadOnlyMemory<byte> buffer) {
            int offset = 0;
            int length = buffer.Length;
            while (offset < length)
                offset += await socket.SendAsync(buffer[offset..^0], SocketFlags.None).ConfigureAwait(false);
        }

        int dataLength = chunk.Data.Length;
        bool shortData = dataLength <= SHORT_DATA_MAX_LENGTH;

        byte lowLevelInstruction = shortData ?
            (byte)(dataLength & SHORT_DATA_LENGTH_MASK) :
            LONG_DATA_FLAG;

        lowLevelInstruction |= external ? EXTERNAL_FLAG : (byte)0;
        byte[] instructions = new byte[] { lowLevelInstruction, chunk.Instruction };

        try {
            await sendSemaphore.WaitAsync().ConfigureAwait(false);
            await SendBuffer(connection, instructions).ConfigureAwait(false);

            if (!shortData) {
                byte[] lengthBuffer = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, dataLength);
                await SendBuffer(connection, lengthBuffer).ConfigureAwait(false);
            }

            if (dataLength > 0)
                await SendBuffer(connection, chunk.Data).ConfigureAwait(false);
        } finally {
            sendSemaphore.Release();
        }
    }

    private async Task<(NetworkChunk chunk, bool isExternal)> ReceiveChunk() {
        static async Task ReceiveBuffer(Socket socket, Memory<byte> buffer, CancellationToken cancellationToken) {
            int offset = 0;
            while (offset <buffer.Length)
                offset += await socket.ReceiveAsync(buffer[offset..^0], SocketFlags.None, cancellationToken).ConfigureAwait(false);
        }

        byte[] instructionsBuffer = new byte[INSTRUCTION_BUFFER_LENGTH];
        await ReceiveBuffer(connection, instructionsBuffer, cancellationTokenSource.Token).ConfigureAwait(false);
        
        byte lowLevelInstruction = instructionsBuffer[0];
        byte highLevelInstruction = instructionsBuffer[1];

        bool isExternal = (lowLevelInstruction & EXTERNAL_FLAG) != 0;
        bool isLongData = (lowLevelInstruction & LONG_DATA_FLAG) != 0;

        int dataLength = lowLevelInstruction & SHORT_DATA_LENGTH_MASK;

        if (isLongData) {
            byte[] lengthBuffer = new byte[sizeof(int)];
            await ReceiveBuffer(connection, lengthBuffer, cancellationTokenSource.Token).ConfigureAwait(false);
            dataLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
        }

        byte[] data = new byte[dataLength];
        await ReceiveBuffer(connection, data, cancellationTokenSource.Token).ConfigureAwait(false);
        return (new(highLevelInstruction, data), isExternal);
    }

    private bool UpdateStatus(EndpointStatus newStatus, EndpointStatus comparand) {
        EndpointStatus oldStatus = (EndpointStatus)Interlocked.CompareExchange(ref status, (int)newStatus, (int)comparand);

        if (oldStatus != comparand) return false;

        if (oldStatus != newStatus) 
            EndpointStatusChanged?.Invoke(this, new(oldStatus, newStatus));

        return true;
    }
}