using System.Buffers.Binary;
using TaininIPC.CritBitTree.Abstract;

namespace TaininIPC.CritBitTree.Keys;

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/> backed by a <see langword="short"/> id.
/// </summary>
public sealed class Int16Key : AbstractCritBitKey<short> {
    /// <summary>
    /// Initializes an <see cref="Int16Key"/> from it's <see langword="short"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public Int16Key(short id) : base(id) { }
    /// <summary>
    /// Initializes an <see cref="Int16Key"/> from it's memory representation.
    /// </summary>
    /// <param name="key">The memory to initialize the key from.</param>
    public Int16Key(ReadOnlyMemory<byte> key) : base(key[..sizeof(short)]) { }

    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateId"/>
    protected override short CalculateId() => BinaryPrimitives.ReadInt16BigEndian(Memory.Span);
    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateMemory"/>
    protected override ReadOnlyMemory<byte> CalculateMemory() {
        byte[] keyBuffer = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(keyBuffer, Id);
        return keyBuffer;
    }
}
