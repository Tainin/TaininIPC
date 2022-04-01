using System.Diagnostics;
using TaininIPC.CritBitTree;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Utils;

namespace TaininIPC.Client.Table;

/// <summary>
/// Represents a thread-safe mapping between <see cref="StringKey"/> names and <see cref="Int32Key"/> keys 
/// for use in conjunction with <see cref="Table{T}"/>.
/// </summary>
public sealed class NameMap {

    /// <summary>
    /// Represents an associated name and key in a <see cref="NameMap"/>.
    /// </summary>
    public sealed class Pair {
        /// <summary>
        /// The name in the association
        /// </summary>
        public StringKey Name { get; }
        /// <summary>
        /// The key in the association
        /// </summary>
        public Int32Key Key { get; }

        /// <summary>
        /// Initializes a new <see cref="Pair"/> from it's <paramref name="name"/> and <paramref name="key"/> components.
        /// </summary>
        /// <param name="name">The name component for the pair.</param>
        /// <param name="key">The key component for the pair.</param>
        public Pair(StringKey name, Int32Key key) => (Name, Key) = (name, key);
    }

    private readonly CritBitTree<StringKey, Pair> nameMap;
    private readonly CritBitTree<Int32Key, Pair> keyMap;
    private readonly SemaphoreSlim syncSemaphore;

    /// <summary>
    /// Initializes a new <see cref="NameMap"/>.
    /// </summary>
    public NameMap() => (nameMap, keyMap, syncSemaphore) = (new(), new(), new(1, 1));

    /// <summary>
    /// Attempts to map the specified <paramref name="name"/> to the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="name">The name to map to the key.</param>
    /// <param name="key">The key the name should map to.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the mapping was added,
    /// and <see langword="false"/> if either the <paramref name="name"/> or <paramref name="key"/> aready exists in the map.</returns>
    public async Task<bool> TryMap(StringKey name, Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool added = false;
        Pair pair = new(name, key);
        if (nameMap.TryAdd(name, pair)) {
            if (keyMap.TryAdd(key, pair)) added = true;
            else {
                bool removed = nameMap.TryRemove(name);
                Debug.Assert(removed);
            }
        }
        syncSemaphore.Release();
        return added;
    }

    /// <summary>
    /// Attempts to get the <see cref="Pair"/> associated with the given <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name to get the pair associated with..</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> representing the associated pair if the
    /// <paramref name="name"/> was found in the map.</returns>
    public async Task<Attempt<Pair>> TryGet(StringKey name) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        Attempt<Pair> attempt = nameMap.TryGet(name, out Pair? pair) ? new(pair!) : Attempt<Pair>.Failed;
        syncSemaphore.Release();
        return attempt;
    }

    /// <summary>
    /// Attempts to get the <see cref="Pair"/> associated with the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to get the pair associated with..</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> representing the associated pair if the
    /// <paramref name="key"/> was found in the map.</returns>
    public async Task<Attempt<Pair>> TryGet(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        Attempt<Pair> attempt = keyMap.TryGet(key, out Pair? pair) ? new(pair!) : Attempt<Pair>.Failed;
        syncSemaphore.Release();
        return attempt;
    }
    /// <summary>
    /// Determines if the given <paramref name="name"/> exists in the map.
    /// </summary>
    /// <param name="name">The name to search for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="name"/> was found,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> ContainsName(StringKey name) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool found = nameMap.ContainsKey(name);
        syncSemaphore.Release();
        return found;
    }
    /// <summary>
    /// Determines if the given <paramref name="key"/> exists in the map.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="key"/> was found,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> ContainsKey(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool found = keyMap.ContainsKey(key);
        syncSemaphore.Release();
        return found;
    }
    /// <summary>
    /// Attempts to remove the given <paramref name="name"/> and it's associated key from the map.
    /// </summary>
    /// <param name="name">The name to remove.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="name"/> was found and removed,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> TryRemove(StringKey name) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool removed = nameMap.TryGet(name, out Pair? pair) && TryRemove(pair!);
        syncSemaphore.Release();
        return removed;
    }
    /// <summary>
    /// Attempts to remove the given <paramref name="key"/> and it's associated name from the map.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the <paramref name="key"/> was found and removed,
    /// <see langword="false"/> otherwise.</returns>
    public async Task<bool> TryRemove(Int32Key key) {
        await syncSemaphore.WaitAsync().ConfigureAwait(false);
        bool removed = keyMap.TryGet(key, out Pair? pair) && TryRemove(pair!);
        syncSemaphore.Release();
        return removed;
    }

    private bool TryRemove(Pair pair) => nameMap.TryRemove(pair.Name) && keyMap.TryRemove(pair.Key);
}
