namespace TaininIPC.Utils;

/// <summary>
/// Represents a potential result in <see cref="Task"/> based <c>Try(Something)</c> style methods where <see langword="out"/>
/// parameters are not permitted.
/// </summary>
/// <typeparam name="T">The type of the result.</typeparam>
public sealed class Attempt<T> where T : notnull {

    /// <summary>
    /// Represents a failed <see cref="Attempt{T}"/>
    /// </summary>
    public static Attempt<T> Failed { get; } = new Attempt<T>();

    /// <summary>
    /// The result of the operation if it was successful.
    /// </summary>
    public T? Result { get; }
    /// <summary>
    /// Indicates whether or not there is a result.
    /// </summary>
    public bool HasResult { get; } = false;

    /// <summary>
    /// Initializes a new <see cref="Attempt{T}"/> without a result.
    /// </summary>
    public Attempt() { }
    /// <summary>
    /// Initializes a new <see cref="Attempt{T}"/> given the <paramref name="result"/> of the attempt.
    /// </summary>
    /// <param name="result">The result of the attempt.</param>
    public Attempt(T? result) => (Result, HasResult) = (result, true);

    /// <summary>
    /// Attempts to get the result of the <see cref="Attempt{T}"/>.
    /// </summary>
    /// <param name="result">Contains the <see cref="Result"/> on return if the <see cref="Attempt{T}"/> was initialized with one.
    /// Otherwise is undefined.</param>
    /// <returns><see langword="true"/> if the <see cref="Attempt{T}"/> was initialized with a result, 
    /// <see langword="false"/> otherwise.</returns>
    public bool TryResult(out T? result) {
        if (!HasResult) return UtilityFunctions.DefaultAndFalse(out result);
        result = Result;
        return true;
    }

    /// <summary>
    /// Defines an implicit conversion of a <typeparamref name="T"/> to an <see cref="Attempt{T}"/> 
    /// where <see cref="T"/> is <typeparamref name="T"/>
    /// </summary>
    /// <param name="result"></param>
    public static implicit operator Attempt<T>(T? result) => new(result);
}

public static class AttemptExtensions {
    /// <summary>
    /// Converts a <see langword="bool"/> and a <typeparamref name="T"/> to an <see cref="Attempt{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="hasResult">A flag indicating whether or not there is a result.</param>
    /// <param name="result">The result.</param>
    /// <returns>The <see cref="Attempt{T}"/> of <paramref name="hasResult"/> and <paramref name="result"/>.</returns>
    public static Attempt<T> ToAttempt<T>(this bool hasResult, T? result) where T : notnull {
        if (hasResult) return result;
        return Attempt<T>.Failed;
    }
}
