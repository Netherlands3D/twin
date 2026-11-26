using System;
using System.Collections.Generic;
using System.Linq;

namespace Netherlands3D.Twin.DataSets
{
    internal static class DashboardSummary
    {
        public static (PhaseTotals[] Phases, BreakdownRow[] Segments, BreakdownRow[] Rooms, BreakdownRow[] Categories, BreakdownRow[] SocialFacilities) Calculate(DashboardData data)
        {
            var inputs = SelectPhases(data, null);
            var breakdowns = CalculateBreakdowns(data, inputs);
            return (inputs.Select(CalculatePhase).ToArray(), breakdowns.Segments, breakdowns.Rooms, breakdowns.Categories, breakdowns.SocialFacilities);
        }

        public static (PhaseTotals Current, BreakdownRow[] Segments, BreakdownRow[] Categories) CalculateProgramOverview(DashboardData data, string block)
        {
            return CalculateProgramOverview(data, block, null);
        }

        public static (PhaseTotals Current, BreakdownRow[] Segments, BreakdownRow[] Categories) CalculateProgramOverview(DashboardData data, string block, string phase)
        {
            if (data == null)
                return (default, Array.Empty<BreakdownRow>(), Array.Empty<BreakdownRow>());

            var input = SelectPhase(data, phase ?? DashboardData.Phases.Last(), block, useEarlierPhases: phase == null);
            var breakdowns = CalculateBreakdowns(data, new[] { input });
            return (CalculatePhase(input), breakdowns.Segments, breakdowns.Categories);
        }

        public static PhaseTotals[] CalculatePhases(DashboardData data, string block = null)
        {
            return SelectPhases(data, block).Select(CalculatePhase).ToArray();
        }

        public static PhaseTotals CalculateCurrent(DashboardData data) => CalculatePhases(data).LastOrDefault();

        public static int CountBlocks(DashboardData data) => CountBlocks(data, null);

        public static int CountBlocks(DashboardData data, string phase)
        {
            if (data == null)
                return 0;

            return data.Housing.Where(row => phase == null || Equal(row.Phase, phase)).Select(row => row.Block)
                .Concat(data.NonHousing.Where(row => phase == null || Equal(row.Phase, phase)).Select(row => row.Block))
                .Concat(data.ProgramChanges.Where(row => phase == null || Equal(row.Phase, phase)).Select(row => row.Block))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }

