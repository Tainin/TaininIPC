using TaininIPC.CritBitTree.Interface;

namespace TaininIPC.CritBitTree.Abstract;

/// <summary>
/// An <see langword="abstract"/> base for implementations of <see cref="ICritBitKey{TId}"/>
/// </summary>
/// <typeparam name="TId"></typeparam>
public abstract class AbstractCritBitKey<TId> : ICritBitKey<TId> {
    private TId? id = default;
    private ReadOnlyMemory<byte> memory = ReadOnlyMemory<byte>.Empty;

    /// <inheritdoc cref="ICritBitKey{T}.Id"/>
    public TId Id => id is null ? (id = CalculateId()) : id;
    /// <inheritdoc cref="ICritBitKey.Memory"/>
    public ReadOnlyMemory<byte> Memory => memory.IsEmpty ? (memory = CalculateMemory()) : memory;

    /// <summary>
    /// Initializes an <see cref="AbstractCritBitKey{T}"/> from it's <paramref name="id"/> representation.
    /// </summary>
    /// <param name="id">The id to initialize the key from.</param>
    public AbstractCritBitKey(TId id) => this.id = id;
    /// <summary>
    /// Initializes an <see cref="AbstractCritBitKey{T}"/> from it's memory representation.
    /// </summary>
    /// <param name="memory">The region of memory to initialize the key from.</param>
    public AbstractCritBitKey(ReadOnlyMemory<byte> memory) => this.memory = memory;

    /// <summary>
    /// Calculates the key's id representation from it's memory representation.
    /// </summary>
    /// <returns>The id representation of the key.</returns>
    protected abstract TId CalculateId();
    /// <summary>
    /// Calculates the key's memory representation from it's id representation.
    /// </summary>
    /// <returns>The memory representation of the key.</returns>
    protected abstract ReadOnlyMemory<byte> CalculateMemory();
}
