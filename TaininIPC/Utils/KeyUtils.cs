using System.Buffers.Binary;

namespace TaininIPC.Utils;

/// <summary>
/// Static class containing utility functions for converting integer primative ids to <see cref="ReadOnlyMemory{T}"/> 
/// (where <typeparamref name="T"/> is <see langword="byte"/>) keys.
/// </summary>
public static class KeyUtils {
    /// <summary>
    /// Utility function which converts a <see langword="int"/> <paramref name="id"/> into a key.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>The key representation of the <paramref name="id"/>.</returns>
    public static ReadOnlyMemory<byte> GetKey(int id) {
        byte[] key = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(key, id);
        return key;
    }

    /// <summary>
    /// Utility function which converts a <see langword="short"/> <paramref name="id"/> into a key.
    /// </summary>
    /// <param name="id">The id to convert.</param>
    /// <returns>The key representation of the <paramref name="id"/>.</returns>
    public static ReadOnlyMemory<byte> GetKey(short id) {
        byte[] key = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt32BigEndian(key, id);
        return key;
    }
}
