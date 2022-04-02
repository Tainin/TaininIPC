using System.Buffers.Binary;
using TaininIPC.CritBitTree.Abstract;

namespace TaininIPC.CritBitTree.Keys;

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/> backed by a <see langword="long"/> id.
/// </summary>
public sealed class Int64Key : AbstractCritBitKey<long> {
    /// <summary>
    /// Initializes an <see cref="Int64Key"/> from it's <see langword="long"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public Int64Key(long id) : base(id) { }
    /// <summary>
    /// Initializes an <see cref="Int64Key"/> from it's memory representation.
    /// </summary>
    /// <param name="memory">The memory to initialize the key from.</param>
    public Int64Key(ReadOnlyMemory<byte> memory) : base(memory[..sizeof(long)]) { }

    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateId"/>
    protected override long CalculateId() => BinaryPrimitives.ReadInt64BigEndian(Memory.Span);
    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateMemory"/>
    protected override ReadOnlyMemory<byte> CalculateMemory() {
        byte[] buffer = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(buffer, Id);
        return buffer;
    }
}
