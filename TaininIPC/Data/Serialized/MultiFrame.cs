using System.Buffers.Binary;
using TaininIPC.Data.CritBitTree;

namespace TaininIPC.Data.Serialized;

public sealed class MultiFrame {

    private readonly CritBitTree<Frame> subFrames;

    public MultiFrame() => subFrames = new(); 

    public IEnumerable<Frame> SubFrames => subFrames.Values;
    public IEnumerable<short> Keys => 
        subFrames.Keys.Select(rom => BinaryPrimitives.ReadInt16BigEndian(rom.Span));
    public IEnumerable<(Frame Frame, short Key)> Pairs =>
        subFrames.Pairs.Select(pair => (pair.Value, BinaryPrimitives.ReadInt16BigEndian(pair.Key.Span)));

    public Frame Create(short id) {
        Frame frame = new();
        if (subFrames.TryAdd(GetKeyBuffer(id), frame)) return frame;
        else throw new InvalidOperationException();
    }
    public Frame Get(short id) => subFrames.TryGet(GetKeyBuffer(id), out Frame? frame) ? frame! : 
        throw new InvalidOperationException();
    public bool ContainsKey(short id) => subFrames.ContainsKey(GetKeyBuffer(id));
    public void Remove(short id) {
        if (subFrames.TryRemove(GetKeyBuffer(id))) return;
        else throw new InvalidOperationException();
    }
    public void Clear() => subFrames.Clear();

    private static byte[] GetKeyBuffer(short id) {
        byte[] key = new byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(key, id);
        return key;
    }
}
