using System;
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
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardHUD : VisualElement
    {
        private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("nl-NL");
        private const string SquareMetres = " m\u00b2";
        private const string AllBlocksExplanation = "Alle blokken: laatst beschikbare woonfase per blok.";
        private const string PhaseComparisonExplanation = "Per blok de laatst beschikbare uitwerking tot en met de fase.";
        private const string IncompleteDataExplanation = "*Onvolledig: berekend met bekende gegevens. Voor gemiddelden gebruiken we alleen bekende aantallen en oppervlakten.";
        private static readonly CustomStyleProperty<Color> SocialColor = new("--color-dashboard-segment-social");
        private static readonly CustomStyleProperty<Color> MidRentColor = new("--color-dashboard-segment-mid-rent");
        private static readonly CustomStyleProperty<Color> FreeSectorColor = new("--color-dashboard-segment-free-sector");

        private readonly DashboardBlockOverview blockOverview;
        private readonly DashboardPhaseComparisonPanel phaseComparison;
        private readonly BarChart housingChart;
        private readonly TextCallOut sidebarDataExplanation;
        private readonly ToggleSwitch phaseView;
        private Color socialColor = Color.gray;
        private Color midRentColor = Color.gray;
        private Color freeSectorColor = Color.gray;
        private Layers layers;
        private ProjectDataHandler projectLoader;
        private DashboardData displayedData;
        private (DashboardSummary.PhaseTotals[] Phases, DashboardSummary.BreakdownRow[] Segments, DashboardSummary.BreakdownRow[] Rooms,
            DashboardSummary.BreakdownRow[] Categories, DashboardSummary.BreakdownRow[] SocialFacilities) summary;
        private (string Block, DashboardSummary.PhaseTotals[] Phases, DashboardSummary.PhaseTotals Housing, DashboardSummary.PhaseTotals NonHousing) blockSelection;
        private DashboardSummary.HousingBlockSummary[] housingBlocks = Array.Empty<DashboardSummary.HousingBlockSummary>();

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
            housingChart.NumberCulture = NumberCulture;
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
                    housingChart.EnableInClassList(UtilityClassConstants.HIDDEN, selection[i]);
                    return;
                }
            }

            void ShowPhaseView(bool showPhaseProgress)
            {
                programOverview.EnableInClassList(UtilityClassConstants.HIDDEN, showPhaseProgress);
                phaseProgress.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                phaseChanges.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                UpdateSidebarExplanation();
            }
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
            ShowHousingChart();
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
                var values = phaseComparison.SelectedTab switch
                {
                    DashboardPhaseComparisonPanel.ComparisonTab.Segments => summary.Segments.SelectMany(row => row.Values),
                    DashboardPhaseComparisonPanel.ComparisonTab.Rooms => summary.Rooms.SelectMany(row => row.Values),
                    DashboardPhaseComparisonPanel.ComparisonTab.Categories => summary.Categories.SelectMany(row => row.Values),
                    DashboardPhaseComparisonPanel.ComparisonTab.SocialFacilities => summary.SocialFacilities.SelectMany(row => row.Values),
                    _ => phaseComparison.NonHousing
                        ? summary.Phases.Where(phase => phase.NonHousingSources.HasData).Select(phase => phase.GrossFloorArea)
                        : summary.Phases.Where(phase => phase.HousingSources.HasData).SelectMany(phase => new[] { phase.Dwellings, phase.AverageMinimumFloorArea })
                };
                var visibleValues = values.ToArray();
                SetSidebarExplanation(visibleValues.Length > 0 ? PhaseComparisonExplanation : null, visibleValues.Any(IsIncomplete));
                return;
            }

            var (_, _, housing, nonHousing) = blockSelection;
            var incomplete = housing.HasData && (IsIncomplete(housing.Dwellings) || IsIncomplete(housing.AverageMinimumFloorArea))
                || nonHousing.HasData && IsIncomplete(nonHousing.GrossFloorArea);

            var blocks = phaseComparison.NonHousing ? Array.Empty<DashboardSummary.HousingBlockSummary>() : housingBlocks;
            incomplete |= blocks.Any(entry => IsIncomplete(entry.Total));
            SetSidebarExplanation(blocks.Length > 1 ? AllBlocksExplanation : null, incomplete);
        }

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
            var changed = UpdateChartColor(evt.customStyle, SocialColor, ref socialColor);
            changed |= UpdateChartColor(evt.customStyle, MidRentColor, ref midRentColor);
            changed |= UpdateChartColor(evt.customStyle, FreeSectorColor, ref freeSectorColor);
            if (changed)
                ShowHousingChart();
        }

        private static bool UpdateChartColor(ICustomStyle styles, CustomStyleProperty<Color> property, ref Color current)
        {
            if (!styles.TryGetValue(property, out var color) || color == current)
                return false;

            current = color;
            return true;
        }

        private void ShowHousingChart()
        {
            var blocks = housingBlocks;
            if (blocks.Length == 0)
            {
                housingChart.SetData(Array.Empty<BarChart.Series>(), Array.Empty<BarChart.Category>());
                return;
            }

            var series = blocks[0].Segments.Select(segment => new BarChart.Series(segment, SegmentColor(segment))).ToArray();
            var categories = blocks.Select(block =>
            {
                var source = $"{DashboardSummary.BlockName(block.Block)}\nBronfase: {block.SourcePhase ?? "onbekend"}";
                var values = block.Values.Select((value, i) => new BarChart.Value(value.Value,
                    $"{source}\n{block.Segments[i]}: {ChartNumber(value)} woningen")).ToArray();
                var details = string.Join("\n", block.Values.Select((value, i) => $"{block.Segments[i]}: {ChartNumber(value)}"));
                var label = block.Total.Status == DashboardSummary.ResultStatus.Complete ? block.Block : block.Block + "*";
                var totalLabel = block.Total.Value.HasValue ? housingChart.FormatValue(block.Total.Value.Value) : "\u2014";
                if (block.Total.Status == DashboardSummary.ResultStatus.Partial)
                    totalLabel += "*";
                return new BarChart.Category(label, values, $"{source}\nTotaal: {ChartNumber(block.Total)} woningen\n{details}", totalLabel);
            }).ToArray();
            housingChart.SetData(series, categories);
        }

        private Color SegmentColor(string segment)
        {
            switch (segment)
            {
                case "Sociaal": return socialColor;
                case "Middenhuur": return midRentColor;
                case "Vrije sector": return freeSectorColor;
            }

            // Keep additional segment colours stable when rows or filters change.
            uint hash = 2166136261;
            foreach (var character in segment.ToUpperInvariant())
                hash = unchecked((hash ^ character) * 16777619);
            return Color.HSVToRGB((hash % 360) / 360f, 0.65f, 0.7f);
        }

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
