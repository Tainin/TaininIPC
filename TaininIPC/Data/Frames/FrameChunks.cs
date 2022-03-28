using TaininIPC.Data.Protocol;

namespace TaininIPC.Data.Frames;

/// <summary>
/// Static helper class providing properties and functions for getting <see cref="NetworkChunk"/> instances 
/// which represent the structure of a <see cref="MultiFrame"/>.
/// </summary>
public static class FrameChunks {
    /// <summary>
    /// Represents the start of a new <see cref="MultiFrame"/>.
    /// </summary>
    public static readonly NetworkChunk StartMultiFrame = new((byte)FrameInstruction.StartMultiFrame, ReadOnlyMemory<byte>.Empty);
    /// <summary>
    /// Represents the end of the current <see cref="MultiFrame"/>.
    /// </summary>
    public static readonly NetworkChunk EndMultiFrame = new((byte)FrameInstruction.EndMultiFrame, ReadOnlyMemory<byte>.Empty);
    /// <summary>
    /// Represents the end of the current sub-<see cref="Frame"/>.
    /// </summary>
    public static readonly NetworkChunk EndFrame = new((byte)FrameInstruction.EndFrame, ReadOnlyMemory<byte>.Empty);
    /// <summary>
    /// Gets a <see cref="NetworkChunk"/> which represents the start of a sub-<see cref="Frame"/> in serialized form.
    /// </summary>
    /// <param name="key">The key to map to the new sub-<see cref="Frame"/>.</param>
    /// <returns>The <see cref="NetworkChunk"/> representation.</returns>
    public static NetworkChunk StartFrame(ReadOnlyMemory<byte> key) => new((byte)FrameInstruction.StartFrame, key);
    /// <summary>
    /// Gets a <see cref="NetworkChunk"/> which reprents the given <paramref name="buffer"/> of data in a serialized <see cref="Frame"/>.
    /// </summary>
    /// <param name="buffer">The buffer to represent.</param>
    /// <returns>The <see cref="NetworkChunk"/> representation.</returns>
    public static NetworkChunk AppendBuffer(ReadOnlyMemory<byte> buffer) => new((byte)FrameInstruction.AppendBuffer, buffer);
}
