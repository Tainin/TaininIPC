namespace TaininIPC.Utils;

public static class UtilityFunctions {
    public static bool DefaultAndFalse<T>(out T? defaultValue) {
        defaultValue = default;
        return false;
    }
}
