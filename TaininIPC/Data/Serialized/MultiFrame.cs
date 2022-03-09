using System.Buffers.Binary;
using TaininIPC.Utils;

namespace TaininIPC.Data.Serialized;

public sealed class MultiFrame {

    private readonly CritBitTree<Frame> subFrames;

    public MultiFrame() => subFrames = new();

    public IEnumerable<(ReadOnlyMemory<byte> Key, Frame Frame)> AllFrames => subFrames.Pairs;

    public Frame Create(short id) {
        Frame frame = new();
        if (subFrames.TryAdd(GetKey(id), frame)) return frame;
        else throw new InvalidOperationException();
    }
    public Frame Create(ReadOnlyMemory<byte> key) {
        Frame frame = new();
        if (subFrames.TryAdd(key[..sizeof(short)], frame)) return frame;
        else throw new InvalidOperationException();
    }
    public Frame Get(short id) => subFrames.TryGet(GetKey(id).Span, out Frame? frame) ? frame! : 
        throw new InvalidOperationException();
    public bool ContainsKey(short id) => subFrames.ContainsKey(GetKey(id).Span);
    public void Remove(short id) {
        if (subFrames.TryRemove(GetKey(id).Span)) return;
        else throw new InvalidOperationException();
    }
    public void Clear() => subFrames.Clear();

    private static ReadOnlyMemory<byte> GetKey(short id) {
        byte[] key = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(key, id);
        return key;
    }
}
