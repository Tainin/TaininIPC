namespace TaininIPC.Data.Protocol;

public sealed record NetworkChunk(byte Instruction, ReadOnlyMemory<byte> Data);