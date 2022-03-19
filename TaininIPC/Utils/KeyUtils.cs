using System.Buffers.Binary;
using System.Text;

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
        BinaryPrimitives.WriteInt16BigEndian(key, id);
        return key;
    }

    /// <summary>
    /// Utility function which converts the given <paramref name="name"/> into a key.
    /// </summary>
    /// <param name="name">The name to convert.</param>
    /// <returns>The key representation of the <paramref name="name"/>.</returns>
    public static ReadOnlyMemory<byte> GetKey(string name) => Encoding.BigEndianUnicode.GetBytes(name);

    /// <summary>
    /// Utility function which converts the given <paramref name="key"/> back into it's <see langword="int"/> id form.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The id form of the given <paramref name="key"/>.</returns>
    public static int GetIntId(ReadOnlyMemory<byte> key) => BinaryPrimitives.ReadInt32BigEndian(key.Span);

    /// <summary>
    /// Utility function which converts the given <paramref name="key"/> back into it's <see langword="short"/> id form.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The id form of the given <paramref name="key"/>.</returns>
    public static short GetShortId(ReadOnlyMemory<byte> key) => BinaryPrimitives.ReadInt16BigEndian(key.Span);

    /// <summary>
    /// Utility function which converts the given <paramref name="key"/> back into it's <see langword="string"/> name form.
    /// </summary>
    /// <param name="key">The key to convert.</param>
    /// <returns>The name form of the given <paramref name="key"/></returns>
    public static string GetName(ReadOnlyMemory<byte> key) => Encoding.BigEndianUnicode.GetString(key.Span);
}
