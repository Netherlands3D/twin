using System;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.DataSets
{
    internal static class DashboardSummary
    {
        private static readonly string[] Phases = { "BBP", "SO", "VO", "DO" };

        public static PhaseTotals[] CalculatePhases(DashboardData data, string block = null)
        {
            if (data == null)
                return Array.Empty<PhaseTotals>();

            var housing = data.Housing.Where(row => block == null || Equal(row.Block, block)).ToArray();
            var nonHousing = data.NonHousing.Where(row => block == null || Equal(row.Block, block)).ToArray();

            return Phases.Select(phase => CalculatePhase(phase, housing.Where(row => Equal(row.Phase, phase)).ToArray(),
                nonHousing.Where(row => Equal(row.Phase, phase)).ToArray())).ToArray();
        }

        public static (decimal? Difference, decimal? RelativeChange)[] CalculateChanges(PhaseTotals[] phases, Func<PhaseTotals, decimal?> selectValue)
        {
            var changes = new (decimal? Difference, decimal? RelativeChange)[phases.Length];
            for (var i = 1; i < phases.Length; i++)
            {
                var current = selectValue(phases[i]);
                var previous = selectValue(phases[i - 1]);
                if (!current.HasValue || !previous.HasValue)
                    continue;

                var difference = current.Value - previous.Value;
                changes[i] = (difference, previous.Value > 0 ? difference / previous.Value : (decimal?)null);
            }

            return changes;
        }

        public static decimal? CalculateSegmentShare(decimal? dwellingsInSegment, long? phaseTotal)
        {
            return phaseTotal > 0 ? dwellingsInSegment / phaseTotal.Value : null;
        }

        public static string FirstBlockWithData(DashboardData data)
        {
            if (data == null)
                return null;

            return data.Housing.Where(row => IsKnownPhase(row.Phase)).Select(row => row.Block)
                .Concat(data.NonHousing.Where(row => IsKnownPhase(row.Phase)).Select(row => row.Block))
                .OrderBy(block => block, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        public static (BreakdownRow[] Segments, BreakdownRow[] Rooms, BreakdownRow[] Categories, BreakdownRow[] HousingBlocks, BreakdownRow[] NonHousingBlocks)
            CalculateBreakdowns(DashboardData data)
        {
            if (data == null)
            {
                var empty = Array.Empty<BreakdownRow>();
                return (empty, empty, empty, empty, empty);
            }

            var housing = data.Housing.Where(row => IsKnownPhase(row.Phase)).ToArray();
            var nonHousing = data.NonHousing.Where(row => IsKnownPhase(row.Phase)).ToArray();

            var segments = housing.GroupBy(row => SegmentName(row.Segment), StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key switch { "Sociaal" => 0, "Middenhuur" => 1, "Vrije sector" => 2, _ => 3 })
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => SummarizeBreakdown(group.Key, group, row => row.Phase, row => row.DwellingCount.Value)).ToArray();
            var rooms = housing.GroupBy(row => row.Rooms.Value).OrderBy(group => group.Key)
                .Select(group => SummarizeBreakdown(group.Key == 1 ? "1 kamer" : $"{group.Key} kamers", group, row => row.Phase, row => row.DwellingCount.Value)).ToArray();
            var categories = nonHousing.GroupBy(row => row.Category, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => SummarizeBreakdown(group.Key, group, row => row.Phase, row => row.GrossFloorArea.Value)).ToArray();
            var housingBlocks = housing.GroupBy(row => row.Block, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => SummarizeBreakdown(BlockName(group.Key), group, row => row.Phase, row => row.DwellingCount.Value)).ToArray();
            var nonHousingBlocks = nonHousing.GroupBy(row => row.Block, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => SummarizeBreakdown(BlockName(group.Key), group, row => row.Phase, row => row.GrossFloorArea.Value)).ToArray();

            return (segments, rooms, categories, housingBlocks, nonHousingBlocks);
        }

        public static string BlockName(string block) => block.StartsWith("Blok ", StringComparison.OrdinalIgnoreCase) ? block : $"Blok {block}";

        private static BreakdownRow SummarizeBreakdown<T>(string label, IEnumerable<T> rows, Func<T, string> getPhase, Func<T, decimal> getValue)
        {
            var totals = rows.GroupBy(getPhase, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Sum(getValue), StringComparer.OrdinalIgnoreCase);
            return new BreakdownRow(label, Phases.Select(phase => totals.TryGetValue(phase, out var total) ? (decimal?)total : null).ToArray());
        }

        private static string SegmentName(string segment)
        {
            if (Equal(segment, "Midden") || Equal(segment, "Middenhuur"))
                return "Middenhuur";
            if (Equal(segment, "Vrij") || Equal(segment, "Vrije sector"))
                return "Vrije sector";
            return Equal(segment, "Sociaal") ? "Sociaal" : segment;
        }

        private static PhaseTotals CalculatePhase(string phase, DashboardData.HousingRow[] housing, DashboardData.NonHousingRow[] nonHousing)
        {
            long? dwellings = housing.Length > 0 ? housing.Sum(row => (long)row.DwellingCount.Value) : null;
            decimal? grossFloorArea = nonHousing.Length > 0 ? nonHousing.Sum(row => row.GrossFloorArea.Value) : null;
            var housingWithArea = housing.Where(row => row.MinimumUsableFloorArea.HasValue).ToArray();

            decimal? averageMinimumFloorArea = null;
            if (housingWithArea.Length > 0)
            {
                var totalMinimumFloorArea = housingWithArea.Sum(row => row.MinimumUsableFloorArea.Value * row.DwellingCount.Value);
                var dwellingsWithArea = housingWithArea.Sum(row => (long)row.DwellingCount.Value);
                averageMinimumFloorArea = totalMinimumFloorArea / dwellingsWithArea;
            }

            return new PhaseTotals(
                phase,
                dwellings,
                DwellingsInSegment(housing, "Sociaal"),
                DwellingsInSegment(housing, "Middenhuur"),
                DwellingsInSegment(housing, "Vrije sector"),
                averageMinimumFloorArea,
                grossFloorArea);
        }

        private static long? DwellingsInSegment(DashboardData.HousingRow[] housing, string segment)
        {
            var matchingRows = housing.Where(row => Equal(SegmentName(row.Segment), segment)).ToArray();
            return matchingRows.Length > 0 ? matchingRows.Sum(row => (long)row.DwellingCount.Value) : null;
        }

        private static bool IsKnownPhase(string phase) => Phases.Any(knownPhase => Equal(phase, knownPhase));

        private static bool Equal(string first, string second) => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);

        internal readonly struct BreakdownRow
        {
            public string Label { get; }
            public decimal?[] Values { get; }

            public BreakdownRow(string label, decimal?[] values)
            {
                Label = label;
                Values = values;
            }
        }

        internal readonly struct PhaseTotals
        {
            public string Phase { get; }
            public long? Dwellings { get; }
            public long? SocialDwellings { get; }
            public long? MidRentDwellings { get; }
            public long? FreeSectorDwellings { get; }
            public decimal? AverageMinimumFloorArea { get; }
            public decimal? GrossFloorArea { get; }
            public bool HasData => Dwellings.HasValue || GrossFloorArea.HasValue;

            public PhaseTotals(string phase, long? dwellings, long? socialDwellings, long? midRentDwellings, long? freeSectorDwellings,
                decimal? averageMinimumFloorArea, decimal? grossFloorArea)
            {
                Phase = phase;
                Dwellings = dwellings;
                SocialDwellings = socialDwellings;
                MidRentDwellings = midRentDwellings;
                FreeSectorDwellings = freeSectorDwellings;
                AverageMinimumFloorArea = averageMinimumFloorArea;
                GrossFloorArea = grossFloorArea;
            }
        }
    }
}
