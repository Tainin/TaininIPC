using TaininIPC.CritBitTree.Abstract;

namespace TaininIPC.CritBitTree.Keys;

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/> backed by a <see langword="byte"/> id.
/// </summary>
public sealed class ByteKey : AbstractCritBitKey<byte> {
    /// <summary>
    /// Initializes an <see cref="ByteKey"/> from it's <see langword="short"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public ByteKey(byte id) : base(id) { }
    /// <summary>
    /// Initializes an <see cref="ByteKey"/> from it's memory representation.
    /// </summary>
    /// <param name="memory">The memory to initialize the key from.</param>
    public ByteKey(ReadOnlyMemory<byte> memory) : base(memory[..sizeof(byte)]) { }

    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateId"/>
    protected override byte CalculateId() => Memory.Span[0];
    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateMemory"/>
    protected override ReadOnlyMemory<byte> CalculateMemory() => new byte[] { Id };
}
