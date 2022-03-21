namespace TaininIPC.CritBitTree.Interface;

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/>
/// </summary>
public interface ICritBitKey {
    /// <summary>
    /// The key as a region of memory.
    /// </summary>
    public ReadOnlyMemory<byte> Memory { get; }
    /// <summary>
    /// The key as a span of memory.
    /// </summary>
    public sealed ReadOnlySpan<byte> Span => Memory.Span;
}

/// <summary>
/// Represents the key of a <see cref="CritBitTree{TKey, TValue}"/> which can be converted to and from an id of another type.
/// </summary>
/// <typeparam name="T">The type of the id</typeparam>
public interface ICritBitKey<T> : ICritBitKey {
    /// <summary>
    /// The id which can be converted to and from <see cref="ICritBitKey.Memory"/>
    /// </summary>
    public T Id { get; }
}