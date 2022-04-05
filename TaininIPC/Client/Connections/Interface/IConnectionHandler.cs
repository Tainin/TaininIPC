namespace TaininIPC.Client.Connections.Interface;

/// <summary>
/// Represents an object which can handle new connection attempts.
/// </summary>
public interface IConnectionHandler {
    /// <summary>
    /// Handles the specified <paramref name="connection"/> attempt.
    /// </summary>
    /// <param name="connection">The connection attempt to handle.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public Task HandleConnection(IConnection connection);
}
