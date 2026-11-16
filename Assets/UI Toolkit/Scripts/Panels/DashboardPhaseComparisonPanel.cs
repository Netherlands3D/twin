using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardPhaseComparisonPanel : VisualElement
    {
        private const string EmptyValue = "\u2014";
        private const string ValueClass = "dashboard-phase-comparison__value";
        private const string PhaseCellClass = "dashboard-phase-comparison__phase-cell";
        private const string LabelClass = "dashboard-phase-comparison__label";
        private const string RowClass = "dashboard-phase-comparison__row";
        private const int PhaseCount = 4;

        private enum ComparisonTab
        {
            KeyFigures,
            ProgramBreakdown,
            Rooms,
            Blocks
        }

        private readonly Header header;
        private readonly ToggleButtonGroup tabs;
        private readonly Button keyFiguresButton;
        private readonly Button programBreakdownButton;
        private readonly Button roomsButton;
        private readonly Button blockTotalsButton;
        private readonly UnityEngine.UIElements.ScrollView scrollView;
        private readonly VisualElement columnHeadings;
        private readonly Label rowHeading;
        private readonly VisualElement tableRows;
        private readonly Label emptyBreakdown;
        private string[] totalDwellingsCells;
        private string[] dwellingChangesCells;
        private string[] averageMinimumFloorAreaCells;
        private string[] minimumFloorAreaChangesCells;
        private string[] totalGrossFloorAreaCells;
        private string[] grossFloorAreaChangesCells;

        private bool nonHousing;
        private string segmentBreakdown;
        private string categoryBreakdown;
        private string roomBreakdown;
        private string blockBreakdown;
        private string nonHousingBlockBreakdown;
        private (IReadOnlyList<(string Label, string[] Cells)> Segments, IReadOnlyList<(string Label, string[] Cells)> Rooms,
            IReadOnlyList<(string Label, string[] Cells)> Categories, IReadOnlyList<(string Label, string[] Cells)> HousingBlocks,
            IReadOnlyList<(string Label, string[] Cells)> NonHousingBlocks) loadedBreakdowns;

        [UxmlAttribute("non-housing")]
        public bool NonHousing
        {
            get => nonHousing;
            set
            {
                nonHousing = value;
                header.LabelText = value ? "Fasevergelijking \u2013 Niet-wonen" : "Fasevergelijking \u2013 Wonen";
                programBreakdownButton.LabelText = value ? "Categorieën" : "Segmenten";
                roomsButton.EnableInClassList(UtilityClassConstants.HIDDEN, value);
                roomsButton.SetEnabled(!value);
                blockTotalsButton.LabelText = value ? "BVO" : "Woningen";

                if (value && GetSelectedTab(tabs.value) == ComparisonTab.Rooms)
                    SelectKeyFigures();

                ShowTab(GetSelectedTab(tabs.value));
            }
        }

        [UxmlAttribute("total-dwellings")]
        public string TotalDwellings
        {
            get => GetPhaseValues(totalDwellingsCells);
            set => SetPhaseValues(ref totalDwellingsCells, value);
        }

        [UxmlAttribute("dwelling-changes")]
        public string DwellingChanges
        {
            get => GetPhaseValues(dwellingChangesCells);
            set => SetPhaseValues(ref dwellingChangesCells, value);
        }

        [UxmlAttribute("average-minimum-floor-area")]
        public string AverageMinimumFloorArea
        {
            get => GetPhaseValues(averageMinimumFloorAreaCells);
            set => SetPhaseValues(ref averageMinimumFloorAreaCells, value);
        }

        [UxmlAttribute("minimum-floor-area-changes")]
        public string MinimumFloorAreaChanges
        {
            get => GetPhaseValues(minimumFloorAreaChangesCells);
            set => SetPhaseValues(ref minimumFloorAreaChangesCells, value);
        }

        [UxmlAttribute("total-gross-floor-area")]
        public string TotalGrossFloorArea
        {
            get => GetPhaseValues(totalGrossFloorAreaCells);
            set => SetPhaseValues(ref totalGrossFloorAreaCells, value);
        }

        [UxmlAttribute("gross-floor-area-changes")]
        public string GrossFloorAreaChanges
        {
            get => GetPhaseValues(grossFloorAreaChangesCells);
            set => SetPhaseValues(ref grossFloorAreaChangesCells, value);
        }

        [UxmlAttribute("segment-breakdown")]
        public string SegmentBreakdown
        {
            get => segmentBreakdown;
            set => SetBreakdown(ref segmentBreakdown, value, ComparisonTab.ProgramBreakdown);
        }

        [UxmlAttribute("category-breakdown")]
        public string CategoryBreakdown
        {
            get => categoryBreakdown;
            set => SetBreakdown(ref categoryBreakdown, value, ComparisonTab.ProgramBreakdown);
        }

        [UxmlAttribute("room-breakdown")]
        public string RoomBreakdown
        {
            get => roomBreakdown;
            set => SetBreakdown(ref roomBreakdown, value, ComparisonTab.Rooms);
        }

        [UxmlAttribute("block-breakdown")]
        public string BlockBreakdown
        {
            get => blockBreakdown;
            set => SetBreakdown(ref blockBreakdown, value, ComparisonTab.Blocks);
        }

        [UxmlAttribute("non-housing-block-breakdown")]
        public string NonHousingBlockBreakdown
        {
            get => nonHousingBlockBreakdown;
            set => SetBreakdown(ref nonHousingBlockBreakdown, value, ComparisonTab.Blocks);
        }

        public DashboardPhaseComparisonPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            header = this.Q<Header>("ComparisonHeader");
            tabs = this.Q<ToggleButtonGroup>("ComparisonTabs");
            keyFiguresButton = tabs.Q<Button>("KeyFiguresTab");
            programBreakdownButton = tabs.Q<Button>("ProgramBreakdownTab");
            roomsButton = tabs.Q<Button>("RoomsTab");
            blockTotalsButton = tabs.Q<Button>("BlockTotalsTab");
            scrollView = this.Q<UnityEngine.UIElements.ScrollView>("ComparisonScrollView");
            columnHeadings = this.Q("ColumnHeadings");
            rowHeading = this.Q<Label>("RowHeading");
            tableRows = this.Q("BreakdownRows");
            emptyBreakdown = this.Q<Label>("EmptyBreakdown");

            SelectKeyFigures();

            scrollView.verticalScroller.valueChanged += offset => columnHeadings.style.translate = new Translate(0, offset);

            tabs.RegisterValueChangedCallback(evt =>
            {
                ShowTab(GetSelectedTab(evt.newValue));
                scrollView.verticalScroller.value = 0;
            });

            ShowTab(GetSelectedTab(tabs.value));
        }

        internal void SetKeyFigureData(string dwellings, string dwellingDifferences, string minimumFloorArea, string floorAreaDifferences, string grossFloorArea, string grossFloorAreaDifferences)
        {
            totalDwellingsCells = dwellings?.Split('|');
            dwellingChangesCells = dwellingDifferences?.Split('|');
            averageMinimumFloorAreaCells = minimumFloorArea?.Split('|');
            minimumFloorAreaChangesCells = floorAreaDifferences?.Split('|');
            totalGrossFloorAreaCells = grossFloorArea?.Split('|');
            grossFloorAreaChangesCells = grossFloorAreaDifferences?.Split('|');

            if (GetSelectedTab(tabs.value) == ComparisonTab.KeyFigures)
                ShowTab(ComparisonTab.KeyFigures);
        }

        internal void SetBreakdownData((string Label, string[] Cells)[] segments, (string Label, string[] Cells)[] rooms,
            (string Label, string[] Cells)[] categories, (string Label, string[] Cells)[] housingBlocks, (string Label, string[] Cells)[] nonHousingBlocks)
        {
            loadedBreakdowns = (segments, rooms, categories, housingBlocks, nonHousingBlocks);
            var selectedTab = GetSelectedTab(tabs.value);
            if (selectedTab != ComparisonTab.KeyFigures)
                ShowTab(selectedTab);
        }

        private void SelectKeyFigures()
        {
            var selection = new ToggleButtonGroupState(0, tabs.childCount);
            for (var i = 0; i < selection.length; i++)
            {
                if (tabs.GetButton(i) != keyFiguresButton)
                    continue;

                selection[i] = true;
                tabs.SetValueWithoutNotify(selection);
                return;
            }
        }

        private ComparisonTab GetSelectedTab(ToggleButtonGroupState selection)
        {
            for (var i = 0; i < selection.length; i++)
            {
                if (!selection[i])
                    continue;

                var button = tabs.GetButton(i);
                if (button == programBreakdownButton)
                    return ComparisonTab.ProgramBreakdown;
                if (button == roomsButton)
                    return ComparisonTab.Rooms;
                if (button == blockTotalsButton)
                    return ComparisonTab.Blocks;

                break;
            }

            return ComparisonTab.KeyFigures;
        }

        private void ShowTab(ComparisonTab tab)
        {
            var (heading, previewRows, loadedRows) = tab switch
            {
                ComparisonTab.ProgramBreakdown => nonHousing
                    ? ("BVO per categorie (m\u00b2)", categoryBreakdown, loadedBreakdowns.Categories)
                    : ("Woningen per segment", segmentBreakdown, loadedBreakdowns.Segments),
                ComparisonTab.Rooms => ("Woningen per kamertype", roomBreakdown, loadedBreakdowns.Rooms),
                ComparisonTab.Blocks => nonHousing
                    ? ("BVO per bouwblok (m\u00b2)", nonHousingBlockBreakdown, loadedBreakdowns.NonHousingBlocks)
                    : ("Woningen per bouwblok", blockBreakdown, loadedBreakdowns.HousingBlocks),
                _ => ("Vergelijking", null, GetKeyFigureRows())
            };

            rowHeading.text = heading;
            var hasData = RenderRows(loadedRows ?? ParsePreviewRows(previewRows));
            scrollView.EnableInClassList(UtilityClassConstants.HIDDEN, !hasData);
            emptyBreakdown.EnableInClassList(UtilityClassConstants.HIDDEN, hasData);
        }

        private IReadOnlyList<(string Label, string[] Cells)> GetKeyFigureRows()
        {
            if (nonHousing)
            {
                return new (string Label, string[] Cells)[]
                {
                    ("Totaal BVO", totalGrossFloorAreaCells),
                    ("Verschil in BVO (t.o.v. de vorige fase)", grossFloorAreaChangesCells)
                };
            }

            return new (string Label, string[] Cells)[]
            {
                ("Totaal woningen", totalDwellingsCells),
                ("Verschil in woningen (t.o.v. de vorige fase)", dwellingChangesCells),
                ("Gemiddelde minimale woonoppervlakte", averageMinimumFloorAreaCells),
                ("Verschil in woonoppervlakte (t.o.v. de vorige fase)", minimumFloorAreaChangesCells)
            };
        }

        private void SetBreakdown(ref string destination, string value, ComparisonTab tab)
        {
            destination = value;
            if (GetSelectedTab(tabs.value) == tab)
                ShowTab(tab);
        }

        private static (string Label, string[] Cells)[] ParsePreviewRows(string rows)
        {
            if (string.IsNullOrWhiteSpace(rows))
                return System.Array.Empty<(string Label, string[] Cells)>();

            return rows.Split(';').Where(row => !string.IsNullOrWhiteSpace(row))
                .Select(row => row.Split('|'))
                .Select(columns => (columns[0].Trim(), columns.Skip(1).ToArray())).ToArray();
        }

        private bool RenderRows(IEnumerable<(string Label, string[] Cells)> rows)
        {
            tableRows.Clear();
            var hasData = false;

            foreach (var (rowTitle, cells) in rows)
            {
                var row = new VisualElement();
                row.AddToClassList(RowClass);

                var label = new Label(rowTitle);
                label.AddToClassList(LabelClass);
                row.Add(label);

                for (var i = 0; i < PhaseCount; i++)
                {
                    var phaseValue = GetPhaseValue(cells, i);
                    hasData |= phaseValue != EmptyValue;

                    var cell = new VisualElement();
                    cell.AddToClassList(ValueClass);
                    cell.AddToClassList(PhaseCellClass);

                    var value = new Label(phaseValue);
                    value.AddToClassList("text-header");
                    cell.Add(value);
                    row.Add(cell);
                }

                tableRows.Add(row);
            }

            return hasData;
        }

        private static string GetPhaseValues(string[] cells) => string.Join(" | ", Enumerable.Range(0, PhaseCount).Select(i => GetPhaseValue(cells, i)));

        private void SetPhaseValues(ref string[] cells, string values)
        {
            cells = values?.Split('|');

            if (GetSelectedTab(tabs.value) == ComparisonTab.KeyFigures)
                ShowTab(ComparisonTab.KeyFigures);
        }

        private static string GetPhaseValue(string[] values, int index)
        {
            var value = values != null && index < values.Length ? values[index].Trim() : null;
            return string.IsNullOrEmpty(value) ? EmptyValue : value;
        }
    }
}
