using TaininIPC.Data.Protocol;

namespace TaininIPC.Network;

/// <summary>
/// Represents a method that will handle <see cref="NetworkChunk"/> instances.
/// </summary>
/// <param name="chunk">The <see cref="NetworkChunk"/> to handle.</param>
/// <returns>An asyncronous task representing the operation.</returns>
public delegate Task ChunkHandler(NetworkChunk chunk);