        public static string[] GetBlockPhases(DashboardData data, string block)
        {
            if (data == null || block == null)
                return Array.Empty<string>();

            var available = data.Housing.Where(row => Equal(row.Block, block)).Select(row => row.Phase)
                .Concat(data.NonHousing.Where(row => Equal(row.Block, block)).Select(row => row.Phase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return DashboardData.Phases.Where(available.Contains).ToArray();
        }

        public static (string Label, NumericResult Total, NumericResult[] Values)[] CalculateHousingRooms(DashboardData data)
        {
            return CalculateHousingRooms(data, null);
        }

        public static (string Label, NumericResult Total, NumericResult[] Values)[] CalculateHousingRooms(DashboardData data, string block)
        {
            return CalculateHousingRooms(data, block, null);
        }

        public static (string Label, NumericResult Total, NumericResult[] Values)[] CalculateHousingRooms(DashboardData data, string block, string phase)
        {
            if (data == null)
                return Array.Empty<(string Label, NumericResult Total, NumericResult[] Values)>();

            var selection = SelectRows(data.Housing, row => row.Block, row => row.Phase, phase ?? DashboardData.Phases.Last(), block, useEarlierPhases: phase == null);
            var segments = HousingSegments(data).ToArray();
            var incomplete = selection.Sources.MissingBlocks > 0 || selection.Rows.Any(row => !row.Rooms.HasValue && row.DwellingCount != 0);
            return data.Housing.Where(row => row.Rooms.HasValue).Select(row => row.Rooms.Value).Distinct().OrderBy(room => room).Select(room =>
            {
                var rows = selection.Rows.Where(row => row.Rooms == room).ToArray();
                var values = segments.Select(segment => Sum(rows.Where(row => Equal(SegmentName(row.Segment), segment))
                    .Select(row => (decimal?)row.DwellingCount), incomplete)).ToArray();
                return (room == 1 ? "1 kamer" : $"{room} kamers", Sum(rows.Select(row => (decimal?)row.DwellingCount), incomplete), values);
            }).ToArray();
        }

        public static (string Block, string Phase, NumericResult Value)[] CalculateNonHousingBlockPhases(DashboardData data)
        {
            if (data == null)
                return Array.Empty<(string Block, string Phase, NumericResult Value)>();

            return data.NonHousing.GroupBy(row => row.Block, StringComparer.OrdinalIgnoreCase).OrderBy(block => block.Key, StringComparer.OrdinalIgnoreCase)
                .SelectMany(block => DashboardData.Phases.Where(phase => block.Any(row => Equal(row.Phase, phase)))
                    .Select(phase => (block.Key, phase, Sum(block.Where(row => Equal(row.Phase, phase)).Select(row => row.GrossFloorArea), false)))).ToArray();
        }

        public static NumericResult SumResults(IEnumerable<NumericResult> values)
        {
            var items = values.ToArray();
            return Sum(items.Select(item => item.Value), items.Any(item => item.Status == ResultStatus.Unknown || item.Status == ResultStatus.Partial));
        }

        public static (string Label, NumericResult Value)[] CalculateHousingFloorAreas(DashboardData data)
        {
            return CalculateHousingFloorAreas(data, null);
        }

        public static (string Label, NumericResult Value)[] CalculateHousingFloorAreas(DashboardData data, string block)
        {
            return CalculateHousingFloorAreas(data, block, null);
        }

        public static (string Label, NumericResult Value)[] CalculateHousingFloorAreas(DashboardData data, string block, string phase)
        {
            if (data == null)
                return Array.Empty<(string Label, NumericResult Value)>();

            var selection = SelectRows(data.Housing, row => row.Block, row => row.Phase, phase ?? DashboardData.Phases.Last(), block, useEarlierPhases: phase == null);
            return HousingSegments(data).Select(segment => (segment, AverageMinimumFloorArea(
                selection.Rows.Where(row => Equal(SegmentName(row.Segment), segment)).ToArray(), selection.Sources.MissingBlocks > 0))).ToArray();
        }

        public static HousingBlockSummary[] CalculateHousingBlocks(DashboardData data, string phase = null)
        {
            return CalculateHousingBlocks(data, phase, null);
        }

        public static HousingBlockSummary[] CalculateHousingBlocks(DashboardData data, string phase, string block)
        {
            if (data == null)
                return Array.Empty<HousingBlockSummary>();

            var selection = SelectRows(data.Housing, row => row.Block, row => row.Phase, phase ?? DashboardData.Phases.Last(), block, useEarlierPhases: phase == null);
            var segments = HousingSegments(data).ToArray();
            return selection.Sources.BlockPhases.Where(source => phase == null || source.Value != null)
                .OrderBy(source => source.Key, StringComparer.OrdinalIgnoreCase).Select(source =>
                {
                    var rows = selection.Rows.Where(row => Equal(row.Block, source.Key)).ToArray();
                    var values = segments.Select(segment => Sum(rows.Where(row => Equal(SegmentName(row.Segment), segment))
                        .Select(row => (decimal?)row.DwellingCount), source.Value == null)).ToArray();
                    return new HousingBlockSummary(source.Key, source.Value, Sum(rows.Select(row => (decimal?)row.DwellingCount), source.Value == null), segments, values);
                }).ToArray();
        }

        public static (NumericResult Difference, NumericResult RelativeChange)[] CalculateChanges(NumericResult[] values)
        {
            var changes = new (NumericResult Difference, NumericResult RelativeChange)[values.Length];
            for (var i = 1; i < values.Length; i++)
            {
                var current = values[i];
                var previous = values[i - 1];
                if (!current.Value.HasValue || !previous.Value.HasValue)
                {
                    var status = UnavailableStatus(current, previous);
                    changes[i] = (new NumericResult(null, status), new NumericResult(null, status));
                    continue;
                }

                var resultStatus = current.Status == ResultStatus.Complete && previous.Status == ResultStatus.Complete ? ResultStatus.Complete : ResultStatus.Partial;
                var difference = current.Value.Value - previous.Value.Value;
                changes[i] = (new NumericResult(difference, resultStatus), previous.Value > 0
                    ? new NumericResult(difference / previous.Value.Value, resultStatus)
                    : new NumericResult(null, ResultStatus.NotApplicable));
            }

            return changes;
        }

        public static NumericResult CalculateSegmentShare(NumericResult dwellingsInSegment, NumericResult phaseTotal)
        {
            if (!dwellingsInSegment.Value.HasValue || !phaseTotal.Value.HasValue)
                return new NumericResult(null, UnavailableStatus(dwellingsInSegment, phaseTotal));

            var status = dwellingsInSegment.Status == ResultStatus.Complete && phaseTotal.Status == ResultStatus.Complete
                ? ResultStatus.Complete : ResultStatus.Partial;
            return phaseTotal.Value > 0
                ? new NumericResult(dwellingsInSegment.Value.Value / phaseTotal.Value.Value, status)
                : new NumericResult(null, ResultStatus.NotApplicable);
        }

        public static string FirstBlockWithData(DashboardData data)
        {
            if (data == null)
                return null;

            return data.Housing.Select(row => row.Block)
                .Concat(data.NonHousing.Select(row => row.Block))
                .OrderBy(block => block, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        public static (BreakdownRow[] Segments, BreakdownRow[] Rooms, BreakdownRow[] Categories, BreakdownRow[] SocialFacilities) CalculateBreakdowns(DashboardData data)
        {
            return CalculateBreakdowns(data, SelectPhases(data, null));
        }

        private static (BreakdownRow[] Segments, BreakdownRow[] Rooms, BreakdownRow[] Categories, BreakdownRow[] SocialFacilities) CalculateBreakdowns(DashboardData data, PhaseInput[] phases)
        {
            if (phases.Length == 0)
            {
                var empty = Array.Empty<BreakdownRow>();
                return (empty, empty, empty, empty);
            }

            var segments = HousingSegments(data)
                .Select(segment => new BreakdownRow(segment, phases.Select(input => Sum(input.Housing.Rows
                    .Where(row => Equal(SegmentName(row.Segment), segment)).Select(row => (decimal?)row.DwellingCount), input.Housing.Sources.MissingBlocks > 0)).ToArray())).ToArray();
            var rooms = data.Housing.Where(row => row.Rooms.HasValue).Select(row => row.Rooms.Value).Distinct().OrderBy(room => room)
                .Select(room => new BreakdownRow(room == 1 ? "1 kamer" : $"{room} kamers", phases.Select(input => Sum(input.Housing.Rows
                    .Where(row => row.Rooms == room).Select(row => (decimal?)row.DwellingCount), input.Housing.Sources.MissingBlocks > 0 ||
                        input.Housing.Rows.Any(row => !row.Rooms.HasValue && row.DwellingCount != 0))).ToArray())).ToArray();
            var categoriesByName = data.NonHousing.Select(row => row.Category).Distinct().ToDictionary(category => category, SplitCategory);
            var categories = categoriesByName.Values;
            var mainCategories = categories.Select(category => category.Main).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                .Select(category => new BreakdownRow(category, phases.Select(input => Sum(input.NonHousing.Rows
                    .Where(row => Equal(categoriesByName[row.Category].Main, category)).Select(row => row.GrossFloorArea), input.NonHousing.Sources.MissingBlocks > 0)).ToArray())).ToArray();
            var socialFacilities = categories.Where(category => Equal(category.Main, "Maatschappelijk") && !string.IsNullOrEmpty(category.Subcategory))
                .Select(category => category.Subcategory).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                .Select(category => new BreakdownRow(category, phases.Select(input => Sum(input.NonHousing.Rows.Where(row =>
                {
                    var split = categoriesByName[row.Category];
                    return Equal(split.Main, "Maatschappelijk") && Equal(split.Subcategory, category);
                }).Select(row => row.GrossFloorArea), input.NonHousing.Sources.MissingBlocks > 0)).ToArray())).ToArray();
            return (segments, rooms, mainCategories, socialFacilities);
        }

        public static string BlockName(string block) => block.StartsWith("Blok ", StringComparison.OrdinalIgnoreCase) ? block : $"Blok {block}";

        private static PhaseInput[] SelectPhases(DashboardData data, string block)
        {
            if (data == null)
                return Array.Empty<PhaseInput>();

            // Area-wide phase comparisons keep the latest available version up to each phase.
            return DashboardData.Phases.Select(phase => SelectPhase(data, phase, block, useEarlierPhases: block == null)).ToArray();
        }

        private static PhaseInput SelectPhase(DashboardData data, string phase, string block, bool useEarlierPhases)
        {
            return new PhaseInput(phase,
                SelectRows(data.Housing, row => row.Block, row => row.Phase, phase, block, useEarlierPhases),
                SelectRows(data.NonHousing, row => row.Block, row => row.Phase, phase, block, useEarlierPhases));
        }

        private static Selection<T> SelectRows<T>(IEnumerable<T> rows, Func<T, string> getBlock, Func<T, string> getPhase, string phase, string block, bool useEarlierPhases = false)
        {
            var selected = new List<T>();
            var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var phaseIndex = PhaseIndex(phase);
            // Each program has its own block coverage; a housing-only block does not imply unknown non-housing data.
            foreach (var blockRows in rows.Where(row => block == null || Equal(getBlock(row), block)).GroupBy(getBlock, StringComparer.OrdinalIgnoreCase))
            {
                // Select a whole version before inspecting its values. Unknown fields must not fall back independently.
                var source = blockRows.Select(getPhase).Where(candidate => useEarlierPhases ? PhaseIndex(candidate) <= phaseIndex : Equal(candidate, phase))
                    .OrderByDescending(PhaseIndex).FirstOrDefault();
                sources.Add(blockRows.Key, source);
                if (source != null)
                    selected.AddRange(blockRows.Where(row => Equal(getPhase(row), source)));
            }

            return new Selection<T>(selected.ToArray(), new PhaseSources(phase, sources));
        }

        private static int PhaseIndex(string phase)
        {
            for (var i = 0; i < DashboardData.Phases.Count; i++)
            {
                if (Equal(DashboardData.Phases[i], phase))
                    return i;
            }

            return -1;
        }

        private static PhaseTotals CalculatePhase(PhaseInput input)
        {
            var housing = input.Housing.Rows;
            var nonHousing = input.NonHousing.Rows;
            var missingHousing = input.Housing.Sources.MissingBlocks > 0;
            return new PhaseTotals(input.Phase,
                Sum(housing.Select(row => (decimal?)row.DwellingCount), missingHousing),
                Sum(housing.Where(row => Equal(SegmentName(row.Segment), "Sociaal")).Select(row => (decimal?)row.DwellingCount), missingHousing),
                Sum(housing.Where(row => Equal(SegmentName(row.Segment), "Middenhuur")).Select(row => (decimal?)row.DwellingCount), missingHousing),
                Sum(housing.Where(row => Equal(SegmentName(row.Segment), "Vrije sector")).Select(row => (decimal?)row.DwellingCount), missingHousing),
                AverageMinimumFloorArea(housing, missingHousing),
                Sum(nonHousing.Select(row => row.GrossFloorArea), input.NonHousing.Sources.MissingBlocks > 0),
                input.Housing.Sources, input.NonHousing.Sources);
        }

        private static NumericResult Sum(IEnumerable<decimal?> values, bool missingBlocks)
        {
            var items = values.ToArray();
            if (!items.Any(value => value.HasValue))
                return new NumericResult(null, ResultStatus.Unknown);

            return new NumericResult(items.Sum(), missingBlocks || items.Any(value => !value.HasValue) ? ResultStatus.Partial : ResultStatus.Complete);
        }

        private static ResultStatus UnavailableStatus(NumericResult first, NumericResult second)
        {
            if (first.Status == ResultStatus.Partial || second.Status == ResultStatus.Partial)
                return ResultStatus.Partial;
            if (first.Status == ResultStatus.NotApplicable || second.Status == ResultStatus.NotApplicable)
                return ResultStatus.NotApplicable;

            return first.Status == ResultStatus.Complete || second.Status == ResultStatus.Complete ? ResultStatus.Partial : ResultStatus.Unknown;
        }

        private static NumericResult AverageMinimumFloorArea(DashboardData.HousingRow[] rows, bool missingBlocks)
        {
            var usable = rows.Where(row => row.DwellingCount > 0 && row.MinimumUsableFloorArea.HasValue).ToArray();
            var incomplete = missingBlocks || rows.Any(row => !row.DwellingCount.HasValue || row.DwellingCount > 0 && !row.MinimumUsableFloorArea.HasValue);
            if (usable.Length == 0)
                return new NumericResult(null, !incomplete && rows.Length > 0 ? ResultStatus.NotApplicable : ResultStatus.Unknown);

            var weightedArea = usable.Sum(row => row.MinimumUsableFloorArea.Value * row.DwellingCount.Value);
            var dwellings = usable.Sum(row => (long)row.DwellingCount.Value);
            return new NumericResult(weightedArea / dwellings, incomplete ? ResultStatus.Partial : ResultStatus.Complete);
        }

        private static (string Main, string Subcategory) SplitCategory(string value)
        {
            var category = value.Trim();
            var openingBracket = category.IndexOf('(');
            var subcategory = string.Empty;
            if (openingBracket > 0 && category.EndsWith(")", StringComparison.Ordinal))
            {
                subcategory = category.Substring(openingBracket + 1, category.Length - openingBracket - 2).Trim();
                if (subcategory.Length > 0)
                    category = category.Substring(0, openingBracket).TrimEnd();
            }

            if (Equal(category, "Maatschappelijk") || Equal(category, "Maatschappelijke"))
                category = "Maatschappelijk";
            else if (Equal(category, "Commercieel") || Equal(category, "Horeca"))
                category = "Horeca";

            return (category, subcategory);
        }

        private static string SegmentName(string segment)
        {
            if (Equal(segment, "Midden") || Equal(segment, "Middenhuur"))
                return "Middenhuur";
            if (Equal(segment, "Vrij") || Equal(segment, "Vrije sector"))
                return "Vrije sector";
            return Equal(segment, "Sociaal") ? "Sociaal" : segment;
        }

        private static IEnumerable<string> HousingSegments(DashboardData data) => data.Housing.Select(row => SegmentName(row.Segment))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(segment => segment switch { "Sociaal" => 0, "Middenhuur" => 1, "Vrije sector" => 2, _ => 3 })
            .ThenBy(segment => segment, StringComparer.OrdinalIgnoreCase);

        private static bool Equal(string first, string second) => string.Equals(first, second, StringComparison.OrdinalIgnoreCase);

        internal enum ResultStatus
        {
            Unknown,
            Complete,
            Partial,
            NotApplicable
        }

        internal readonly struct NumericResult
        {
            public decimal? Value { get; }
            public ResultStatus Status { get; }
            public decimal? CompleteValue => Status == ResultStatus.Complete ? Value : null;

            public NumericResult(decimal? value, ResultStatus status)
            {
                Value = value;
                Status = status;
            }
        }

        internal sealed class PhaseSources
        {
            public string Phase { get; }
            public IReadOnlyDictionary<string, string> BlockPhases { get; }
            public int MissingBlocks => BlockPhases.Count(entry => entry.Value == null);
            public int ExactBlocks => BlockPhases.Count(entry => Equal(entry.Value, Phase));
            public int EarlierBlocks => BlockPhases.Count - MissingBlocks - ExactBlocks;
            public bool HasData => BlockPhases.Any(entry => entry.Value != null);

            public PhaseSources(string phase, Dictionary<string, string> blockPhases)
            {
                Phase = phase;
                BlockPhases = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(blockPhases);
            }
        }

        private readonly struct Selection<T>
        {
            public T[] Rows { get; }
            public PhaseSources Sources { get; }

            public Selection(T[] rows, PhaseSources sources)
            {
                Rows = rows;
                Sources = sources;
            }
        }

        private readonly struct PhaseInput
        {
            public string Phase { get; }
            public Selection<DashboardData.HousingRow> Housing { get; }
            public Selection<DashboardData.NonHousingRow> NonHousing { get; }

            public PhaseInput(string phase, Selection<DashboardData.HousingRow> housing, Selection<DashboardData.NonHousingRow> nonHousing)
            {
                Phase = phase;
                Housing = housing;
                NonHousing = nonHousing;
            }
        }

        internal readonly struct HousingBlockSummary
        {
            public string Block { get; }
            public string SourcePhase { get; }
            public NumericResult Total { get; }
            public string[] Segments { get; }
            public NumericResult[] Values { get; }

            public HousingBlockSummary(string block, string sourcePhase, NumericResult total, string[] segments, NumericResult[] values)
            {
                Block = block;
                SourcePhase = sourcePhase;
                Total = total;
                Segments = segments;
                Values = values;
            }
        }

        internal readonly struct BreakdownRow
        {
            public string Label { get; }
            public NumericResult[] Values { get; }

            public BreakdownRow(string label, NumericResult[] values)
            {
                Label = label;
                Values = values;
            }
        }

        internal readonly struct PhaseTotals
        {
            public string Phase { get; }
            public NumericResult Dwellings { get; }
            public NumericResult SocialDwellings { get; }
            public NumericResult MidRentDwellings { get; }
            public NumericResult FreeSectorDwellings { get; }
            public NumericResult AverageMinimumFloorArea { get; }
            public NumericResult GrossFloorArea { get; }
            public PhaseSources HousingSources { get; }
            public PhaseSources NonHousingSources { get; }
            public bool HasData => HousingSources?.HasData == true || NonHousingSources?.HasData == true;

            public PhaseTotals(string phase, NumericResult dwellings, NumericResult socialDwellings, NumericResult midRentDwellings, NumericResult freeSectorDwellings,
                NumericResult averageMinimumFloorArea, NumericResult grossFloorArea, PhaseSources housingSources, PhaseSources nonHousingSources)
            {
                Phase = phase;
                Dwellings = dwellings;
                SocialDwellings = socialDwellings;
                MidRentDwellings = midRentDwellings;
                FreeSectorDwellings = freeSectorDwellings;
                AverageMinimumFloorArea = averageMinimumFloorArea;
                GrossFloorArea = grossFloorArea;
                HousingSources = housingSources;
                NonHousingSources = nonHousingSources;
            }
        }
    }
}
