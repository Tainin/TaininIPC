using TaininIPC.Client.Connections.Interface;
using TaininIPC.CritBitTree.Keys;
using TaininIPC.CritBitTree;
using System.Diagnostics.CodeAnalysis;
using TaininIPC.Utils;
using System.Diagnostics;
using TaininIPC.Client.Routing.Interface;
using TaininIPC.Data.Frames;
using TaininIPC.Client.Routing.Endpoints;
using TaininIPC.Protocol;

namespace TaininIPC.Client.Connections;

/// <summary>
/// Represents a read only set of connection sources for use in a <see cref="Node"/>.
/// </summary>
public sealed class ConnectionSourceSet : IRouter {

    private class Builder : IConnectionSourceSetBuilder, IBuiltConnectionSourceSetBuilder {

        private readonly ConnectionSourceSet connectionSourceSet;

        public ConnectionSourceSet ConnectionSourceSet => connectionSourceSet;

        public Builder() => connectionSourceSet = new();

        public IConnectionSourceSetBuilder AddConnectionSource(StringKey nameKey, Int32Key key, IConnectionSource connectionSource) {
            if (connectionSourceSet.nameMap.TryAdd(nameKey, key)) {
                if (connectionSourceSet.connectionSources.TryAdd(key, connectionSource)) return this;

                bool removed = connectionSourceSet.nameMap.TryRemove(nameKey);
                Debug.Assert(removed);
                throw new InvalidOperationException($"The specified {nameof(key)} is already in use");
            }
            throw new InvalidOperationException($"The specified {nameof(nameKey)} is already in use.");
        }

        public IBuiltConnectionSourceSetBuilder Build() => this;
    }

    /// <summary>
    /// Starts building a new <see cref="ConnectionSourceSet"/>.
    /// </summary>
    /// <returns>A new <see cref="IEmptyConnectionSourceSetBuilder"/> which can be used to build a new <see cref="ConnectionSourceSet"/>.</returns>
    public static IEmptyConnectionSourceSetBuilder New() => new Builder();

    private readonly CritBitTree<StringKey, Int32Key> nameMap;
    private readonly CritBitTree<Int32Key, IConnectionSource> connectionSources;

    private ConnectionSourceSet() => (nameMap, connectionSources) = (new(), new());

    /// <summary>
    /// Gets the <see cref="IConnectionSource"/> mapped to by the given <paramref name="nameKey"/>.
    /// </summary>
    /// <param name="nameKey">The name of the connection source to get.</param>
    /// <param name="connectionSource">Gets set to the connection source mapped to by the given 
    /// <paramref name="nameKey"/> given that it is present in the set.</param>
    /// <returns><see langword="true"/> if the given <paramref name="nameKey"/> was present in the set, 
    /// <see langword="false"/> otherwise.</returns>
    public bool TryGetConnectionSource(StringKey nameKey, [NotNullWhen(true)] out IConnectionSource? connectionSource) {
        if (nameMap.TryGet(nameKey, out Int32Key? key)) return connectionSources.TryGet(key!, out connectionSource);
        return UtilityFunctions.DefaultAndFalse(out connectionSource);
    }
    /// <summary>
    /// Gets the <see cref="IConnectionSource"/> mapped to by the given <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key of the connection source to get.</param>
    /// <param name="connectionSource">Gets set to the connection source mapped to by the given 
    /// <paramref name="key"/> given that it is present in the set.</param>
    /// <returns><see langword="true"/> if the given <paramref name="key"/> was present in the set, 
    /// <see langword="false"/> otherwise.</returns>
    public bool TryGetConnectionSource(Int32Key key, [NotNullWhen(true)] out IConnectionSource? connectionSource) =>
        connectionSources.TryGet(key, out connectionSource);

    /// <summary>
    /// Routes the specified <paramref name="frame"/> by attempting to complete a connection request
    /// through the <see cref="IConnectionSource"/> specified in it's body.
    /// </summary>
    /// <param name="frame">The frame to route.</param>
    /// <param name="_"></param>
    /// <returns>An asyncronous task representing the operation.</returns>
    public async Task RouteFrame(MultiFrame frame, EndpointTableEntry? _) {
        if (!frame.TryGet(MultiFrameKeys.CONNECTION_INFO_KEY, out Frame? subFrame)) return;

        if (TryGetConnectionSource(new Int32Key(subFrame.Get(^1)), out IConnectionSource? connectionSource))
            await connectionSource.CompleteConnectionRequest(subFrame).ConfigureAwait(false);
    }
}
