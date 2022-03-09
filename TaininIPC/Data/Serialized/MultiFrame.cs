using System.Buffers.Binary;
using TaininIPC.Utils;

namespace TaininIPC.Data.Serialized;

/// <summary>
/// Represents a mapped collection of <see cref="Frame"/> instances keyed by <see langword="short"/> ids.
/// </summary>
public sealed class MultiFrame {

    // Internal map of Frames
    private readonly CritBitTree<Frame> subFrames;

    /// <summary>
    /// Initializes a new empty <see cref="MultiFrame"/>.
    /// </summary>
    public MultiFrame() => subFrames = new();

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> over all the <see cref="Frame"/> instances in the <see cref="MultiFrame"/>.
    /// </summary>
    public IEnumerable<(ReadOnlyMemory<byte> Key, Frame Frame)> AllFrames => subFrames.Pairs;

    /// <summary>
    /// Creates a new sub <see cref="Frame"/> with the given <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id to map to the created <see cref="Frame"/></param>
    /// <returns>The created frame.</returns>
    public Frame Create(short id) => CreateInternal(GetKey(id));
    /// <summary>
    /// Creates a new sub <see cref="Frame"/> with the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to map to the created <see cref="Frame"/></param>
    /// <returns>The created frame.</returns>
    public Frame Create(ReadOnlyMemory<byte> key) => CreateInternal(key);
    /// <summary>
    /// Gets the <see cref="Frame"/> associated with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of the <see cref="Frame"/> to get.</param>
    /// <returns>The <see cref="Frame"/> associated with the specified <paramref name="id"/>.</returns>
    public Frame Get(short id) => Get(GetKey(id));
    /// <summary>
    /// Gets the <see cref="Frame"/> associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the <see cref="Frame"/> to get.</param>
    /// <returns>The <see cref="Frame"/> associated with the specified <paramref name="key"/>.</returns>
    /// <exception cref="InvalidOperationException">If the specified <paramref name="key"/> does not exist.</exception>
    private Frame Get(ReadOnlyMemory<byte> key) => subFrames.TryGet(key.Span, out Frame? frame) ? frame! :
        throw new InvalidOperationException($"The specified key does not exist in the {nameof(MultiFrame)}");
    /// <summary>
    /// Checks if the specified <paramref name="id"/> is present in the <see cref="MultiFrame"/>.
    /// </summary>
    /// <param name="id">The id to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="id"/> is present in the <see cref="MultiFrame"/>,
    /// <see langword="false"/> otherwise.</returns>
    public bool ContainsId(short id) => ContainsKey(GetKey(id));
    /// <summary>
    /// Checks if the specified <paramref name="key"/> is present in the <see cref="MultiFrame"/>.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns><see langword="true"/> if the specified <paramref name="key"/> is present in the <see cref="MultiFrame"/>,
    /// <see langword="false"/> otherwise.</returns>
    public bool ContainsKey(ReadOnlyMemory<byte> key) => subFrames.ContainsKey(key.Span);
    /// <summary>
    /// Removes the <see cref="Frame"/> associated with the specified <paramref name="id"/>
    /// </summary>
    /// <param name="id">The id of the <see cref="Frame"/> to remove</param>
    public void Remove(short id) => Remove(GetKey(id));
    /// <summary>
    /// Removes the <see cref="Frame"/> associated with the specified <paramref name="key"/>
    /// </summary>
    /// <param name="key">The key of the <see cref="Frame"/> to remove.</param>
    /// <exception cref="InvalidOperationException">If the specified <paramref name="key"/> does not exist.</exception>
    public void Remove(ReadOnlyMemory<byte> key) {
        if (subFrames.TryRemove(key.Span)) return;
        else throw new InvalidOperationException($"The specified key does not exist in the {nameof(MultiFrame)}");
    }
    /// <summary>
    /// Removes all <see cref="Frame"/> instances from the <see cref="MultiFrame"/>.
    /// </summary>
    public void Clear() => subFrames.Clear();

    /// <summary>
    /// Helper method to create a new sub <see cref="Frame"/> with the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to map to the created <see cref="Frame"/></param>
    /// <returns>The created frame.</returns>
    /// <exception cref="InvalidOperationException">If the given key is already taken.</exception>
    private Frame CreateInternal(ReadOnlyMemory<byte> key) {
        Frame frame = new();
        if (subFrames.TryAdd(key[..sizeof(short)], frame)) return frame;
        else throw new InvalidOperationException($"The specified key already exists in the {nameof(MultiFrame)}");
    }

    /// <summary>
    /// Static helper method which converts a <see langword="short"/> id into a <see cref="ReadOnlyMemory{T}"/> key.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>The <see cref="ReadOnlyMemory{T}"/> key representation of the <paramref name="id"/>.</returns>
    private static ReadOnlyMemory<byte> GetKey(short id) {
        byte[] key = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(key, id);
        return key;
    }
}
