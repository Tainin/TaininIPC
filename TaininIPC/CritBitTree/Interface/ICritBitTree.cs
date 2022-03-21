
namespace TaininIPC.CritBitTree.Interface;

/// <summary>
/// Represents the associative array interface of <see cref="CritBitTree{TKey, TValue}"/>
/// </summary>
/// <typeparam name="TKey">The type of the key to use in the tree.</typeparam>
/// <typeparam name="TValue">The type of data to be stored in the tree.</typeparam>
public interface ICritBitTree<TKey, TValue> where TKey : ICritBitKey {
    /// <summary>
    /// Gets an enumerable over all of the keys in the tree in left to right depth first order.
    /// </summary>
    public IEnumerable<TKey> Keys { get; }
    /// <summary>
    /// Gets an enumerable over all of the key value pairs in the tree in left to right depth first order.
    /// </summary>
    public IEnumerable<(TKey Key, TValue Value)> Pairs { get; }
    /// <summary>
    /// Gets an enumerable over all of the values in the tree in left to right depth first order.
    /// </summary>
    public IEnumerable<TValue> Values { get; }

    /// <summary>
    /// Clears all contents of the tree.
    /// </summary>
    public void Clear();
    /// <summary>
    /// Checks whether the specified <paramref name="key"/> is present in the tree.
    /// </summary>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if <paramref name="key"/> is present in the tree. <see langword="false"/> otherwise.</returns>
    public bool ContainsKey(TKey key);
    /// <summary>
    /// Attempts to add the specified key and value to the tree.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><see langword="false"/> if <paramref name="key"/> was already present in the tree. <see langword="true"/> otherwise.</returns>
    public bool TryAdd(TKey key, TValue value);
    /// <summary>
    /// Attempts to retrieve the value associated with the specified <paramref name="key"/> 
    /// from the tree and copy it to the <paramref name="value"/> parameter
    /// </summary>
    /// <param name="key">The key associated with the value to get.</param>
    /// <param name="value">If the <paramref name="key"/> was found, contains the associated value on return.
    /// Otherwise, the default value of the type of <paramref name="value"/> parameter.</param>
    /// <returns><see langword="false"/> if <paramref name="key"/> was not found in the tree. <see langword="true"/> otherwise</returns>
    public bool TryGet(TKey key, out TValue? value);
    /// <summary>
    /// Attempts to remove an element from the tree and copy it's <c>Value</c> to the <paramref name="value"/> parameter.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">If present, the <c>Value</c> associated with the given <paramref name="key"/>.
    /// Otherwise the default value of <c>T</c>.</param>
    /// <returns><see langword="false"/> if <paramref name="key"/> was not found in the tree. <see langword="true"/> otherwise.</returns>
    public bool TryPop(TKey key, out TValue? value);
    /// <summary>
    /// Attempts to remove an element from the tree.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns><see langword="false"/> if <paramref name="key"/> was not found in the tree. <see langword="true"/> otherwise.</returns>
    public bool TryRemove(TKey key);
    /// <summary>
    /// Attempts to change the value associated with <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key which should have it's value updated.</param>
    /// <param name="newValue">The new value.</param>
    /// <returns><see langword="false"/> if <paramref name="key"/> was not found in the tree. <see langword="true"/> otherwise.</returns>
    public bool TryUpdate(TKey key, TValue newValue);
}