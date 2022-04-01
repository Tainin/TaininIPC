using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;

namespace TaininIPC.Data.Frames;

/// <summary>
/// Represents a mapped collection of <see cref="Frame"/> instances keyed by <see langword="short"/> ids.
/// </summary>
public sealed class MultiFrame {

    // Internal map of Frames
    private readonly CritBitTree<Int16Key, Frame> subFrames;

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> over all the <see cref="Frame"/> instances in the <see cref="MultiFrame"/>.
    /// </summary>
    public IEnumerable<(Int16Key, Frame Frame)> AllFrames => subFrames.Pairs;

    /// <summary>
    /// Initializes a new empty <see cref="MultiFrame"/>.
    /// </summary>
    public MultiFrame() => subFrames = new();

    /// <summary>
    /// Creates a new sub <see cref="Frame"/> and attemtps to add it to the <see cref="MultiFrame"/> with the
    /// given <paramref name="key"/>
    /// </summary>
    /// <param name="key">The key to map to the created <see cref="Frame"/>.</param>
    /// <param name="frame">Contains the created <see cref="Frame"/> on return.</param>
    /// <returns><see langword="true"/> if the created <paramref name="frame"/> could be added 
    /// to the <see cref="MultiFrame"/>, false if the <paramref name="key"/> already exists.</returns>
    public bool TryCreate(Int16Key key, out Frame frame) => subFrames.TryAdd(key, frame = new());
    /// <summary>
    /// Sets the <see cref="Frame"/> mapped to by the given <paramref name="key"/>. If the <paramref name="key"/> already maps
    /// to a frame it is replaced.
    /// </summary>
    /// <param name="key">The key which maps to the frame to set.</param>
    /// <param name="frame">The frame to set.</param>
    public void Set(Int16Key key, Frame frame) {
        if (subFrames.TryAdd(key, frame)) return;
        bool updated = subFrames.TryUpdate(key, frame);
        Debug.Assert(updated);
    }
    /// <summary>
    /// Attempts to get the <see cref="Frame"/> mapped to by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the <see cref="Frame"/> to get.</param>
    /// <param name="frame">Contains the <see cref="Frame"/> mapped to by the <paramref name="key"/> on return if it exists.</param>
    /// <returns><see langword="true"/> if the <paramref name="key"/> exists, <see langword="false"/> otherwise.</returns>
    public bool TryGet(Int16Key key, [NotNullWhen(true)] out Frame? frame) => subFrames.TryGet(key, out frame);
    /// <summary>
    /// Checks if the given <paramref name="key"/> exists in the <see cref="MultiFrame"/>.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns><see langword="true"/> if the <paramref name="key"/> exists, <see langword="false"/> otherwise.</returns>
    public bool ContainsKey(Int16Key key) => subFrames.ContainsKey(key);
    /// <summary>
    /// Attempts to pop (get and remove) the <see cref="Frame"/> mapped to by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the <see cref="Frame"/> to pop.</param>
    /// <param name="frame">Contains the <see cref="Frame"/> mapped to by the given <paramref name="key"/> on return if it exists.</param>
    /// <returns><see langword="true"/> if the <paramref name="key"/> exists, <see langword="false"/> otherwise.</returns>
    public bool TryPop(Int16Key key, [NotNullWhen(true)] out Frame? frame) => subFrames.TryPop(key, out frame);
    /// <summary>
    /// Attempts to remove the <see cref="Frame"/> mapped to by the given <paramref name="key"/> from the <see cref="MultiFrame"/>.
    /// </summary>
    /// <param name="key">The <paramref name="key"/> of the <see cref="Frame"/> to remove.</param>
    /// <returns><see langword="true"/> if the <paramref name="key"/> existed in the table 
    /// and was removed, <see langword="false"/> otherwise.</returns>
    public bool TryRemove(Int16Key key) => subFrames.TryRemove(key);
    /// <summary>
    /// Removes all <see cref="Frame"/> instances from the <see cref="MultiFrame"/>.
    /// </summary>
    public void Clear() => subFrames.Clear();
}
/// <summary>
/// Provides extension methods for <see cref="MultiFrame"/>.
/// </summary>
public static class MultiFrameExtensions {
    /// <summary>
    /// Attempts to pop the <see cref="Frame"/> mapped to by <paramref name="from"/> and map <paramref name="to"/> to it.
    /// </summary>
    /// <param name="multiFrame">The multiframe to operate on.</param>
    /// <param name="from">The key of the <see cref="Frame"/> to pop.</param>
    /// <param name="to">The key to map to the popped <see cref="Frame"/>.</param>
    /// <returns></returns>
    /// <remarks>
    /// This removes <paramref name="from"/> from the <see cref="MultiFrame"/> and replaces the <see cref="Frame"/> mapped to by <paramref name="to"/>.
    /// </remarks>
    public static bool TrySwapOver(this MultiFrame multiFrame, Int16Key from, Int16Key to) {
        if (!multiFrame.TryPop(from, out Frame? frame)) return false;
        multiFrame.Set(to, frame);
        return true;
    }
}
