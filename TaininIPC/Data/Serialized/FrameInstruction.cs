namespace TaininIPC.Data.Serialized;

using Data.Protocol;

/// <summary>
/// Specifies the instruction of a <see cref="NetworkChunk"/> representing the structure of a <see cref="MultiFrame"/>
/// </summary>
public enum FrameInstruction : byte {
    StartMultiFrame = 1,
    EndMultiFrame = 2,
    StartFrame = 3,
    EndFrame = 4,
    AppendBuffer = 5,
}
