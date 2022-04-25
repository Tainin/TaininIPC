using TaininIPC.CritBitTree.Keys;

namespace TaininIPC.Client.Connections.Interface;

/// <summary>
/// Represents a builder for a <see cref="ConnectionSourceSet"/> which does not yet contain any <see cref="IConnectionSource"/> instances.
/// </summary>
public interface IEmptyConnectionSourceSetBuilder {
    /// <summary>
    /// Adds an <see cref="IConnectionSource"/> instance to the <see cref="ConnectionSourceSet"/> being built.
    /// </summary>
    /// <param name="nameKey">The name to map to the <paramref name="connectionSource"/>.</param>
    /// <param name="key">The key to map to the <paramref name="connectionSource"/>.</param>
    /// <param name="connectionSource">The connectionSource to add to the set being built.</param>
    /// <returns>The <see cref="IConnectionSourceSetBuilder"/></returns>
    public IConnectionSourceSetBuilder AddConnectionSource(StringKey nameKey, Int32Key key, IConnectionSource connectionSource);
}

/// <summary>
/// Represents a builder for a <see cref="Connections.ConnectionSourceSet"/> which has been built.
/// </summary>
public interface IBuiltConnectionSourceSetBuilder {
    /// <summary>
    /// The built <see cref="Connections.ConnectionSourceSet"/>
    /// </summary>
    public ConnectionSourceSet ConnectionSourceSet { get; }
}

/// <summary>
/// Represents a builder for a <see cref="ConnectionSourceSet"/> which can be built.
/// </summary>
public interface IConnectionSourceSetBuilder : IEmptyConnectionSourceSetBuilder {
    /// <summary>
    /// Builds the <see cref="Connections.ConnectionSourceSet"/>.
    /// </summary>
    /// <returns>A builder as a <see cref="IBuiltConnectionSourceSetBuilder"/>.</returns>
    public IBuiltConnectionSourceSetBuilder Build();
}
