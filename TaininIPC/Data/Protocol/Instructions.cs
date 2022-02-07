namespace TaininIPC.Data.Protocol;

public static class Instructions {
    public static readonly byte StartMultiFrame = 0x00;
    public static readonly byte EndMultiFrame = 0x01;
    public static readonly byte StartFrame = 0x02;
    public static readonly byte EndFrame = 0x03;
    public static readonly byte AppendBuffer = 0x04;
}
