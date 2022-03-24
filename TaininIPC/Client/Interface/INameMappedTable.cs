using TaininIPC.CritBitTree.Keys;
using TaininIPC.Utils;

namespace TaininIPC.Client.Interface;

/// <summary>
/// Represents an <see cref="ITable{TInput, TStored}"/> with an additional layer of mapping between <see langword="string"/> names and 
/// the <see cref="Int32Key"/>s of the <see cref="ITable{TInput, TStored}"/>.
/// </summary>
/// <typeparam name="TInput"><inheritdoc cref="ITable{TInput, TStored}"/></typeparam>
/// <typeparam name="TStored"><inheritdoc cref="ITable{TInput, TStored}"/></typeparam>
public interface INameMappedTable<TInput, TStored> : ITable<TInput, TStored> where TInput : notnull where TStored : notnull {
    /// <summary>
    /// Attempts to map the given <paramref name="nameKey"/> to the given <paramref name="key"/>.
    /// </summary>
    /// <param name="nameKey">The name to map to the <paramref name="key"/></param>
    /// <param name="key">The key to map the <paramref name="nameKey"/> to.</param>
    /// <returns>An asyncronous task whcich completes with <see langword="true"/> if the <paramref name="nameKey"/> could be mapped,
    /// and <see langword="false"/> if the <paramref name="key"/> does not exist in the table.</returns>
    public Task<bool> TryAddName(StringKey nameKey, Int32Key key);
    /// <summary>
    /// Attempts to remove the name mapping from <paramref name="nameKey"/> to it's key.
    /// </summary>
    /// <param name="nameKey">The name to un-map.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> representing the key which <paramref name="nameKey"/>
    /// had mapped to, if it was removed.</returns>
    public Task<Attempt<Int32Key>> TryRemoveName(StringKey nameKey);
    /// <summary>
    /// Attempts to remove the name mapping to <paramref name="key"/> from it's <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to un-map.</param>
    /// <returns>An asyncronous task which completes with <see langword="true"/> if the name mapping could be removed, 
    /// and <see langword="false"/> if <paramref name="key"/> does not exist in the table.</returns>
    public Task<bool> TryRemoveName(Int32Key key);
    /// <summary>
    /// Attempts to get the key mapped to by <paramref name="nameKey"/>.
    /// </summary>
    /// <param name="nameKey">The name to get the key for.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> representing the key mapped to by <paramref name="nameKey"/>,
    /// given that it exists.</returns>
    public Task<Attempt<Int32Key>> TryGetKey(StringKey nameKey);
    /// <summary>
    /// Attempts to get the name which maps to <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key to get the name which maps to.</param>
    /// <returns>An asyncronous task which completes with an <see cref="Attempt{T}"/> representing the name which maps to <paramref name="key"/>,
    /// given that it exists.</returns>
    public Task<Attempt<StringKey>> TryGetNameKey(Int32Key key);
}
