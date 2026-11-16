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
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardHUD : VisualElement
    {
        private static readonly CultureInfo NumberCulture = CultureInfo.GetCultureInfo("nl-NL");
        private const string SquareMetres = " m\u00b2";

        private readonly DashboardBlockOverview blockOverview;
        private readonly DashboardPhaseComparisonPanel phaseComparison;
        private Layers layers;
        private ProjectDataHandler projectLoader;
        private DashboardData displayedData;

        public DashboardHUD()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            var phaseView = this.Q<ToggleSwitch>("DashboardViewSwitch");
            var programOverview = this.Q("ProgramOverviewContent");
            var phaseProgress = this.Q("PhaseProgressContent");
            var phaseChanges = this.Q("PhaseChangesPanel");
            blockOverview = programOverview.Q<DashboardBlockOverview>();
            phaseComparison = phaseChanges.Q<DashboardPhaseComparisonPanel>();

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
            var phases = DashboardSummary.CalculatePhases(data);

            phaseComparison.SetKeyFigureData(
                phases.Select(phase => FormatNumber(phase.Dwellings)).ToArray(),
                FormatChanges(phases, phase => phase.Dwellings, ""),
                phases.Select(phase => FormatNumber(phase.AverageMinimumFloorArea, SquareMetres)).ToArray(),
                FormatChanges(phases, phase => phase.AverageMinimumFloorArea, SquareMetres),
                phases.Select(phase => FormatNumber(phase.GrossFloorArea, SquareMetres)).ToArray(),
                FormatChanges(phases, phase => phase.GrossFloorArea, SquareMetres));

            var breakdowns = DashboardSummary.CalculateBreakdowns(data);
            phaseComparison.SetBreakdownData(
                breakdowns.Segments.Select(row => (row.Label, row.Values.Select((value, i) => FormatSegment(value, phases[i].Dwellings)).ToArray())).ToArray(),
                FormatBreakdown(breakdowns.Rooms),
                FormatBreakdown(breakdowns.Categories, SquareMetres),
                FormatBreakdown(breakdowns.SocialFacilities, SquareMetres));

            var block = DashboardSummary.FirstBlockWithData(data);
            var blockPhases = block == null ? Array.Empty<DashboardSummary.PhaseTotals>() : DashboardSummary.CalculatePhases(data, block);
            var availablePhases = blockPhases.Where(phase => phase.HasData).ToArray();
            var currentPhase = availablePhases.LastOrDefault();

            blockOverview.HeaderText = currentPhase.HasData ? $"Overzicht {DashboardSummary.BlockName(block)} ({currentPhase.Phase})" : "Bouwblokoverzicht";
            blockOverview.TotalDwellings = FormatNumber(currentPhase.Dwellings);
            blockOverview.SocialDwellings = FormatNumber(currentPhase.SocialDwellings);
            blockOverview.MidRentDwellings = FormatNumber(currentPhase.MidRentDwellings);
            blockOverview.FreeSectorDwellings = FormatNumber(currentPhase.FreeSectorDwellings);
            blockOverview.AverageMinimumFloorArea = FormatNumber(currentPhase.AverageMinimumFloorArea, SquareMetres);
            blockOverview.AvailablePhases = string.Join(" / ", availablePhases.Select(phase => phase.Phase));
            blockOverview.NonHousingGrossFloorArea = FormatNumber(currentPhase.GrossFloorArea, SquareMetres);
        }

        private static (string Label, string[] Cells)[] FormatBreakdown(DashboardSummary.BreakdownRow[] rows, string unit = "")
        {
            return rows.Select(row => (row.Label, row.Values.Select(value => FormatNumber(value, unit)).ToArray())).ToArray();
        }

        private static string FormatSegment(decimal? value, long? phaseTotal)
        {
            var count = FormatNumber(value);
            var share = DashboardSummary.CalculateSegmentShare(value, phaseTotal);
            return share.HasValue ? $"{count} ({share.Value.ToString("0%", NumberCulture)})" : count;
        }

        private static string FormatNumber(decimal? value, string unit = "")
        {
            return value.HasValue ? value.Value.ToString("#,0.##", NumberCulture) + unit : string.Empty;
        }

        private static string[] FormatChanges(DashboardSummary.PhaseTotals[] phases, Func<DashboardSummary.PhaseTotals, decimal?> selectValue,
            string unit)
        {
            var changes = DashboardSummary.CalculateChanges(phases, selectValue);
            return changes.Select(change =>
            {
                if (!change.Difference.HasValue)
                    return string.Empty;

                var difference = change.Difference.Value.ToString("+#,0.##;-#,0.##;0", NumberCulture) + unit;
                return change.RelativeChange.HasValue
                    ? $"{difference} ({change.RelativeChange.Value.ToString("+0%;-0%;0%", NumberCulture)})"
                    : difference;
            }).ToArray();
        }
    }
}
