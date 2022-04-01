using TaininIPC.CritBitTree.Interface;

namespace TaininIPC.CritBitTree.Keys;

/// <summary>
/// A simple implementation of <see cref="ICritBitKey"/>
/// </summary>
public sealed class BasicKey : ICritBitKey {
    /// <inheritdoc cref="ICritBitKey.Memory"/>
    public ReadOnlyMemory<byte> Memory { get; }

    /// <summary>
    /// Initializes a <see cref="BasicKey"/> from a region of memory.
    /// </summary>
    /// <param name="memory"></param>
    public BasicKey(ReadOnlyMemory<byte> memory) => Memory = memory;
}
