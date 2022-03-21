using TaininIPC.CritBitTree.Interface;

namespace TaininIPC.CritBitTree.Abstract;

/// <summary>
/// An <see langword="abstract"/> base for implementations of <see cref="ICritBitKey{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AbstractCritBitKey<T> : ICritBitKey<T> {
    private T? id = default;
    private ReadOnlyMemory<byte> key = ReadOnlyMemory<byte>.Empty;

    /// <inheritdoc cref="ICritBitKey{T}.Id"/>
    public T Id => id is null ? (id = CalculateId()) : id;
    /// <inheritdoc cref="ICritBitKey.Memory"/>
    public ReadOnlyMemory<byte> Memory => key.IsEmpty ? (key = CalculateMemory()) : key;

    /// <summary>
    /// Initializes an <see cref="AbstractCritBitKey{T}"/> from it's <paramref name="id"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public AbstractCritBitKey(T id) => this.id = id;
    /// <summary>
    /// Initializes an <see cref="AbstractCritBitKey{T}"/> from it's memory representation.
    /// </summary>
    /// <param name="key">The region of memory to initialize the key from.</param>
    public AbstractCritBitKey(ReadOnlyMemory<byte> key) => this.key = key;

    /// <summary>
    /// Calculates the key's id representation from it's memory representation.
    /// </summary>
    /// <returns>The id representation of the key.</returns>
    protected abstract T CalculateId();
    /// <summary>
    /// Calculates the key's memory representation from it's id representation.
    /// </summary>
    /// <returns>The memory representation of the key.</returns>
    protected abstract ReadOnlyMemory<byte> CalculateMemory();
}
