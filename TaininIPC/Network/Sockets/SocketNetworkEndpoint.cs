using System.Buffers.Binary;
using System.Net.Sockets;
using TaininIPC.Data.Protocol;
using TaininIPC.Network.Interface;

namespace TaininIPC.Network.Sockets;

/// <summary>
/// Provides an endpoint for a network connection through a socket.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketNetworkEndpoint"/> class from the given socket.
    /// </summary>
    /// <param name="socket">Socket to use as the underlying network connection.</param>
    /// <param name="incomingChunkHandler">Handler function to be called on for each received <see cref="NetworkChunk"/></param>
    /// <param name="timeoutOptions">Options for configuring keep alive timings.</param>
    public SocketNetworkEndpoint(Socket socket, ChunkHandler incomingChunkHandler, TimeoutOptions timeoutOptions) {
        connection = socket;
        this.timeoutOptions = timeoutOptions;
        this.incomingChunkHandler = incomingChunkHandler;

        cancellationTokenSource = new CancellationTokenSource();

        // initialize semaphores such that they block on the first call to Wait pending the first call to Release
        keepAliveBeginSemaphore = new(0, 1);
        disconnectSemaphore = new(0, 1);

        // initialize semaphore to not block the first call to Wait but block all subsequent calls pending the corrisponding Release calls
        sendSemaphore = new(1, 1);

        // ensure that initial status is Unstarted even though it should be by default
        UpdateStatus(EndpointStatus.Unstarted, Status);
    }

    /// <summary>
    /// Run the four lifetime services (KeepAlive, Receive, Timeout, and Disconnect) required by a running <see cref="SocketNetworkEndpoint"/>.
    /// </summary>
    /// <returns>An asyncronous task that completes once the underlying services are stopped or fault.</returns>
    /// <exception cref="InvalidOperationException">If the <see cref="SocketNetworkEndpoint"/> is already in a state other than 
    /// <see cref="EndpointStatus.Unstarted"/></exception>
    public async Task Run() {
        // Check that the endpoint is in the Unstarted state and if so transition it to Starting
        // Otherwise throw an exception
        if (!UpdateStatus(EndpointStatus.Starting, EndpointStatus.Unstarted))
            throw new InvalidOperationException($"Cannot start a {nameof(SocketNetworkEndpoint)} which is already running.");

        // Perform connection handshake
        await InitializeConnection().ConfigureAwait(false);

        // Initialize lifetime tasks and wait for the first to stop or fault
        Task[] lifetimeTasks = new Task[] { KeepAliveService(), ReceiveService(), TimeoutService(), DisconnectService() };
        Task firstToFinish = await Task.WhenAny(lifetimeTasks).ConfigureAwait(false);

        // Check if the finished task ended in a fault
        // If so transition the endpoint to the Faulted state if it is not already in Stopped or Faulted
        if (firstToFinish.IsFaulted) UpdateStatus(EndpointStatus.Faulted, EndpointStatus.Running); //TODO: is the condition needed (and see below)

        // Allow disconnect serive to run.
        disconnectSemaphore.Release();

        // Cancell all operations withing the lifetime services
        cancellationTokenSource.Cancel();

        Exception? exception = null;

        // Allow all lifetime services to complete and catch any exceptions.
        try {
            await Task.WhenAll(lifetimeTasks).ConfigureAwait(false);
        } catch (Exception ex) {
            if (Status is EndpointStatus.Faulted) exception = ex; //TODO: is the condition needed (and see above)
        }

        // Transition the status to Stopped if it is not already Faulted
        UpdateStatus(EndpointStatus.Stopped, EndpointStatus.Running);

        // Attempt to shutdown both ends of the socket
        try {
            connection.Shutdown(SocketShutdown.Both);
        } catch (Exception ex) {
            exception = exception is null ? ex : new AggregateException(exception, ex);
        }

        // Close / dispose the socket
        connection.Close();

        // Throw any exceptions which may have occured
        if (exception is not null) throw exception;
    }
    public void Stop() {
        // Transition the endpoint to the Stopped status if is not already Stopped or Faulted.
        UpdateStatus(EndpointStatus.Stopped, EndpointStatus.Running);
        cancellationTokenSource.Cancel();
    }
    public async Task SendChunk(NetworkChunk chunk) {
        // Ensure that the endpoint is running
        if (Status is not EndpointStatus.Running) throw new InvalidOperationException("Cannot send throug an endpoint which isn't running.");
        // Call internal send routine and flag the operation as externally called
        await SendChunkInternal(chunk, external: true).ConfigureAwait(false);
    }

    
    /// <summary>
    /// Lifetime service which periodically sends a keep alive chunk to the remote.
    /// </summary>
    /// <returns>An asyncronous task that completes once the service is stopped or faults.</returns>
    private async Task KeepAliveService() {
        // Sends a chunk indicating the start of the keep alive sequence
        await SendChunkInternal(new((byte)(KEEP_ALIVE_FLAG | INITIAL_FLAG), ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);

        while (Status is EndpointStatus.Running) {
            // Send the keep alive chunk
            await SendChunkInternal(new(KEEP_ALIVE_FLAG, ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);

            // Delay for the specified time
            await Task.Delay(timeoutOptions.Period, cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// Lifetime servie which receives all incoming chunks.
    /// </summary>
    /// <returns>An asyncronous task that completes once the service is stopped or faults.</returns>
    private async Task ReceiveService() {
        while (Status is EndpointStatus.Running) {
            // Recieve the chunk
            (NetworkChunk chunk, bool isExternal) = await ReceiveChunk().ConfigureAwait(false);

            // Determine if chunk is meant to be handled by the endpoint or by the handler provided external chunks
            if (isExternal) await incomingChunkHandler(chunk).ConfigureAwait(false);
            else HandleInternalChunk(chunk);
        }
    }
    /// <summary>
    /// Lifetime service that ensures keep alive chunks are received often enough and throws an exception if not.
    /// </summary>
    /// <returns>An asyncronous task that completes once the service is stopped or faults.</returns>
    /// <exception cref="TimeoutException">If it has been longer than <see cref="TimeoutOptions.Timeout"/> since the last
    /// keep alive chunk was received.</exception>
    private async Task TimeoutService() {
        // wait for initial keep alive chunk.
        await keepAliveBeginSemaphore.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
        while (Status is EndpointStatus.Running) {
            // Calculate time until next expiration
            long now = DateTime.UtcNow.Ticks;
            long expiresAt = Interlocked.Read(ref keepAliveExpiresAt);
            TimeSpan delay = TimeSpan.FromTicks(expiresAt - now);

            // If the timeout has expired throw exception
            if (now > expiresAt) throw new TimeoutException("Keep alive timed out.");

            // Delay until next expiration
            await Task.Delay(delay, cancellationTokenSource.Token).ConfigureAwait(false);
        }
    }
    /// <summary>
    /// Lifetime service which simply yields until disconnect is requested and then signals the remote
    /// </summary>
    /// <returns>An asyncronous task that completes once the service is stopped or faults.</returns>
    private async Task DisconnectService() {
        await disconnectSemaphore.WaitAsync().ConfigureAwait(false);
        await SendChunkInternal(new(DISCONNECT_FLAG, ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);
    }

    /// <summary>
    /// Helper method which performs connection handshake
    /// </summary>
    /// <returns>An asyncronous task representing the operation.</returns>
    /// <exception cref="IOException">If the handshake failed.</exception>
    private async Task InitializeConnection() {
        // Send handshake chunk
        await SendChunkInternal(new(INITIAL_FLAG, ReadOnlyMemory<byte>.Empty)).ConfigureAwait(false);

        // Receive handshake chunk from remote
        (NetworkChunk receivedInitialization, bool isExternal) = await ReceiveChunk().ConfigureAwait(false);

        string? message = null;

        // Verify the received chunk matches what is expected during handshake
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

        //Transition from Starting state to Running state
        UpdateStatus(EndpointStatus.Running, EndpointStatus.Starting);
    }
    /// <summary>
    /// Helper method which handles chunks intended for the endpoint itself.
    /// </summary>
    /// <param name="chunk">The chunk which needs handling.</param>
    private void HandleInternalChunk(NetworkChunk chunk) {
        // Handle keep alive chunks
        if ((chunk.Instruction & KEEP_ALIVE_FLAG) != 0) {
            // Calculate next expiration time
            long expiresAt = (DateTime.UtcNow + timeoutOptions.Timeout).Ticks;
            Interlocked.Exchange(ref keepAliveExpiresAt, expiresAt);

            // Release TimeoutService waiting for initial keep alive chunk if necessary
            if ((chunk.Instruction & INITIAL_FLAG) != 0) keepAliveBeginSemaphore.Release();

        // Handle disconnect chunk
        } else if (chunk.Instruction == DISCONNECT_FLAG) Stop();
    }
    /// <summary>
    /// Helper method for sending chunks to the remote
    /// </summary>
    /// <param name="chunk">The chunk to send</param>
    /// <param name="external">Flag indicating the chunk is meant to be handled outside the <see cref="SocketNetworkEndpoint"/></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    private async Task SendChunkInternal(NetworkChunk chunk, bool external = false) {
        // Determine data length and whether or not an extra length field is necessary
        int dataLength = chunk.Data.Length;
        bool shortData = dataLength <= SHORT_DATA_MAX_LENGTH;

        // Encode length into instruction or set flag indicating the use of a length field
        byte lowLevelInstruction = shortData ?
            (byte)(dataLength & SHORT_DATA_LENGTH_MASK) :
            LONG_DATA_FLAG;

        // Encode external flag into instruction
        lowLevelInstruction |= external ? EXTERNAL_FLAG : (byte)0;
        // Package low and high level instruction into buffer
        byte[] instructions = new byte[] { lowLevelInstruction, chunk.Instruction };

        try {
            // Use semaphore to ensure multiple chunks don't get interlaced
            await sendSemaphore.WaitAsync().ConfigureAwait(false);
            // Send instruction buffer
            await connection.SendBuffer(instructions).ConfigureAwait(false);

            // Send length field if needed
            if (!shortData) {
                byte[] lengthBuffer = new byte[sizeof(int)];
                BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, dataLength);
                await connection.SendBuffer(lengthBuffer).ConfigureAwait(false);
            }

            // Send data buffer
            if (dataLength > 0)
                await connection.SendBuffer(chunk.Data).ConfigureAwait(false);
        } finally {
            sendSemaphore.Release();
        }
    }
    /// <summary>
    /// Helper method for receiving chunks from the remote
    /// </summary>
    /// <returns>An asyncronous task representing the operation.</returns>
    private async Task<(NetworkChunk chunk, bool isExternal)> ReceiveChunk() {
        // Receive instructions
        byte[] instructionsBuffer = new byte[INSTRUCTION_BUFFER_LENGTH];
        await connection.ReceiveBuffer(instructionsBuffer, cancellationTokenSource.Token).ConfigureAwait(false);
        
        // Unpack instructions
        byte lowLevelInstruction = instructionsBuffer[0];
        byte highLevelInstruction = instructionsBuffer[1];

        // Unpack flags
        bool isExternal = (lowLevelInstruction & EXTERNAL_FLAG) != 0;
        bool isLongData = (lowLevelInstruction & LONG_DATA_FLAG) != 0;

        int dataLength = lowLevelInstruction & SHORT_DATA_LENGTH_MASK;

        // Receive separate length field if necessary
        if (isLongData) {
            byte[] lengthBuffer = new byte[sizeof(int)];
            await connection.ReceiveBuffer(lengthBuffer, cancellationTokenSource.Token).ConfigureAwait(false);
            dataLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
        }

        // Receive data if necessary
        Memory<byte> data = Memory<byte>.Empty;
        if (dataLength > 0) {
            data = new byte[dataLength];
            await connection.ReceiveBuffer(data, cancellationTokenSource.Token).ConfigureAwait(false);
        }
        
        return (new(highLevelInstruction, data), isExternal);
    }
    /// <summary>
    /// Helper method for updating the status of the <see cref="SocketNetworkEndpoint"/>.
    /// Sets the status to <paramref name="newStatus"/> only if it is currently <paramref name="comparand"/>.
    /// </summary>
    /// <param name="newStatus">The new status.</param>
    /// <param name="comparand">The value to compare the current status against.</param>
    /// <returns>True if the old status was equal to <paramref name="comparand"/>. False otherwise.</returns>
    private bool UpdateStatus(EndpointStatus newStatus, EndpointStatus comparand) {
        // CompareExchange the status
        EndpointStatus oldStatus = (EndpointStatus)Interlocked.CompareExchange(ref status, (int)newStatus, (int)comparand);

        // If the old status was not equal to the comparand no change was made
        if (oldStatus != comparand) return false;

        // If the old status is different from the new status raise the EndpointStatusChanged event
        if (oldStatus != newStatus) 
            EndpointStatusChanged?.Invoke(this, new(oldStatus, newStatus));

        // Return true as the status was changed
        return true;
    }
}