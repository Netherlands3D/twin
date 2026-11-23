using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.Twin;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardHUD : VisualElement
    {
        private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("nl-NL");
        private const string SquareMetres = " m\u00b2";
        private const string AllBlocksExplanation = "Per blok gebruiken we de laatst beschikbare gegevens.";
        private const string PhaseComparisonExplanation = "BVO per blok en fase toont alleen de beschikbare fasegegevens.";
        private const string IncompleteDataExplanation = "*Onvolledig: alleen bekende gegevens tellen mee, ook voor percentages en gemiddelden.";

        private readonly DashboardBlockOverview blockOverview;
        private readonly DashboardPhaseComparisonPanel phaseComparison;
        private readonly BarChart housingChart;
        private readonly RingChart housingSegmentChart;
        private readonly AreaComparisonChart housingFloorAreaChart;
        private readonly BarChart housingRoomChart;
        private readonly RingChart nonHousingCategoryChart;
        private readonly BarChart housingPhaseChart;
        private readonly BarChart housingRoomPhaseChart;
        private readonly AreaComparisonChart housingFloorAreaPhaseChart;
        private readonly BarChart nonHousingPhaseChart;
        private readonly BarChart nonHousingBlockPhaseChart;
        private readonly Dictionary<string, Color> additionalColors = new(StringComparer.OrdinalIgnoreCase);
        private readonly TextCallOut sidebarDataExplanation;
        private readonly ToggleSwitch phaseView;
        private Layers layers;
        private ProjectDataHandler projectLoader;
        private DashboardData displayedData;
        private (DashboardSummary.PhaseTotals[] Phases, DashboardSummary.BreakdownRow[] Segments, DashboardSummary.BreakdownRow[] Rooms,
            DashboardSummary.BreakdownRow[] Categories, DashboardSummary.BreakdownRow[] SocialFacilities) summary;
        private (string Block, DashboardSummary.PhaseTotals[] Phases, DashboardSummary.PhaseTotals Housing, DashboardSummary.PhaseTotals NonHousing) blockSelection;
        private DashboardSummary.HousingBlockSummary[] housingBlocks = Array.Empty<DashboardSummary.HousingBlockSummary>();
        private (string Label, DashboardSummary.NumericResult Value)[] housingFloorAreas = Array.Empty<(string Label, DashboardSummary.NumericResult Value)>();
        private (string Label, DashboardSummary.NumericResult Total, DashboardSummary.NumericResult[] Values)[] housingRooms = Array.Empty<(string Label, DashboardSummary.NumericResult Total, DashboardSummary.NumericResult[] Values)>();
        private (string Block, string Phase, DashboardSummary.NumericResult Value)[] nonHousingBlockPhases = Array.Empty<(string Block, string Phase, DashboardSummary.NumericResult Value)>();
        private DashboardSummary.NumericResult[] roomPhaseTotals = Array.Empty<DashboardSummary.NumericResult>();

        public DashboardHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            phaseView = this.Q<ToggleSwitch>("DashboardViewSwitch");
            sidebarDataExplanation = this.Q<TextCallOut>("SidebarDataExplanation");
            var programOverview = this.Q("ProgramOverviewContent");
            var phaseProgress = this.Q("PhaseProgressContent");
            var phaseChanges = this.Q("PhaseChangesPanel");
            blockOverview = programOverview.Q<DashboardBlockOverview>();
            phaseComparison = phaseChanges.Q<DashboardPhaseComparisonPanel>();
            phaseComparison.ViewChanged += UpdateSidebarExplanation;
            housingChart = programOverview.Q<BarChart>("HousingBlockChart");
            housingSegmentChart = programOverview.Q<RingChart>("HousingSegmentChart");
            housingFloorAreaChart = programOverview.Q<AreaComparisonChart>("HousingFloorAreaChart");
            housingRoomChart = programOverview.Q<BarChart>("HousingRoomChart");
            nonHousingCategoryChart = programOverview.Q<RingChart>("NonHousingCategoryChart");
            housingPhaseChart = phaseProgress.Q<BarChart>("HousingPhaseChart");
            housingRoomPhaseChart = phaseProgress.Q<BarChart>("HousingRoomPhaseChart");
            housingFloorAreaPhaseChart = phaseProgress.Q<AreaComparisonChart>("HousingFloorAreaPhaseChart");
            nonHousingPhaseChart = phaseProgress.Q<BarChart>("NonHousingPhaseChart");
            nonHousingBlockPhaseChart = phaseProgress.Q<BarChart>("NonHousingBlockPhaseChart");
            foreach (var chart in this.Query<BarChart>().ToList())
                chart.NumberCulture = NumberCulture;
            foreach (var chart in this.Query<RingChart>().ToList())
                chart.NumberCulture = NumberCulture;
            housingChart.ValueFormat = "#,0";
            housingChart.RegisterCallback<CustomStyleResolvedEvent>(OnChartStylesResolved);

            var housingTypes = this.Q<ToggleButtonGroup>("HousingTypes");
            var nonHousingButton = housingTypes.Q("NonHousingType");
            housingTypes.RegisterValueChangedCallback(evt => ShowHousingType(evt.newValue));
            ShowHousingType(housingTypes.value);

            phaseView.RegisterValueChangedCallback(evt => ShowPhaseView(evt.newValue));
            ShowPhaseView(phaseView.value);

            void ShowHousingType(ToggleButtonGroupState selection)
            {
                for (var i = 0; i < selection.length; i++)
                {
                    if (housingTypes.GetButton(i) != nonHousingButton)
                        continue;

                    phaseComparison.NonHousing = selection[i];
                    return;
                }
            }

            void ShowPhaseView(bool showPhaseProgress)
            {
                programOverview.EnableInClassList(UtilityClassConstants.HIDDEN, showPhaseProgress);
                phaseProgress.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                phaseChanges.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                phaseComparison.ResetView();
                schedule.Execute(ResetScrollPositions);
            }
        }

        private void ResetScrollPositions()
        {
            foreach (var scrollView in this.Q("Sidebar").Query<UnityEngine.UIElements.ScrollView>().ToList())
                scrollView.scrollOffset = Vector2.zero;
        }

        internal void StartDataBinding()
        {
            if (layers != null)
                return;

            layers = App.Layers;
            projectLoader = ProjectDataHandler.Instance;
            layers?.LayerAdded.AddListener(OnLayerAdded);
            layers?.LayerRemoved.AddListener(OnLayerRemoved);
            if (projectLoader != null)
                projectLoader.OnLoadCompleted?.AddListener(RefreshData);

            RefreshData(ProjectData.Current);
        }

        internal void StopDataBinding()
        {
            layers?.LayerAdded.RemoveListener(OnLayerAdded);
            layers?.LayerRemoved.RemoveListener(OnLayerRemoved);
            if (projectLoader != null)
                projectLoader.OnLoadCompleted?.RemoveListener(RefreshData);

            layers = null;
            projectLoader = null;
            ShowData(null);
        }

        private void OnLayerAdded(LayerData layer)
        {
            if (layer.HasProperty<DashboardCsvPropertyData>())
                RefreshData(ProjectData.Current);
        }

        private void OnLayerRemoved(LayerData layer, bool isRemovalRoot) => OnLayerAdded(layer);

        private void RefreshData(ProjectData project)
        {
            var data = project?.RootLayer?.GetFlatHierarchy()
                .Where(layer => !layer.IsDisposed)
                .Select(layer => layer.GetProperty<DashboardCsvPropertyData>()?.Data)
                .FirstOrDefault(candidate => candidate != null);

            ShowData(data);
        }

        private void ShowData(DashboardData data)
        {
            if (ReferenceEquals(displayedData, data))
                return;

            displayedData = data;
            summary = DashboardSummary.Calculate(data);
            blockSelection = SelectBlockOverview(data);
            housingBlocks = DashboardSummary.CalculateHousingBlocks(data);
            housingFloorAreas = DashboardSummary.CalculateHousingFloorAreas(data);
            housingRooms = DashboardSummary.CalculateHousingRooms(data);
            nonHousingBlockPhases = DashboardSummary.CalculateNonHousingBlockPhases(data);
            roomPhaseTotals = summary.Phases.Select((phase, index) => DashboardSummary.SumResults(summary.Rooms.Select(row => row.Values[index]))).ToArray();
            RefreshHousingColors();
            ShowHousingChart();
            ShowAdditionalCharts();
            var phases = summary.Phases;

            phaseComparison.SetKeyFigureData(
                phases.Select(phase => FormatNumber(phase.Dwellings)).ToArray(),
                FormatChanges(phases, phase => phase.Dwellings, ""),
                phases.Select(phase => FormatNumber(phase.AverageMinimumFloorArea, SquareMetres)).ToArray(),
                FormatChanges(phases, phase => phase.AverageMinimumFloorArea, SquareMetres),
                phases.Select(phase => FormatNumber(phase.GrossFloorArea, SquareMetres)).ToArray(),
                FormatChanges(phases, phase => phase.GrossFloorArea, SquareMetres));

            phaseComparison.SetBreakdownData(
                summary.Segments.Select(row => (row.Label, row.Values.Select((value, i) => FormatSegment(value, phases[i].Dwellings)).ToArray())).ToArray(),
                FormatBreakdown(summary.Rooms),
                FormatBreakdown(summary.Categories, SquareMetres),
                FormatBreakdown(summary.SocialFacilities, SquareMetres));

            phaseComparison.SetSourceData(
                phases.Any(phase => phase.HousingSources.HasData), phases.Any(phase => phase.NonHousingSources.HasData),
                phases.Select(phase => FormatSources(phase.HousingSources)).ToArray(),
                phases.Select(phase => FormatSources(phase.NonHousingSources)).ToArray());
            var (block, availablePhases, housingPhase, nonHousingPhase) = blockSelection;
            var sourcePhases = new[]
            {
                housingPhase.HasData ? $"Wonen: {housingPhase.Phase}" : null,
                nonHousingPhase.HasData ? $"Niet-wonen: {nonHousingPhase.Phase}" : null
            }.Where(source => source != null);

            blockOverview.HeaderText = availablePhases.Length > 0 ? $"Overzicht {DashboardSummary.BlockName(block)} ({string.Join(", ", sourcePhases)})" : "Bouwblokoverzicht";
            blockOverview.TotalDwellings = FormatNumber(housingPhase.Dwellings);
            blockOverview.SocialDwellings = FormatNumber(housingPhase.SocialDwellings);
            blockOverview.MidRentDwellings = FormatNumber(housingPhase.MidRentDwellings);
            blockOverview.FreeSectorDwellings = FormatNumber(housingPhase.FreeSectorDwellings);
            blockOverview.AverageMinimumFloorArea = FormatNumber(housingPhase.AverageMinimumFloorArea, SquareMetres);
            blockOverview.AvailablePhases = string.Join(" / ", availablePhases.Select(phase => phase.Phase));
            blockOverview.NonHousingGrossFloorArea = FormatNumber(nonHousingPhase.GrossFloorArea, SquareMetres);
            phaseComparison.Refresh();
        }

        private static (string Block, DashboardSummary.PhaseTotals[] Phases, DashboardSummary.PhaseTotals Housing, DashboardSummary.PhaseTotals NonHousing) SelectBlockOverview(DashboardData data)
        {
            var block = DashboardSummary.FirstBlockWithData(data);
            var phases = block == null ? Array.Empty<DashboardSummary.PhaseTotals>() : DashboardSummary.CalculatePhases(data, block);
            var availablePhases = phases.Where(phase => phase.HasData).ToArray();
            return (block, availablePhases, availablePhases.LastOrDefault(phase => phase.HousingSources.HasData),
                availablePhases.LastOrDefault(phase => phase.NonHousingSources.HasData));
        }

        private void UpdateSidebarExplanation()
        {
            if (displayedData == null)
            {
                SetSidebarExplanation(null, false);
                return;
            }

            if (phaseView.value)
            {
                var values = summary.Phases.Where(phase => phase.HousingSources.HasData).SelectMany(phase => new[] { phase.Dwellings, phase.AverageMinimumFloorArea })
                    .Concat(summary.Phases.Where(phase => phase.NonHousingSources.HasData).Select(phase => phase.GrossFloorArea))
                    .Concat(summary.Segments.Concat(summary.Rooms).Concat(summary.Categories).SelectMany(row => row.Values));
                if (phaseComparison.SelectedTab == DashboardPhaseComparisonPanel.ComparisonTab.SocialFacilities)
                    values = values.Concat(summary.SocialFacilities.SelectMany(row => row.Values));
                var visibleValues = values.ToArray();
                var context = string.Join(" ", new[]
                {
                    summary.Phases.Any(HasEarlierSources) ? AllBlocksExplanation : null,
                    nonHousingBlockPhases.Length > 0 ? PhaseComparisonExplanation : null
                }.Where(text => text != null));
                SetSidebarExplanation(context, visibleValues.Any(IsIncomplete));
                return;
            }

            var (_, _, housing, nonHousing) = blockSelection;
            var incomplete = housing.HasData && (IsIncomplete(housing.Dwellings) || IsIncomplete(housing.AverageMinimumFloorArea))
                || nonHousing.HasData && IsIncomplete(nonHousing.GrossFloorArea);

            var blocks = housingBlocks;
            incomplete |= blocks.Any(entry => IsIncomplete(entry.Total));
            incomplete |= housingFloorAreas.Any(entry => IsIncomplete(entry.Value)) || housingRooms.Any(entry => IsIncomplete(entry.Total));
            var current = summary.Phases.LastOrDefault();
            incomplete |= current.NonHousingSources?.HasData == true && IsIncomplete(current.GrossFloorArea);
            SetSidebarExplanation(HasEarlierSources(current) ? AllBlocksExplanation : null, incomplete);
        }

        private static bool HasEarlierSources(DashboardSummary.PhaseTotals phase) =>
            phase.HousingSources?.EarlierBlocks > 0 || phase.NonHousingSources?.EarlierBlocks > 0;

        private static bool IsIncomplete(DashboardSummary.NumericResult result) =>
            result.Status == DashboardSummary.ResultStatus.Unknown || result.Status == DashboardSummary.ResultStatus.Partial;

        // Context text and completeness are supplied together so future filters can update one shared callout.
        private void SetSidebarExplanation(string context, bool hasIncompleteData)
        {
            sidebarDataExplanation.Text = string.Join("\n\n", new[] { context, hasIncompleteData ? IncompleteDataExplanation : null }
                .Where(text => !string.IsNullOrEmpty(text)));
            sidebarDataExplanation.EnableInClassList(UtilityClassConstants.HIDDEN, string.IsNullOrEmpty(sidebarDataExplanation.Text));
        }

        private void OnChartStylesResolved(CustomStyleResolvedEvent evt)
        {
            if (DashboardColors.Read(evt.customStyle, additionalColors))
            {
                RefreshHousingColors();
                ShowHousingChart();
                ShowAdditionalCharts();
            }
        }

        private void RefreshHousingColors()
        {
            DashboardColors.SetSegmentColors((summary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>()).Select(row => row.Label), additionalColors);
            DashboardColors.SetRoomColors((summary.Rooms ?? Array.Empty<DashboardSummary.BreakdownRow>()).Select(row => row.Label), additionalColors);
        }

        private void ShowHousingChart()
        {
            housingFloorAreaChart.SetData(housingFloorAreas.Select(entry =>
            {
                var text = AreaText(entry.Value);
                return new AreaComparisonChart.Item(entry.Label, entry.Value.Value, DashboardColors.GetSegmentColor(entry.Label, additionalColors),
                    $"Gemiddelde minimale\nwoonoppervlakte: {text}", text);
            }).ToArray());
            var total = summary.Phases?.LastOrDefault().Dwellings ?? default;
            var segments = summary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>();
            ShowRingChart(housingSegmentChart, segments, total, " woningen", label => DashboardColors.GetSegmentColor(label, additionalColors));

            var blocks = housingBlocks;
            if (blocks.Length == 0)
            {
                housingChart.SetData(Array.Empty<BarChart.Series>(), Array.Empty<BarChart.Category>());
                return;
            }

            var series = blocks[0].Segments.Select(segment => new BarChart.Series(segment, DashboardColors.GetSegmentColor(segment, additionalColors))).ToArray();
            var categories = blocks.Select(block =>
            {
                var source = $"{DashboardSummary.BlockName(block.Block)}\nBronfase: {block.SourcePhase ?? "onbekend"}";
                var values = block.Values.Select((value, i) => new BarChart.Value(value.Value,
                    $"{source}\n{block.Segments[i]}: {ChartNumber(value)} woningen")).ToArray();
                var details = string.Join("\n", block.Values.Select((value, i) => $"{block.Segments[i]}: {ChartNumber(value)}"));
                var label = block.Total.Status == DashboardSummary.ResultStatus.Complete ? block.Block : block.Block + "*";
                var totalLabel = ChartLabel(block.Total, housingChart);
                return new BarChart.Category(label, values, $"{source}\nTotaal: {ChartNumber(block.Total)} woningen\n{details}", totalLabel);
            }).ToArray();
            housingChart.SetData(series, categories);
        }

        private void ShowRingChart(RingChart chart, DashboardSummary.BreakdownRow[] rows, DashboardSummary.NumericResult total, string unit, Func<string, Color> colorForLabel)
        {
            chart.SetData(rows.Select(row =>
            {
                var value = row.Values.Last();
                var percentage = value.Value.HasValue && total.Value > 0
                    ? (value.Value.Value / total.Value.Value).ToString("0.#%", NumberCulture) + (IsIncomplete(total) ? "*" : "") : null;
                var tooltip = $"{row.Label}: {ChartNumber(value)}{unit}" + (percentage == null ? "" : $"\n{percentage}");
                return new RingChart.Segment(row.Label, value.Value, colorForLabel(row.Label), tooltip, percentage);
            }).ToArray(), ChartLabel(total), $"Totaal: {ChartNumber(total)}{unit}");
        }

        private void ShowAdditionalCharts()
        {
            var phases = summary.Phases ?? Array.Empty<DashboardSummary.PhaseTotals>();
            var segments = summary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>();
            var rooms = summary.Rooms ?? Array.Empty<DashboardSummary.BreakdownRow>();
            var categories = summary.Categories ?? Array.Empty<DashboardSummary.BreakdownRow>();
            housingRoomChart.SetData(segments.Select(row => new BarChart.Series(row.Label, DashboardColors.GetSegmentColor(row.Label, additionalColors))).ToArray(),
                housingRooms.Select(room => new BarChart.Category(room.Label, room.Values.Select((value, index) => new BarChart.Value(value.Value,
                    $"{room.Label}\n{segments[index].Label}: {ChartNumber(value)} woningen")),
                    $"{room.Label}\nTotaal: {ChartNumber(room.Total)} woningen", ChartLabel(room.Total, housingRoomChart))).ToArray());
            ShowRingChart(nonHousingCategoryChart, categories, phases.LastOrDefault().GrossFloorArea, SquareMetres, SegmentColor);
            ShowPhaseChart(housingPhaseChart, segments, phases.Select(phase => phase.Dwellings).ToArray(), " woningen", label => DashboardColors.GetSegmentColor(label, additionalColors));
            ShowPhaseChart(housingRoomPhaseChart, rooms, roomPhaseTotals, " woningen", label => DashboardColors.GetRoomColor(label, additionalColors));
            ShowPhaseChart(nonHousingPhaseChart, categories, phases.Select(phase => phase.GrossFloorArea).ToArray(), SquareMetres, SegmentColor);
            housingFloorAreaPhaseChart.SetData(phases.Select(phase =>
            {
                var text = AreaText(phase.AverageMinimumFloorArea);
                return new AreaComparisonChart.Item(phase.Phase, phase.AverageMinimumFloorArea.Value, SegmentColor(phase.Phase),
                    $"{phase.Phase}\nGemiddelde minimale\nwoonoppervlakte: {text}", text);
            }).ToArray());

            // Each bar represents one block-phase value; phases are never stacked or added together.
            nonHousingBlockPhaseChart.SetData(DashboardData.Phases.Select(phase => new BarChart.Series(phase, SegmentColor(phase))).ToArray(),
                nonHousingBlockPhases.Select(row => new BarChart.Category($"{row.Block}\n{row.Phase}",
                    DashboardData.Phases.Select(phase => new BarChart.Value(phase == row.Phase ? row.Value.Value : 0,
                        $"{DashboardSummary.BlockName(row.Block)}\n{row.Phase}: {ChartNumber(row.Value)}{SquareMetres}")),
                    $"{DashboardSummary.BlockName(row.Block)}\n{row.Phase}: {ChartNumber(row.Value)}{SquareMetres}", ChartLabel(row.Value, nonHousingBlockPhaseChart))).ToArray());
        }

        private void ShowPhaseChart(BarChart chart, DashboardSummary.BreakdownRow[] rows, DashboardSummary.NumericResult[] totals, string unit, Func<string, Color> colorForLabel)
        {
            var phases = summary.Phases ?? Array.Empty<DashboardSummary.PhaseTotals>();
            chart.SetData(rows.Select(row => new BarChart.Series(row.Label, colorForLabel(row.Label))).ToArray(), phases.Select((phase, index) =>
                new BarChart.Category(phase.Phase, rows.Select(row => new BarChart.Value(row.Values[index].Value,
                    $"{phase.Phase}\n{row.Label}: {ChartNumber(row.Values[index])}{unit}")),
                    $"{phase.Phase}\nTotaal: {ChartNumber(totals[index])}{unit}", ChartLabel(totals[index], chart))).ToArray());
        }

        private static string ChartLabel(DashboardSummary.NumericResult value, BarChart chart = null)
        {
            if (!value.Value.HasValue)
                return "\u2014";

            var number = chart != null ? chart.FormatValue(value.Value.Value)
                : ChartUtility.FormatValue(value.Value.Value, NumberCulture, compact: true, millionSuffix: " mln", billionSuffix: " mld");
            return number + (IsIncomplete(value) ? "*" : "");
        }

        private static string AreaText(DashboardSummary.NumericResult value)
        {
            var text = FormatNumber(value, SquareMetres);
            return string.IsNullOrEmpty(text) ? "\u2014" : text;
        }

        private Color SegmentColor(string segment) => DashboardColors.GetColor(segment, additionalColors);

        private static string ChartNumber(DashboardSummary.NumericResult result) => result.Value.HasValue ? FormatNumber(result) : "\u2014";

        private static (string Label, string[] Cells)[] FormatBreakdown(DashboardSummary.BreakdownRow[] rows, string unit = "")
        {
            return rows.Select(row => (row.Label, row.Values.Select(value => FormatNumber(value, unit)).ToArray())).ToArray();
        }

        private static string FormatSources(DashboardSummary.PhaseSources sources)
        {
            var sourceCounts = DashboardData.Phases.Select(phase => (Phase: phase, Count: sources.BlockPhases.Count(entry => string.Equals(entry.Value, phase, StringComparison.OrdinalIgnoreCase))))
                .Where(source => source.Count > 0).Select(source => $"{source.Phase}: {source.Count}");
            return $"{sources.Phase}: {sources.ExactBlocks} blokken met gegevens van deze fase, {sources.EarlierBlocks} met een eerdere uitwerking, {sources.MissingBlocks} zonder uitwerking.\nBronfasen: {string.Join(", ", sourceCounts)}. Lege numerieke waarden blijven onbekend.";
        }

        private static string FormatSegment(DashboardSummary.NumericResult value, DashboardSummary.NumericResult phaseTotal)
        {
            var count = FormatNumber(value);
            var share = DashboardSummary.CalculateSegmentShare(value, phaseTotal);
            return share.CompleteValue.HasValue ? $"{count} ({share.CompleteValue.Value.ToString("0%", NumberCulture)})" : count;
        }

        private static string FormatNumber(DashboardSummary.NumericResult result, string unit = "")
        {
            if (result.Status == DashboardSummary.ResultStatus.NotApplicable)
                return "n.v.t.";

            if (!result.Value.HasValue)
                return string.Empty;

            var number = result.Value.Value.ToString("#,0.##", NumberCulture) + unit;
            return result.Status == DashboardSummary.ResultStatus.Partial ? $"{number}*" : number;
        }

        private static string[] FormatChanges(DashboardSummary.PhaseTotals[] phases, Func<DashboardSummary.PhaseTotals, DashboardSummary.NumericResult> selectValue,
            string unit)
        {
            var changes = DashboardSummary.CalculateChanges(phases.Select(selectValue).ToArray());
            return changes.Select(change =>
            {
                if (!change.Difference.Value.HasValue)
                    return string.Empty;

                var difference = change.Difference.Value.Value.ToString("+#,0.##;-#,0.##;0", NumberCulture) + unit;
                var formatted = change.RelativeChange.Value.HasValue
                    ? $"{difference} ({change.RelativeChange.Value.Value.ToString("+0%;-0%;0%", NumberCulture)})"
                    : difference;
                return change.Difference.Status == DashboardSummary.ResultStatus.Partial ? $"{formatted}*" : formatted;
            }).ToArray();
        }
    }
}
