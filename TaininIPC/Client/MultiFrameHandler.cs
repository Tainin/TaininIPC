using TaininIPC.Data.Frames;

namespace TaininIPC.Client;

/// <summary>
/// Represents a fucntion that will handle <see cref="MultiFrame"/> instances.
/// </summary>
/// <param name="multiFrame">The <see cref="MultiFrame"/> to handle.</param>
/// <returns>An asyncronous task representing the operation.</returns>
public delegate Task MultiFrameHandler(MultiFrame multiFrame);