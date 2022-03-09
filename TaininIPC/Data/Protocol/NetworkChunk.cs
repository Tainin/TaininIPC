namespace TaininIPC.Data.Protocol;

/// <summary>
/// Represents a single packet for transfer over the network.
/// </summary>
/// <param name="Instruction">A simple byte instruction code used to indicate the purpose of the packet.</param>
/// <param name="Data">The data buffer for the packet.</param>
public sealed record NetworkChunk(byte Instruction, ReadOnlyMemory<byte> Data);