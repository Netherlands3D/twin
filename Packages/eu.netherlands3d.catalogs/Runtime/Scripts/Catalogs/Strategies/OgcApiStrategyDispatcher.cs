using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.OgcApi;
using Netherlands3D.OgcApi.Features;

namespace Netherlands3D.Catalogs.Catalogs.Strategies
{
    /// <summary>
    /// A composite record-parsing strategy that dispatches to the most appropriate
    /// <see cref="OgcApiRecordsStrategy"/> implementation based on heuristics applied to each record.
    ///
    /// <para>
    /// In OGC API - Records catalogs, records may originate from different backends or profiles, such as:
    /// - PyCSW
    /// - GeoNetwork
    /// - CKAN with an OGC plugin
    /// - Native implementations of the OGC API - Records specification
    /// </para>
    ///
    /// <para>
    /// While a single catalog (endpoint) may expose records through a common API surface,
    /// the actual structure and semantics of those records can vary between implementations, and even within the
    /// same collection. Therefore, <b>record parsing strategies must often be selected dynamically, on a per-feature
    /// basis</b>.
    /// </para>
    ///
    /// <para>
    /// <see cref="OgcApiStrategyDispatcher"/> encapsulates this runtime selection logic. It contains a list of
    /// registered <see cref="OgcApiRecordsStrategy"/> instances, each responsible for handling a specific variant of
    /// the standard.
    /// 
    /// When <see cref="ParseFeature"/> is called, the dispatcher:
    /// <list type="number">
    ///   <item>Evaluates each registered strategy's <see cref="OgcApiRecordsStrategy.CanHandle"/> method</item>
    ///   <item>Invokes <see cref="OgcApiRecordsStrategy.ParseFeature"/> on the first strategy that returns true</item>
    ///   <item>Throws an exception if no matching strategy is found</item>
    /// </list>
    /// </para>
    ///
    /// <para>
    /// This allows the system to:
    /// <list type="bullet">
    ///   <item>Support multiple OGC API Records variants without hardcoding assumptions</item>
    ///   <item>Gracefully handle mixed content catalogs where different profiles coexist</item>
    ///   <item>Extend behavior in a plug-and-play fashion by registering additional strategies</item>
    /// </list>
    /// </para>
    ///
    /// <example>
    /// Example usage:
    /// <code>
    /// var dispatcher = new OgcApiStrategyDispatcher(
    ///     conformance,
    ///     new[] {
    ///         new PyCswOgcApiRecordsStrategy(conformance),
    ///         new GeoNetworkOgcApiRecordsStrategy(conformance)
    ///     }
    /// );
    ///
    /// var catalogItem = dispatcher.ParseFeature(feature); // Dispatches to correct strategy
    /// </code>
    /// </example>
    ///
    /// <remarks>
    /// This class itself is a subclass of <see cref="OgcApiRecordsStrategy"/>, meaning it can be passed
    /// anywhere a single strategy is expected. This provides full interoperability with systems that
    /// consume strategies without needing to know whether dispatching is involved.
    /// </remarks>
    /// </summary>
    public class OgcApiStrategyDispatcher : OgcApiRecordsStrategy
    {
        private readonly List<OgcApiRecordsStrategy> strategies;

        public OgcApiStrategyDispatcher(
            ConformanceDeclaration conformance,
            IEnumerable<OgcApiRecordsStrategy> strategies = null
        ) : base(conformance)
        {
            // initialise the default strategies when no strategies are provided
            if (strategies == null)
            {
                strategies = new List<OgcApiRecordsStrategy>
                {
                    new PyCswOgcApiRecordsStrategy(conformance),
                    new FallbackOgcApiRecordsStrategy(conformance)
                };
            }

            this.strategies = strategies.ToList();
        }

        public override bool CanHandle(Feature feature)
        {
            return strategies.Any(s => s.CanHandle(feature));
        }

        public override ICatalogItem ParseFeature(Feature feature)
        {
            if (TryParseFeature(feature, out var catalogItem)) return catalogItem;

            throw new InvalidOperationException("No registered strategy could parse this feature.");
        }

        public override bool TryParseFeature(Feature feature, out ICatalogItem catalogItem)
        {
            foreach (var strategy in strategies)
            {
                if (!strategy.CanHandle(feature)) continue;

                catalogItem = strategy.ParseFeature(feature);
                return true;
            }

            catalogItem = null;
            return false;
        }
    }
}