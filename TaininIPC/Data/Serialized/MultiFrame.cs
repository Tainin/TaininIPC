using System.Buffers.Binary;
using TaininIPC.Utils;

namespace TaininIPC.Data.Serialized;

public sealed class MultiFrame {

    private readonly CritBitTree<Frame> subFrames;

    public MultiFrame() => subFrames = new();

    public IEnumerable<(ReadOnlyMemory<byte> Key, Frame Frame)> AllFrames => subFrames.Pairs;

    public Frame Create(short id) => CreateInternal(GetKey(id));
    public Frame Create(ReadOnlyMemory<byte> key) => CreateInternal(key);
    public Frame Get(short id) => Get(GetKey(id));
    private Frame Get(ReadOnlyMemory<byte> key) => subFrames.TryGet(key.Span, out Frame? frame) ? frame! :
        throw new InvalidOperationException();
    public bool ContainsId(short id) => ContainsKey(GetKey(id));
    public bool ContainsKey(ReadOnlyMemory<byte> key) => subFrames.ContainsKey(key.Span);
    public void Remove(short id) => Remove(GetKey(id));
    public void Remove(ReadOnlyMemory<byte> key) {
        if (subFrames.TryRemove(key.Span)) return;
        else throw new InvalidOperationException();
    }
    public void Clear() => subFrames.Clear();
    private Frame CreateInternal(ReadOnlyMemory<byte> key) {
        Frame frame = new();
        if (subFrames.TryAdd(key[..sizeof(short)], frame)) return frame;
        else throw new InvalidOperationException();
    }

    private static ReadOnlyMemory<byte> GetKey(short id) {
        byte[] key = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(key, id);
        return key;
    }
}
