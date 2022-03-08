namespace TaininIPC.Utils;

/// <summary>
/// Static class containing general utility functions.
/// </summary>
public static class UtilityFunctions {
    /// <summary>
    /// Utility function which sets <paramref name="defaultValue"/> to the default value of <typeparamref name="T"/> and returns false.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="defaultValue"/>.</typeparam>
    /// <param name="defaultValue">Out parameter which will be set to the default value of <typeparamref name="T"/>.</param>
    /// <returns><c>false</c></returns>
    public static bool DefaultAndFalse<T>(out T? defaultValue) {
        defaultValue = default;
        return false;
    }
}
