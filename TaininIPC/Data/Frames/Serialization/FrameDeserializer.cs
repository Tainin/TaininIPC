using System.Diagnostics.CodeAnalysis;
using TaininIPC.Data.Protocol;
using TaininIPC.Utils;

namespace TaininIPC.Data.Frames.Serialization;

/// <summary>
/// Represents the process of deserializing <see cref="MultiFrame"/> instances from a sequence of <see cref="NetworkChunk"/> instances.
/// </summary>
public sealed class FrameDeserializer {

    private enum DeserializationState : byte {
        None = 0,
        MultiFrame = 1,
        Frame = 2,
        Complete = 3,
        Error = 4,
    }

    private MultiFrame workingMultiFrame = null!;
    private Frame workingFrame = null!;
    private DeserializationState deserializationState;

    /// <summary>
    /// Initializes a new <see cref="FrameDeserializer"/>.
    /// </summary>
    public FrameDeserializer() => deserializationState = DeserializationState.None;

    /// <summary>
    /// Applies the specified <paramref name="chunk"/> to the deserialization of the current <see cref="MultiFrame"/>
    /// and sets <paramref name="frame"/> if the <see cref="MultiFrame"/> was completed.
    /// </summary>
    /// <param name="chunk">The chunk to apply.</param>
    /// <param name="frame">The completed <see cref="MultiFrame"/> if available.</param>
    /// <returns><see langword="true"/> if the <paramref name="chunk"/> completed a <see cref="MultiFrame"/>, 
    /// <see langword="false"/> otherwise.</returns>
    public bool ApplyChunk(NetworkChunk chunk, [NotNullWhen(true)] out MultiFrame? frame) {
        deserializationState = deserializationState switch {
            DeserializationState.None => ExpectStartMultiFrame(chunk),
            DeserializationState.MultiFrame => InMultiFrameHandler(chunk),
            DeserializationState.Frame => InFrameHandler(chunk),
            _ => DeserializationState.Error,
        };

        if (deserializationState is DeserializationState.Complete) {
            deserializationState = DeserializationState.None;
            frame = workingMultiFrame;
            return true;
        }

        if (deserializationState is DeserializationState.Error)
            deserializationState = DeserializationState.None;

        return UtilityFunctions.DefaultAndFalse(out frame);
    }

    private DeserializationState ExpectStartMultiFrame(NetworkChunk chunk) {
        FrameInstruction instruction = (FrameInstruction)chunk.Instruction;

        if (instruction is not FrameInstruction.StartMultiFrame)
            return DeserializationState.Error;

        workingMultiFrame = new();
        return DeserializationState.MultiFrame;
    }

    private DeserializationState InMultiFrameHandler(NetworkChunk chunk) {
        FrameInstruction instruction = (FrameInstruction)chunk.Instruction;

        if (instruction is FrameInstruction.EndMultiFrame)
            return DeserializationState.Complete;

        if (instruction is FrameInstruction.StartFrame)
            if (workingMultiFrame.TryCreate(new(chunk.Data), out workingFrame))
                return DeserializationState.Frame;

        return DeserializationState.Error;
    }

    private DeserializationState InFrameHandler(NetworkChunk chunk) {
        FrameInstruction instruction = (FrameInstruction)chunk.Instruction;

        if (instruction is FrameInstruction.EndFrame)
            return DeserializationState.MultiFrame;

        if (instruction is FrameInstruction.AppendBuffer) {
            workingFrame.Insert(chunk.Data, ^1);
            return DeserializationState.Frame;
        }

        return DeserializationState.Error;
    }
}
