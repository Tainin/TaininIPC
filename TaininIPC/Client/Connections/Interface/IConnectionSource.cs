using TaininIPC.Data.Frames;

namespace TaininIPC.Client.Connections.Interface;

/// <summary>
/// Represents a source of new connection attempts.
/// </summary>
public interface IConnectionSource {
    /// <summary>
    /// The connection handler which is responsible for handling the connection attempts.
    /// </summary>
    public IConnectionHandler ConnectionHandler { get; }
    /// <summary>
    /// Attempts to complete the connection request specified by the given <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">The frame containing the connection request.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public Task CompleteConnectionRequest(MultiFrame frame);
    /// <summary>
    /// Gets all information needed to connect to the source from the outside encoded in a <see cref="Frame"/>.
    /// </summary>
    /// <returns>The frame containing the connection information.</returns>
    public Frame GetConnectionInfoAsFrame();
}
