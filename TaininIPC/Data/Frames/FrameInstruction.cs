using TaininIPC.Data.Network;

namespace TaininIPC.Data.Frames;

/// <summary>
/// Specifies the instruction of a <see cref="NetworkChunk"/> representing the structure of a <see cref="MultiFrame"/>
/// </summary>
public enum FrameInstruction : byte {
    /// <summary>
    /// Represents the start of a <see cref="MultiFrame"/> in serialized form.
    /// </summary>
    StartMultiFrame = 1,
    /// <summary>
    /// Represents the end of a <see cref="MultiFrame"/> in serialized form.
    /// </summary>
    EndMultiFrame = 2,
    /// <summary>
    /// Represents the start of a <see cref="Frame"/> in serialized form.
    /// </summary>
    StartFrame = 3,
    /// <summary>
    /// Represents the end of a <see cref="Frame"/> in serialized form.
    /// </summary>
    EndFrame = 4,
    /// <summary>
    /// Represents the start of a data buffer in serialized form.
    /// </summary>
    AppendBuffer = 5,
}
