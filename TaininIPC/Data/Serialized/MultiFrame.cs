using System.Buffers.Binary;
using TaininIPC.Data.CritBitTree;

namespace TaininIPC.Data.Serialized;

public sealed class MultiFrame {

    private readonly CritBitTree<Frame> subFrames;

    public MultiFrame() => subFrames = new();

    public IEnumerable<Frame> SubFrames => subFrames.Values;
    public IEnumerable<short> Keys => 
        subFrames.Keys.Select(rom => GetId(rom.Span));
    public IEnumerable<(Frame Frame, short Key)> Pairs =>
        subFrames.Pairs.Select(pair => (pair.Value, GetId(pair.Key.Span)));

    public Frame Create(short id) {
        Frame frame = new();
        if (subFrames.TryAdd(GetKey(id), frame)) return frame;
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
    private static short GetId(ReadOnlySpan<byte> key) =>
        BinaryPrimitives.ReadInt16BigEndian(key);
}
