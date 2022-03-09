namespace TaininIPC.Data.Serialized;

public enum FrameInstruction : byte {
    StartMultiFrame = 1,
    EndMultiFrame = 2,
    StartFrame = 3,
    EndFrame = 4,
    AppendBuffer = 5,
}
