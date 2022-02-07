namespace TaininIPC.Data.Protocol;

public sealed record NetworkChunk(byte Instruction, ReadOnlyMemory<byte> Data) {
    public static readonly NetworkChunk StartMultiFrame = new(Instructions.StartMultiFrame, ReadOnlyMemory<byte>.Empty);
    public static readonly NetworkChunk EndMultiFrame = new(Instructions.EndMultiFrame, ReadOnlyMemory<byte>.Empty);
    public static readonly NetworkChunk EndFrame = new(Instructions.EndFrame, ReadOnlyMemory<byte>.Empty);
    public static NetworkChunk StartFrame(ReadOnlyMemory<byte> key) => new(Instructions.StartFrame, key);
    public static NetworkChunk AppendBuffer(ReadOnlyMemory<byte> buffer) => new(Instructions.AppendBuffer, buffer);
}