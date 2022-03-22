using TaininIPC.CritBitTree.Keys;
using TaininIPC.Utils;

namespace TaininIPC.Client.Interface;

/// <summary>
/// Represents a associative array like table mapping <see cref="Int32Key"/>s to <typeparamref name="TStored"/> instances.
/// </summary>
/// <typeparam name="TInput">The type of the objects used when adding to the table.</typeparam>
/// <typeparam name="TStored">The type of the objects stored in the table.</typeparam>
public interface ITable<TInput, TStored> where TInput : notnull where TStored : notnull {
    /// <summary>
    /// Gets the number of reserved ids in the table.
    /// </summary>
    public int ReservedCount { get; }
    /// <summary>
    /// Adds the specified <paramref name="input"/> to the table with an automatically assigned key.
    /// </summary>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task which completes with the key assigned to the entry.</returns>
    public Task<Int32Key> Add(TInput input);
    /// <summary>
    /// Adds the specified <paramref name="input"/> to the table with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to map to the specified <paramref name="input"/></param>
    /// <param name="input">The entry to add to the table.</param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public Task AddReserved(Int32Key key, TInput input);
    /// <summary>
    /// Removes all entries from the table.
    /// </summary>
    /// <returns>An asyncronous task representing the operation.</returns>
    public Task Clear();
    /// <summary>
    /// Attempts to get the entry mapped to by the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key which maps to the entry to get.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> representing the entry mapped to by
    /// the <paramref name="key"/> if it exists.</returns>
    public Task<Attempt<TStored>> TryGet(Int32Key key);
    /// <summary>
    /// Checks if the table contains the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to check for.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the table contains the <paramref name="key"/>,
    /// <see langword="false"/> otherwise.</returns>
    public Task<bool> Contains(Int32Key key);
    /// <summary>
    /// Attempts to remove the entry mapped to by the specifed <paramref name="key"/> from the table.
    /// </summary>
    /// <param name="key">The key which maps to the entry to remove.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the entry was removed, 
    /// and <see langword="false"/> if the <paramref name="key"/> could not be found.</returns>
    public Task<bool> TryRemove(Int32Key key);
}