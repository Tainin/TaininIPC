using System.Diagnostics.CodeAnalysis;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.Data.Frames;
using TaininIPC.Utils;

namespace TaininIPC.Protocol;

/// <summary>
/// Extension methods for accessing the response identifier of a <see cref="MultiFrame"/>.
/// </summary>
public static class ResponseIdentifier {
    /// <summary>
    /// Attempts to get the response identifier of the specified <paramref name="frame"/>.
    /// </summary>
    /// <param name="frame">The frame to get the response identifier from.</param>
    /// <param name="responseIdentifier">The response identifier of the <paramref name="frame"/>, if it contained one.</param>
    /// <returns><see langword="true"/> if the frame contained a response identifier, <see langword="false"/> otherwise.</returns>
    public static bool TryGetResponseIdentifier(this MultiFrame frame, [NotNullWhen(true)] out BasicKey? responseIdentifier) {
        if (!frame.TryGet(MultiFrameKeys.RESPONSE_IDENTIFIER_KEY, out Frame? subFrame)) 
            return UtilityFunctions.DefaultAndFalse(out responseIdentifier);
        responseIdentifier = new(subFrame.Get(0));
        return true;
    }
    /// <summary>
    /// Sets the response identifier of the specified <paramref name="frame"/> to the specified <paramref name="responseIdentifier"/>.
    /// </summary>
    /// <param name="frame">The frame to set the <paramref name="responseIdentifier"/> into.</param>
    /// <param name="responseIdentifier">The response identifier to set into the <paramref name="frame"/>.</param>
    public static void SetResponseIdentifier(this MultiFrame frame, BasicKey responseIdentifier) {
        if (!frame.TryGet(MultiFrameKeys.RESPONSE_IDENTIFIER_KEY, out Frame? subFrame))
            frame.TryCreate(MultiFrameKeys.RESPONSE_IDENTIFIER_KEY, out subFrame);

        if (subFrame.Length < 1) subFrame.Prepend(responseIdentifier.Memory);
        else subFrame.Swap(0, responseIdentifier.Memory);
    }
}
