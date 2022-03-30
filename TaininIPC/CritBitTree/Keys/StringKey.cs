using System.Text;
using TaininIPC.CritBitTree.Abstract;

namespace TaininIPC.CritBitTree.Keys;

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/> backed by a <see langword="string"/> id.
/// </summary>
public sealed class StringKey : AbstractCritBitKey<string> {
    /// <summary>
    /// Represents a <see cref="StringKey"/> backed by <see cref="string.Empty"/>.
    /// </summary>
    public static StringKey Empty { get; } = new(string.Empty);

    /// <summary>
    /// Initializes an <see cref="StringKey"/> from it's <see langword="string"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public StringKey(string id) : base(id) { }
    /// <summary>
    /// Initializes an <see cref="StringKey"/> from it's memory representation.
    /// </summary>
    /// <param name="key">The memory to initialize the key from.</param>
    public StringKey(ReadOnlyMemory<byte> key) : base(key) { }

    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateId"/>
    protected override string CalculateId() => Encoding.BigEndianUnicode.GetString(Memory.Span);
    /// <inheritdoc cref="AbstractCritBitKey{T}.CalculateMemory"/>
    protected override ReadOnlyMemory<byte> CalculateMemory() => Encoding.BigEndianUnicode.GetBytes(Id);
}
