using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Netherlands3D.Twin.DataSets;
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
        private const string AvailableDataExplanation = "Het overzicht is gebaseerd op de beschikbare projectgegevens.";
        private const string IncompleteDataExplanation = "*Onvolledig: alleen bekende gegevens tellen mee, ook voor percentages en gemiddelden.";
        private const string MissingGeometryExplanation = "**Geen gekoppeld 3D-object gevonden.";

        private readonly DashboardProjectOverview projectOverview;
        private readonly DashboardBlockOverview blockOverview;
        private readonly DashboardPhaseComparisonPanel phaseComparison;
        private readonly VisualElement programOverviewContent;
        private readonly TextCallOut emptyProgramSelection;
        private readonly TextCallOut phaseDataCoverage;
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
        private readonly ToggleButtonGroup planPhases;
        private DashboardData displayedData;
        private string selectedBlock;
        private bool hasMissingGeometry;
        private (DashboardSummary.PhaseTotals[] Phases, DashboardSummary.BreakdownRow[] Segments, DashboardSummary.BreakdownRow[] Rooms,
            DashboardSummary.BreakdownRow[] Categories, DashboardSummary.BreakdownRow[] SocialFacilities) summary;
        private (DashboardSummary.PhaseTotals Current, DashboardSummary.BreakdownRow[] Segments, DashboardSummary.BreakdownRow[] Categories) programSummary;
        private string[] blockPhases = Array.Empty<string>();
        private DashboardSummary.HousingBlockSummary[] allHousingBlocks = Array.Empty<DashboardSummary.HousingBlockSummary>();
        private DashboardSummary.HousingBlockSummary[] housingBlocks = Array.Empty<DashboardSummary.HousingBlockSummary>();
        private (string Label, DashboardSummary.NumericResult Value)[] housingFloorAreas = Array.Empty<(string Label, DashboardSummary.NumericResult Value)>();
        private (string Label, DashboardSummary.NumericResult Total, DashboardSummary.NumericResult[] Values)[] housingRooms = Array.Empty<(string Label, DashboardSummary.NumericResult Total, DashboardSummary.NumericResult[] Values)>();
        private (string Block, string Phase, DashboardSummary.NumericResult Value)[] nonHousingBlockPhases = Array.Empty<(string Block, string Phase, DashboardSummary.NumericResult Value)>();
        private DashboardSummary.NumericResult[] roomPhaseTotals = Array.Empty<DashboardSummary.NumericResult>();

        internal event Action ProgramSelectionChanged;

        private bool IsProgramSelectionEmpty => displayedData != null && selectedBlock != null && SelectedProgramPhase != null
            && !blockPhases.Contains(SelectedProgramPhase);

        internal string SelectedProgramPhase
        {
            get
            {
                if (phaseView.value)
                    return null;

                var selection = planPhases.value;
                for (var i = 1; i < selection.length; i++)
                {
                    if (selection[i])
                        return planPhases.GetButton(i).name;
                }

                return null;
            }
        }

        public DashboardHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            phaseView = this.Q<ToggleSwitch>("DashboardViewSwitch");
            planPhases = this.Q<ToggleButtonGroup>("PlanPhases");
            sidebarDataExplanation = this.Q<TextCallOut>("SidebarDataExplanation");
            projectOverview = this.Q<DashboardProjectOverview>();
            programOverviewContent = this.Q("ProgramOverviewContent");
            emptyProgramSelection = this.Q<TextCallOut>("EmptyProgramSelection");
            phaseDataCoverage = this.Q<TextCallOut>("PhaseDataCoverage");
            var phaseProgress = this.Q("PhaseProgressContent");
            var phaseChanges = this.Q("PhaseChangesPanel");
            blockOverview = this.Q<DashboardBlockOverview>();
            phaseComparison = phaseChanges.Q<DashboardPhaseComparisonPanel>();
            phaseComparison.ViewChanged += UpdateSidebarExplanation;
            housingChart = programOverviewContent.Q<BarChart>("HousingBlockChart");
            housingSegmentChart = programOverviewContent.Q<RingChart>("HousingSegmentChart");
            housingFloorAreaChart = programOverviewContent.Q<AreaComparisonChart>("HousingFloorAreaChart");
            housingRoomChart = programOverviewContent.Q<BarChart>("HousingRoomChart");
            nonHousingCategoryChart = programOverviewContent.Q<RingChart>("NonHousingCategoryChart");
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

            planPhases.RegisterValueChangedCallback(_ =>
            {
                RefreshSelection();
                UpdateSidebarExplanation();
            });

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
                phaseProgress.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                phaseChanges.EnableInClassList(UtilityClassConstants.HIDDEN, !showPhaseProgress);
                planPhases.EnableInClassList(UtilityClassConstants.HIDDEN, showPhaseProgress);
                RefreshSelection();
                phaseComparison.ResetView();
                schedule.Execute(ResetScrollPositions);
            }
        }

        private void ResetScrollPositions()
        {
            foreach (var scrollView in this.Q("Sidebar").Query<UnityEngine.UIElements.ScrollView>().ToList())
                scrollView.scrollOffset = Vector2.zero;
        }

        internal void SetData(DashboardData data)
        {
            SetData(data, data == null ? null : selectedBlock);
        }

        internal void SetData(DashboardData data, string block)
        {
            var dataChanged = !ReferenceEquals(displayedData, data);
            if (!dataChanged && string.Equals(selectedBlock, block, StringComparison.OrdinalIgnoreCase))
                return;

            displayedData = data;
            selectedBlock = block;
            if (dataChanged)
                RefreshDataset();
            RefreshSelection();
            if (dataChanged)
                phaseComparison.Refresh();
            else
                UpdateSidebarExplanation();
        }

        internal void SetBlock(string block)
        {
            SetData(displayedData, block);
        }

        private void RefreshDataset()
        {
            var data = displayedData;
            if (data != null)
            {
                var selection = planPhases.value;
                for (var i = 1; i < selection.length; i++)
                {
                    if (!selection[i] || data.AvailablePhases.Contains(planPhases.GetButton(i).name))
                        continue;

                    var current = new ToggleButtonGroupState(0, selection.length);
                    current[0] = true;
                    planPhases.SetValueWithoutNotify(current);
                    break;
                }
            }

            summary = DashboardSummary.Calculate(data);
            allHousingBlocks = DashboardSummary.CalculateHousingBlocks(data);
            nonHousingBlockPhases = DashboardSummary.CalculateNonHousingBlockPhases(data);
            roomPhaseTotals = summary.Phases.Select((phase, index) => DashboardSummary.SumResults(summary.Rooms.Select(row => row.Values[index]))).ToArray();
            RefreshCategoryColors();
            ShowPhaseCharts();
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
        }

        private void RefreshProjectOverview()
        {
            var phase = SelectedProgramPhase;
            var current = programSummary.Current;
            var phaseBlocks = DashboardSummary.CountBlocks(displayedData, phase);
            projectOverview.SetData(
                FormatNumber(current.Dwellings),
                displayedData == null ? null : phaseBlocks.ToString("#,0", NumberCulture),
                ChartLabel(current.GrossFloorArea),
                FormatShare(current.SocialDwellings, current.Dwellings), phase);
            phaseDataCoverage.Text = phase == null || displayedData == null ? string.Empty
                : $"{phase}: gegevens voor {phaseBlocks} van {DashboardSummary.CountBlocks(displayedData)} bouwblokken. Blokken zonder gegevens voor deze fase tellen niet mee.";
        }

        private void RefreshSelection()
        {
            var data = displayedData;
            var phase = SelectedProgramPhase;
            blockPhases = DashboardSummary.GetBlockPhases(data, selectedBlock);
            programSummary = selectedBlock == null && phase == null
                ? (summary.Phases?.LastOrDefault() ?? default, summary.Segments, summary.Categories)
                : DashboardSummary.CalculateProgramOverview(data, selectedBlock, phase);
            if (selectedBlock == null)
                RefreshProjectOverview();
            UpdatePhaseAvailability();
            var emptySelection = IsProgramSelectionEmpty;
            projectOverview.EnableInClassList(UtilityClassConstants.HIDDEN, selectedBlock != null);
            phaseDataCoverage.EnableInClassList(UtilityClassConstants.HIDDEN, selectedBlock != null || phase == null || data == null);
            blockOverview.EnableInClassList(UtilityClassConstants.HIDDEN, selectedBlock == null || emptySelection);
            programOverviewContent.EnableInClassList(UtilityClassConstants.HIDDEN, phaseView.value || emptySelection);
            emptyProgramSelection.Text = emptySelection ? $"Voor dit bouwblok zijn nog geen gegevens beschikbaar voor planfase {phase}." : string.Empty;
            emptyProgramSelection.EnableInClassList(UtilityClassConstants.HIDDEN, !emptySelection);
            if (phase != null)
                housingBlocks = DashboardSummary.CalculateHousingBlocks(data, phase, selectedBlock);
            else
                housingBlocks = selectedBlock == null ? allHousingBlocks
                    : allHousingBlocks.Where(item => string.Equals(item.Block, selectedBlock, StringComparison.OrdinalIgnoreCase)).ToArray();
            housingFloorAreas = DashboardSummary.CalculateHousingFloorAreas(data, selectedBlock, phase);
            housingRooms = DashboardSummary.CalculateHousingRooms(data, selectedBlock, phase);
            ShowHousingChart();
            ShowProgramCharts();
            var current = programSummary.Current;
            var sourcePhases = new[]
            {
                GetBlockSourcePhase(current.HousingSources, selectedBlock),
                GetBlockSourcePhase(current.NonHousingSources, selectedBlock)
            }.Where(source => source != null).Distinct();

            var titlePhase = phase ?? string.Join(" / ", sourcePhases);
            blockOverview.HeaderText = selectedBlock == null ? "Bouwblokoverzicht"
                : $"Overzicht {DashboardSummary.BlockName(selectedBlock)}" + (string.IsNullOrEmpty(titlePhase) ? "" : $" ({titlePhase})");
            blockOverview.TotalDwellings = FormatNumber(current.Dwellings);
            blockOverview.SocialDwellings = FormatNumber(current.SocialDwellings);
            blockOverview.MidRentDwellings = FormatNumber(current.MidRentDwellings);
            blockOverview.FreeSectorDwellings = FormatNumber(current.FreeSectorDwellings);
            blockOverview.AverageMinimumFloorArea = FormatNumber(current.AverageMinimumFloorArea, SquareMetres);
            blockOverview.AvailablePhases = string.Join(" / ", blockPhases);
            blockOverview.NonHousingGrossFloorArea = FormatNumber(current.GrossFloorArea, SquareMetres);
            ProgramSelectionChanged?.Invoke();
        }

        private void UpdatePhaseAvailability()
        {
            planPhases.SetEnabled(displayedData != null);
            for (var i = 1; i < planPhases.value.length; i++)
            {
                var button = planPhases.GetButton(i);
                var available = displayedData?.AvailablePhases.Contains(button.name) == true;
                button.SetEnabled(available);
                button.tooltip = available ? string.Empty : "Voor deze planfase zijn nog geen projectgegevens beschikbaar.";
            }
        }

        private static string GetBlockSourcePhase(DashboardSummary.PhaseSources sources, string block) =>
            block != null && sources != null && sources.BlockPhases.TryGetValue(block, out var phase) ? phase : null;

        internal void SetMissingGeometry(bool missing)
        {
            if (hasMissingGeometry == missing)
                return;

            hasMissingGeometry = missing;
            UpdateSidebarExplanation();
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
                SetSidebarExplanation(AvailableDataExplanation, values.Any(IsIncomplete));
                return;
            }

            if (IsProgramSelectionEmpty)
            {
                SetSidebarExplanation(AvailableDataExplanation, false);
                return;
            }

            var current = programSummary.Current;
            var incomplete = current.HousingSources?.HasData == true && (IsIncomplete(current.Dwellings) || IsIncomplete(current.AverageMinimumFloorArea))
                || current.NonHousingSources?.HasData == true && IsIncomplete(current.GrossFloorArea);

            var blocks = housingBlocks;
            incomplete |= blocks.Any(entry => IsIncomplete(entry.Total));
            incomplete |= housingFloorAreas.Any(entry => IsIncomplete(entry.Value)) || housingRooms.Any(entry => IsIncomplete(entry.Total));
            SetSidebarExplanation(AvailableDataExplanation, incomplete);
        }

        private static bool IsIncomplete(DashboardSummary.NumericResult result) =>
            result.Status == DashboardSummary.ResultStatus.Unknown || result.Status == DashboardSummary.ResultStatus.Partial;

        // Context text and completeness are supplied together so future filters can update one shared callout.
        private void SetSidebarExplanation(string context, bool hasIncompleteData)
        {
            var notices = string.Join(" ", new[]
            {
                hasIncompleteData ? IncompleteDataExplanation : null,
                displayedData != null && hasMissingGeometry ? MissingGeometryExplanation : null
            }.Where(text => !string.IsNullOrEmpty(text)));
            sidebarDataExplanation.Text = string.Join("\n\n", new[] { context, notices }
                .Where(text => !string.IsNullOrEmpty(text)));
            sidebarDataExplanation.EnableInClassList(UtilityClassConstants.HIDDEN, string.IsNullOrEmpty(sidebarDataExplanation.Text));
        }

        private void OnChartStylesResolved(CustomStyleResolvedEvent evt)
        {
            if (DashboardColors.Read(evt.customStyle, additionalColors))
            {
                RefreshCategoryColors();
                ShowHousingChart();
                ShowProgramCharts();
                ShowPhaseCharts();
            }
        }

        private void RefreshCategoryColors()
        {
            DashboardColors.SetSegmentColors((summary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>()).Select(row => row.Label), additionalColors);
            DashboardColors.SetRoomColors((summary.Rooms ?? Array.Empty<DashboardSummary.BreakdownRow>()).Select(row => row.Label), additionalColors);
            DashboardColors.SetPhaseColors(displayedData?.AvailablePhases ?? Array.Empty<string>(), additionalColors);
            phaseComparison.SetPhaseColors(additionalColors);
        }

        private void ShowHousingChart()
        {
            housingFloorAreaChart.SetData(housingFloorAreas.Select(entry =>
            {
                var text = AreaText(entry.Value);
                return new AreaComparisonChart.Item(entry.Label, entry.Value.Value, DashboardColors.GetSegmentColor(entry.Label, additionalColors),
                    $"Gemiddelde minimale\nwoonoppervlakte: {text}", text);
            }).ToArray());
            var total = programSummary.Current.Dwellings;
            var segments = programSummary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>();
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
                var percentage = FormatShare(value, total);
                var tooltip = $"{row.Label}: {ChartNumber(value)}{unit}" + (percentage == null ? "" : $"\n{percentage}");
                return new RingChart.Segment(row.Label, value.Value, colorForLabel(row.Label), tooltip, percentage);
            }).ToArray(), ChartLabel(total), $"Totaal: {ChartNumber(total)}{unit}");
        }

        private void ShowProgramCharts()
        {
            var programSegments = programSummary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>();
            housingRoomChart.SetData(programSegments.Select(row => new BarChart.Series(row.Label, DashboardColors.GetSegmentColor(row.Label, additionalColors))).ToArray(),
                housingRooms.Select(room => new BarChart.Category(room.Label, room.Values.Select((value, index) => new BarChart.Value(value.Value,
                    $"{room.Label}\n{programSegments[index].Label}: {ChartNumber(value)} woningen")),
                    $"{room.Label}\nTotaal: {ChartNumber(room.Total)} woningen", ChartLabel(room.Total, housingRoomChart))).ToArray());
            ShowRingChart(nonHousingCategoryChart, programSummary.Categories ?? Array.Empty<DashboardSummary.BreakdownRow>(),
                programSummary.Current.GrossFloorArea, SquareMetres, SegmentColor);
        }

        private void ShowPhaseCharts()
        {
            var phases = summary.Phases ?? Array.Empty<DashboardSummary.PhaseTotals>();
            var segments = summary.Segments ?? Array.Empty<DashboardSummary.BreakdownRow>();
            var rooms = summary.Rooms ?? Array.Empty<DashboardSummary.BreakdownRow>();
            var categories = summary.Categories ?? Array.Empty<DashboardSummary.BreakdownRow>();
            ShowPhaseChart(housingPhaseChart, segments, phases.Select(phase => phase.Dwellings).ToArray(), " woningen", label => DashboardColors.GetSegmentColor(label, additionalColors));
            ShowPhaseChart(housingRoomPhaseChart, rooms, roomPhaseTotals, " woningen", label => DashboardColors.GetRoomColor(label, additionalColors));
            ShowPhaseChart(nonHousingPhaseChart, categories, phases.Select(phase => phase.GrossFloorArea).ToArray(), SquareMetres, SegmentColor);
            housingFloorAreaPhaseChart.SetData(phases.Select(phase =>
            {
                var text = AreaText(phase.AverageMinimumFloorArea);
                return new AreaComparisonChart.Item(phase.Phase, phase.AverageMinimumFloorArea.Value, DashboardColors.GetPhaseColor(phase.Phase, additionalColors),
                    $"{phase.Phase}\nGemiddelde minimale\nwoonoppervlakte: {text}", text);
            }).ToArray());

            // Each bar represents one block-phase value; phases are never stacked or added together.
            nonHousingBlockPhaseChart.SetData(DashboardData.Phases.Select(phase => new BarChart.Series(phase, DashboardColors.GetPhaseColor(phase, additionalColors))).ToArray(),
                nonHousingBlockPhases.Select(row => new BarChart.Category($"{row.Block}\n{row.Phase}",
                    DashboardData.Phases.Select(phase => new BarChart.Value(phase == row.Phase ? row.Value.Value : null,
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

        private static string FormatShare(DashboardSummary.NumericResult value, DashboardSummary.NumericResult total)
        {
            var share = DashboardSummary.CalculateSegmentShare(value, total);
            return share.Value.HasValue ? share.Value.Value.ToString("0.#%", NumberCulture) + (IsIncomplete(share) ? "*" : "") : null;
        }

        private static string FormatSegment(DashboardSummary.NumericResult value, DashboardSummary.NumericResult phaseTotal)
        {
            var count = FormatNumber(value);
            var share = FormatShare(value, phaseTotal);
            return share == null ? count : $"{count} ({share})";
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
