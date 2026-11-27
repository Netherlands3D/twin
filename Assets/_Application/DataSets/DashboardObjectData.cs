using System;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.DataSets
{
    /// <summary>
    /// Indexes dashboard records by their prepared model identifier, independently of model geometry.
    /// </summary>
    internal sealed class DashboardObjectData
    {
        private readonly Dictionary<string, BlockData> blocks = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyList<string> HousingSegments { get; } = Array.Empty<string>();

        public DashboardObjectData(DashboardData data)
        {
            if (data == null)
                return;

            var housing = data.Housing.ToLookup(row => row.Block, StringComparer.OrdinalIgnoreCase);
            var nonHousing = data.NonHousing.ToLookup(row => row.Block, StringComparer.OrdinalIgnoreCase);
            var changes = data.ProgramChanges.ToLookup(row => row.Block, StringComparer.OrdinalIgnoreCase);
            var summaries = DashboardSummary.CalculateHousingBlocks(data).ToDictionary(summary => summary.Block, StringComparer.OrdinalIgnoreCase);
            HousingSegments = summaries.Values.FirstOrDefault().Segments ?? Array.Empty<string>();
            var identifiers = housing.Select(group => group.Key).Concat(nonHousing.Select(group => group.Key)).Concat(changes.Select(group => group.Key));
            foreach (var identifier in identifiers.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                summaries.TryGetValue(identifier, out var summary);
                blocks.Add(identifier, new BlockData(identifier, housing[identifier].ToArray(), nonHousing[identifier].ToArray(), changes[identifier].ToArray(), summary));
            }
        }

        public BlockData Find(string identifier) => string.IsNullOrWhiteSpace(identifier) ? null : blocks.GetValueOrDefault(identifier.Trim());

        internal sealed class BlockData
        {
            public string Block { get; }
            public IReadOnlyList<DashboardData.HousingRow> Housing { get; }
            public IReadOnlyList<DashboardData.NonHousingRow> NonHousing { get; }
            public IReadOnlyList<DashboardData.ProgramChangeRow> ProgramChanges { get; }
            public DashboardSummary.HousingBlockSummary HousingSummary { get; }
            public string LatestPhase { get; }

            public BlockData(string block, DashboardData.HousingRow[] housing, DashboardData.NonHousingRow[] nonHousing, DashboardData.ProgramChangeRow[] programChanges, DashboardSummary.HousingBlockSummary housingSummary)
            {
                Block = block;
                Housing = housing;
                NonHousing = nonHousing;
                ProgramChanges = programChanges;
                HousingSummary = housingSummary;
                var phases = housing.Select(row => row.Phase).Concat(nonHousing.Select(row => row.Phase)).Concat(programChanges.Select(row => row.Phase));
                LatestPhase = DashboardData.Phases.LastOrDefault(phase => phases.Contains(phase, StringComparer.OrdinalIgnoreCase));
            }
        }
    }
}
