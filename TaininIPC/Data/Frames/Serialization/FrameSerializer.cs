using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Protocol;

namespace TaininIPC.Data.Frames.Serialization;

/// <summary>
/// Provides serialization fuctions to transform <see cref="MultiFrame"/> instances to <see cref="NetworkChunk"/> instances.
/// </summary>
public static class FrameSerializer {
    /// <summary>
    /// Static helper function which transforms the specified <paramref name="multiFrame"/> into an <see cref="IEnumerable{T}"/> of 
    /// <see cref="NetworkChunk"/> instances.
    /// </summary>
    /// <param name="multiFrame">The frame to transform.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="NetworkChunk"/> instances
    /// which can be used to rebuild the specified <paramref name="multiFrame"/>.</returns>
    public static IEnumerable<NetworkChunk> SerializeMultiFrame(MultiFrame multiFrame) {
        yield return FrameChunks.StartMultiFrame;
        foreach ((Int16Key key, Frame frame) in multiFrame.AllFrames) {
            yield return FrameChunks.StartFrame(key.Memory);
            foreach (ReadOnlyMemory<byte> buffer in frame.AllBuffers)
                yield return FrameChunks.AppendBuffer(buffer);
            yield return FrameChunks.EndFrame;
        }
        yield return FrameChunks.EndMultiFrame;
    }
}
