using TaininIPC.Data.Protocol;

namespace TaininIPC.Data.Serialized;

/// <summary>
/// Static helper class providing properties and functions for getting <see cref="NetworkChunk"/> instances 
/// which represent the structure of a <see cref="MultiFrame"/>.
/// </summary>
public static class FrameChunks {
    public static readonly NetworkChunk StartMultiFrame = new((byte)FrameInstruction.StartMultiFrame, ReadOnlyMemory<byte>.Empty);
    public static readonly NetworkChunk EndMultiFrame = new((byte)FrameInstruction.EndMultiFrame, ReadOnlyMemory<byte>.Empty);
    public static readonly NetworkChunk EndFrame = new((byte)FrameInstruction.EndFrame, ReadOnlyMemory<byte>.Empty);
    public static NetworkChunk StartFrame(ReadOnlyMemory<byte> key) => new((byte)FrameInstruction.StartFrame, key);
    public static NetworkChunk AppendBuffer(ReadOnlyMemory<byte> buffer) => new((byte)FrameInstruction.AppendBuffer, buffer);
}
