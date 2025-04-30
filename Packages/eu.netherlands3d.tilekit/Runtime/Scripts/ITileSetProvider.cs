namespace Netherlands3D.Tilekit
{
    /// <summary>
    /// The ITileSetProvider is an interface that declares that an object, usually a MonoBehaviour, is responsible for
    /// loading and providing a TileSet.
    ///
    /// It is also used as the means to get the TileSet identifier before the TileSet is actually loaded, since the
    /// identifier is used as a contract between services to know if they are handling the same entity, without having
    /// that entity available.
    ///
    /// As an example, the EventBus relies on this identifier to select the correct EventChannel on which messages
    /// pertaining to this TileSet are emitted.
    /// </summary>
    public interface ITileSetProvider
    {
        /// <summary>
        /// The identifier for the given TileSet.
        ///
        /// This must be available before the TileSet is loaded and before "Start" methods happen in MonoBehaviour; this
        /// identifier is _prescriptive_ and as such acts as a contract. It is recommended to use a UUID as a value
        /// for this identifier, but services could also use other strings that are predictable (such as the endpoint
        /// URI in case of remote services).
        ///
        /// This is similar to Correlation Ids (https://microsoft.github.io/code-with-engineering-playbook/observability/correlation-id/)
        /// that identify all messages belonging to a single transaction, in this case we correlate all messaging to a
        /// stream.
        /// </summary>
        public string TileSetId { get; }
        
        /// <summary>
        /// The actual TileSet that was loaded by this provider.
        ///
        /// This value can be null until the loading is actually done, in the meantime dependent services can use
        /// the TileSetId instead.
        /// </summary>
        public TileSet? TileSet { get; }
    }
}