using System;
using System.Collections.Generic;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
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

        internal enum ComparisonTab
        {
            KeyFigures,
            Segments,
            Categories,
            SocialFacilities,
            Rooms
        }

        internal ComparisonTab SelectedTab => GetSelectedTab(tabs.value);
        internal event Action ViewChanged;

        private readonly Header header;
        private readonly ToggleButtonGroup tabs;
        private readonly Button keyFiguresButton;
        private readonly Button segmentsButton;
        private readonly Button categoriesButton;
        private readonly Button socialFacilitiesButton;
        private readonly Button roomsButton;
        private readonly UnityEngine.UIElements.ScrollView scrollView;
        private readonly VisualElement columnHeadings;
        private readonly List<VisualElement> phaseHeadings;
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
        private bool hasHousingData;
        private bool hasNonHousingData;
        private string[] housingSources;
        private string[] nonHousingSources;
        private (IReadOnlyList<(string Label, string[] Cells)> Segments, IReadOnlyList<(string Label, string[] Cells)> Rooms,
            IReadOnlyList<(string Label, string[] Cells)> Categories, IReadOnlyList<(string Label, string[] Cells)> SocialFacilities) loadedBreakdowns;

        public bool NonHousing
        {
            get => nonHousing;
            set
            {
                nonHousing = value;
                header.LabelText = value ? "Gebiedsprogramma \u2013 Niet-wonen" : "Gebiedsprogramma \u2013 Wonen";
                SetTabVisible(segmentsButton, !value);
                SetTabVisible(roomsButton, !value);
                SetTabVisible(categoriesButton, value);
                SetTabVisible(socialFacilitiesButton, value);

                ResetView();
            }
        }

        public DashboardPhaseComparisonPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            header = this.Q<Header>("ComparisonHeader");
            tabs = this.Q<ToggleButtonGroup>("ComparisonTabs");
            keyFiguresButton = tabs.Q<Button>("KeyFiguresTab");
            segmentsButton = tabs.Q<Button>("ProgramBreakdownTab");
            categoriesButton = tabs.Q<Button>("CategoriesTab");
            socialFacilitiesButton = tabs.Q<Button>("SocialFacilitiesTab");
            roomsButton = tabs.Q<Button>("RoomsTab");
            scrollView = this.Q<UnityEngine.UIElements.ScrollView>("ComparisonScrollView");
            columnHeadings = this.Q("ColumnHeadings");
            phaseHeadings = columnHeadings.Query(className: PhaseCellClass).ToList();
            rowHeading = this.Q<Label>("RowHeading");
            tableRows = this.Q("BreakdownRows");
            emptyBreakdown = this.Q<Label>("EmptyBreakdown");

            scrollView.verticalScroller.valueChanged += offset => columnHeadings.style.translate = new Translate(0, offset);

            tabs.RegisterValueChangedCallback(evt =>
            {
                ShowTab(GetSelectedTab(evt.newValue));
                scrollView.verticalScroller.value = 0;
            });

            NonHousing = false;
        }

        internal void SetPhaseColors(IReadOnlyDictionary<string, Color> colors)
        {
            foreach (var heading in phaseHeadings)
            {
                var badge = heading.Q<Label>();
                badge.style.backgroundColor = DashboardColors.GetPhaseColor(badge.text, colors);
            }
        }

        internal void SetKeyFigureData(string[] dwellings, string[] dwellingDifferences, string[] minimumFloorArea, string[] floorAreaDifferences, string[] grossFloorArea, string[] grossFloorAreaDifferences)
        {
            totalDwellingsCells = dwellings;
            dwellingChangesCells = dwellingDifferences;
            averageMinimumFloorAreaCells = minimumFloorArea;
            minimumFloorAreaChangesCells = floorAreaDifferences;
            totalGrossFloorAreaCells = grossFloorArea;
            grossFloorAreaChangesCells = grossFloorAreaDifferences;
        }

        internal void SetBreakdownData((string Label, string[] Cells)[] segments, (string Label, string[] Cells)[] rooms,
            (string Label, string[] Cells)[] categories, (string Label, string[] Cells)[] socialFacilities)
        {
            loadedBreakdowns = (segments, rooms, categories, socialFacilities);
        }

        internal void SetSourceData(bool housingAvailable, bool nonHousingAvailable, string[] housingPhaseSources, string[] nonHousingPhaseSources)
        {
            hasHousingData = housingAvailable;
            hasNonHousingData = nonHousingAvailable;
            housingSources = housingPhaseSources;
            nonHousingSources = nonHousingPhaseSources;
        }

        internal void Refresh() => ShowTab(GetSelectedTab(tabs.value));

        internal void ResetView()
        {
            SelectTab(keyFiguresButton);
            ShowTab(ComparisonTab.KeyFigures);
            scrollView.scrollOffset = Vector2.zero;
        }

        private static void SetTabVisible(Button button, bool visible)
        {
            button.EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
            button.SetEnabled(visible);
        }

        private void SelectTab(Button button)
        {
            var selection = new ToggleButtonGroupState(0, tabs.childCount);
            for (var i = 0; i < selection.length; i++)
            {
                if (tabs.GetButton(i) != button)
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
                if (button == segmentsButton)
                    return ComparisonTab.Segments;
                if (button == categoriesButton)
                    return ComparisonTab.Categories;
                if (button == socialFacilitiesButton)
                    return ComparisonTab.SocialFacilities;
                if (button == roomsButton)
                    return ComparisonTab.Rooms;

                break;
            }

            return ComparisonTab.KeyFigures;
        }

        private void ShowTab(ComparisonTab tab)
        {
            var (heading, rows) = tab switch
            {
                ComparisonTab.Segments => ("Woningen per segment", loadedBreakdowns.Segments),
                ComparisonTab.Categories => ("BVO per categorie (m\u00b2)", loadedBreakdowns.Categories),
                ComparisonTab.SocialFacilities => ("BVO per subcategorie", loadedBreakdowns.SocialFacilities),
                ComparisonTab.Rooms => ("Woningen per kamertype", loadedBreakdowns.Rooms),
                _ => ("Vergelijking", GetKeyFigureRows())
            };

            rowHeading.text = heading;
            RenderRows(rows ?? Array.Empty<(string Label, string[] Cells)>());
            var hasData = tab == ComparisonTab.KeyFigures ? (nonHousing ? hasNonHousingData : hasHousingData) : rows?.Count > 0;
            var sources = nonHousing ? nonHousingSources : housingSources;
            for (var i = 0; i < phaseHeadings.Count; i++)
                phaseHeadings[i].tooltip = sources != null && i < sources.Length ? sources[i] : string.Empty;

            scrollView.EnableInClassList(UtilityClassConstants.HIDDEN, !hasData);
            emptyBreakdown.EnableInClassList(UtilityClassConstants.HIDDEN, hasData);
            ViewChanged?.Invoke();
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

        private void RenderRows(IEnumerable<(string Label, string[] Cells)> rows)
        {
            tableRows.Clear();

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
        }

        private static string GetPhaseValue(string[] values, int index)
        {
            var value = values != null && index < values.Length ? values[index]?.Trim() : null;
            return string.IsNullOrEmpty(value) ? EmptyValue : value;
        }
    }
}
