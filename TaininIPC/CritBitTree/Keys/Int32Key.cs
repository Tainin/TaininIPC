using System.Buffers.Binary;
using TaininIPC.CritBitTree.Abstract;

namespace TaininIPC.CritBitTree.Keys;

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/> backed by a <see langword="int"/> id.
/// </summary>
public sealed class Int32Key : AbstractCritBitKey<int> {
    /// <summary>
    /// Initializes an <see cref="Int32Key"/> from it's <see langword="int"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public Int32Key(int id) : base(id) { }
    /// <summary>
    /// Initializes an <see cref="Int32Key"/> from it's memory representation.
    /// </summary>
    /// <param name="memory">The memory to initialize the key from.</param>
    public Int32Key(ReadOnlyMemory<byte> memory) : base(memory[..sizeof(int)]) { }

    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateId"/>
    protected override int CalculateId() => BinaryPrimitives.ReadInt16BigEndian(Memory.Span);
    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateMemory"/>
    protected override ReadOnlyMemory<byte> CalculateMemory() {
        byte[] buffer = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, Id);
        return buffer;
    }
}
