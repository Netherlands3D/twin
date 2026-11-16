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
        private readonly Button programBreakdownButton;
        private readonly Button roomsButton;
        private readonly Button blockTotalsButton;
        private readonly UnityEngine.UIElements.ScrollView scrollView;
        private readonly VisualElement columnHeadings;
        private readonly Label rowHeading;
        private readonly VisualElement breakdownRows;
        private readonly Label emptyBreakdown;
        private readonly VisualElement housingRows;
        private readonly VisualElement nonHousingRows;
        private readonly List<Label> totalDwellingsCells;
        private readonly List<Label> dwellingChangesCells;
        private readonly List<Label> averageMinimumFloorAreaCells;
        private readonly List<Label> minimumFloorAreaChangesCells;
        private readonly List<Label> totalGrossFloorAreaCells;
        private readonly List<Label> grossFloorAreaChangesCells;

        private bool nonHousing;
        private string segmentBreakdown;
        private string categoryBreakdown;
        private string roomBreakdown;
        private string blockBreakdown;
        private string nonHousingBlockBreakdown;

        [UxmlAttribute("non-housing")]
        public bool NonHousing
        {
            get => nonHousing;
            set
            {
                nonHousing = value;
                header.LabelText = value ? "Fasevergelijking niet-wonen" : "Fasevergelijking wonen";
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
            set => SetPhaseValues(totalDwellingsCells, value);
        }

        [UxmlAttribute("dwelling-changes")]
        public string DwellingChanges
        {
            get => GetPhaseValues(dwellingChangesCells);
            set => SetPhaseValues(dwellingChangesCells, value);
        }

        [UxmlAttribute("average-minimum-floor-area")]
        public string AverageMinimumFloorArea
        {
            get => GetPhaseValues(averageMinimumFloorAreaCells);
            set => SetPhaseValues(averageMinimumFloorAreaCells, value);
        }

        [UxmlAttribute("minimum-floor-area-changes")]
        public string MinimumFloorAreaChanges
        {
            get => GetPhaseValues(minimumFloorAreaChangesCells);
            set => SetPhaseValues(minimumFloorAreaChangesCells, value);
        }

        [UxmlAttribute("total-gross-floor-area")]
        public string TotalGrossFloorArea
        {
            get => GetPhaseValues(totalGrossFloorAreaCells);
            set => SetPhaseValues(totalGrossFloorAreaCells, value);
        }

        [UxmlAttribute("gross-floor-area-changes")]
        public string GrossFloorAreaChanges
        {
            get => GetPhaseValues(grossFloorAreaChangesCells);
            set => SetPhaseValues(grossFloorAreaChangesCells, value);
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
            programBreakdownButton = this.Q<Button>("ProgramBreakdownTab");
            roomsButton = this.Q<Button>("RoomsTab");
            blockTotalsButton = this.Q<Button>("BlockTotalsTab");
            scrollView = this.Q<UnityEngine.UIElements.ScrollView>("ComparisonScrollView");
            columnHeadings = this.Q("ColumnHeadings");
            rowHeading = this.Q<Label>("RowHeading");
            breakdownRows = this.Q("BreakdownRows");
            emptyBreakdown = this.Q<Label>("EmptyBreakdown");
            housingRows = this.Q("HousingRows");
            nonHousingRows = this.Q("NonHousingRows");
            totalDwellingsCells = GetValueCells("TotalDwellingsRow");
            dwellingChangesCells = GetValueCells("DwellingChangesRow");
            averageMinimumFloorAreaCells = GetValueCells("AverageMinimumFloorAreaRow");
            minimumFloorAreaChangesCells = GetValueCells("MinimumFloorAreaChangesRow");
            totalGrossFloorAreaCells = GetValueCells("TotalGrossFloorAreaRow");
            grossFloorAreaChangesCells = GetValueCells("GrossFloorAreaChangesRow");

            if (tabs.value.length == 0 || !tabs.value[(int)ComparisonTab.KeyFigures])
                SelectKeyFigures();

            scrollView.verticalScroller.valueChanged += offset => columnHeadings.style.translate = new Translate(0, offset);

            tabs.RegisterValueChangedCallback(evt =>
            {
                ShowTab(GetSelectedTab(evt.newValue));
                scrollView.verticalScroller.value = 0;
            });

            ShowTab(GetSelectedTab(tabs.value));
        }

        private void SelectKeyFigures()
        {
            var selection = new ToggleButtonGroupState(0, tabs.childCount);
            selection[(int)ComparisonTab.KeyFigures] = true;
            tabs.SetValueWithoutNotify(selection);
        }

        private static ComparisonTab GetSelectedTab(ToggleButtonGroupState selection)
        {
            for (var i = 0; i < selection.length; i++)
            {
                if (selection[i])
                    return (ComparisonTab)i;
            }

            return ComparisonTab.KeyFigures;
        }

        private void ShowTab(ComparisonTab tab)
        {
            var showKeyFigures = tab == ComparisonTab.KeyFigures;
            housingRows.EnableInClassList(UtilityClassConstants.HIDDEN, !showKeyFigures || nonHousing);
            nonHousingRows.EnableInClassList(UtilityClassConstants.HIDDEN, !showKeyFigures || !nonHousing);
            breakdownRows.EnableInClassList(UtilityClassConstants.HIDDEN, showKeyFigures);

            var (heading, rows) = tab switch
            {
                ComparisonTab.ProgramBreakdown => nonHousing ? ("Categorie", categoryBreakdown) : ("Woonsegment", segmentBreakdown),
                ComparisonTab.Rooms => ("Kamertype", roomBreakdown),
                ComparisonTab.Blocks => ("Bouwblok", nonHousing ? nonHousingBlockBreakdown : blockBreakdown),
                _ => ("Indicator", null)
            };

            rowHeading.text = heading;
            if (!showKeyFigures)
                PopulateBreakdown(rows);

            var visibleRows = showKeyFigures ? (nonHousing ? nonHousingRows : housingRows) : breakdownRows;
            var hasData = visibleRows.Query<Label>(className: ValueClass).ToList().Any(cell => cell.text != EmptyValue);
            scrollView.EnableInClassList(UtilityClassConstants.HIDDEN, !hasData);
            emptyBreakdown.EnableInClassList(UtilityClassConstants.HIDDEN, hasData);
        }

        private void SetBreakdown(ref string destination, string value, ComparisonTab tab)
        {
            destination = value;
            if (GetSelectedTab(tabs.value) == tab)
                ShowTab(tab);
        }

        // A breakdown has one row per semicolon and one phase value per pipe after the label.
        private void PopulateBreakdown(string rows)
        {
            breakdownRows.Clear();

            if (!string.IsNullOrWhiteSpace(rows))
            {
                foreach (var rowText in rows.Split(';'))
                {
                    if (string.IsNullOrWhiteSpace(rowText))
                        continue;

                    var columns = rowText.Split('|');
                    var row = new VisualElement();
                    row.AddToClassList(RowClass);

                    var label = new Label(columns[0].Trim());
                    label.AddToClassList(LabelClass);
                    row.Add(label);

                    for (var i = 0; i < PhaseCount; i++)
                    {
                        var value = new Label(GetPhaseValue(columns, i + 1));
                        value.AddToClassList(ValueClass);
                        value.AddToClassList("text-header");
                        row.Add(value);
                    }

                    breakdownRows.Add(row);
                }
            }

        }

        private List<Label> GetValueCells(string rowName) => this.Q(rowName).Query<Label>(className: ValueClass).ToList();

        private static string GetPhaseValues(List<Label> cells) => string.Join(" | ", cells.Select(cell => cell.text));

        private void SetPhaseValues(List<Label> cells, string values)
        {
            var phaseValues = values?.Split('|');
            for (var i = 0; i < cells.Count; i++)
                cells[i].text = GetPhaseValue(phaseValues, i);

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
