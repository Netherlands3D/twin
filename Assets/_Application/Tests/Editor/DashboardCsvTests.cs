using System;
using System.IO;
using System.Linq;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Tests
{
    public class DashboardCsvTests
    {
        [Test]
        public void ReadsDashboardDataAndReportsThePhysicalLineOfAnInvalidRow()
        {
            using var reader = File.OpenText(GetFixturePath("DashboardCsvSample.csv"));
            var data = DashboardCsv.Read(reader);

            Assert.That(data.SourceRowCount, Is.EqualTo(11));
            Assert.That(data.SkippedLineNumbers, Is.EqualTo(new[] { 6, 13 }));
            Assert.That(data.Housing.Count, Is.EqualTo(4));
            Assert.That(data.NonHousing.Count, Is.EqualTo(4));
            Assert.That(data.ProgramChanges.Count, Is.EqualTo(1));

            var housing = data.Housing[0];
            Assert.That(housing.Block, Is.EqualTo("01"));
            Assert.That(housing.Phase, Is.EqualTo("SO"));
            Assert.That(housing.Segment, Is.EqualTo("Sociaal"));
            Assert.That(housing.Rooms, Is.EqualTo(2));
            Assert.That(housing.MinimumUsableFloorArea, Is.EqualTo(42.5m));
            Assert.That(housing.DwellingCount, Is.EqualTo(12));
            Assert.That(data.Housing[1].DwellingCount, Is.Zero);

            var housingWithoutRoomType = data.Housing[2];
            Assert.That(housingWithoutRoomType.Phase, Is.EqualTo("BBP"));
            Assert.That(housingWithoutRoomType.Rooms, Is.Null);
            Assert.That(housingWithoutRoomType.DwellingCount, Is.EqualTo(20));
            Assert.That(data.Housing[3].DwellingCount, Is.Zero);

            var nonHousing = data.NonHousing[0];
            Assert.That(nonHousing.Block, Is.EqualTo("01"));
            Assert.That(nonHousing.Phase, Is.EqualTo("VO"));
            Assert.That(nonHousing.Category, Is.EqualTo("Maatschappelijk"));
            Assert.That(nonHousing.GrossFloorArea, Is.EqualTo(320.5m));
            Assert.That(data.NonHousing[1].GrossFloorArea, Is.EqualTo(450m));
            Assert.That(data.NonHousing[2].Phase, Is.EqualTo("SO"));
            Assert.That(data.NonHousing[2].GrossFloorArea, Is.Zero);
            Assert.That(data.NonHousing[3].Phase, Is.EqualTo("DO"));
            Assert.That(data.NonHousing[3].GrossFloorArea, Is.Null);

            var change = data.ProgramChanges[0];
            Assert.That(change.Block, Is.EqualTo("01"));
            Assert.That(change.Phase, Is.EqualTo("VO"));
            Assert.That(change.Program, Is.EqualTo("Wonen"));
            Assert.That(change.Deviation.Replace("\r\n", "\n"), Is.EqualTo("Meer woningen;\nuitwerking volgt"));
        }

        [TestCase("Wonen", "", true)]
        [TestCase("Wonen", "0", true)]
        [TestCase("Wonen", "-1", false)]
        [TestCase("Wonen", "ongeldig", false)]
        [TestCase("Niet-wonen", "", true)]
        [TestCase("Niet-wonen", "0", true)]
        [TestCase("Niet-wonen", "-1", false)]
        [TestCase("Niet-wonen", "ongeldig", false)]
        public void DistinguishesUnknownZeroAndInvalidQuantities(string type, string quantity, bool accepted)
        {
            const string header = "Type;Blok;Planproduct;Segment;Kamers;Min GBO;Aantal woningen;Categorie;BVO;Programma;Afwijking";
            var row = type == "Wonen"
                ? $"Wonen;01;SO;Sociaal;2;42.5;{quantity};;;;"
                : $"Niet-wonen;01;SO;;;;;Kantoor;{quantity};;";
            using var reader = new StringReader(header + "\n" + row);
            var data = DashboardCsv.Read(reader);

            Assert.That(data.SourceRowCount, Is.EqualTo(1));
            Assert.That(data.SkippedLineNumbers, Is.EqualTo(accepted ? Array.Empty<int>() : new[] { 2 }));
            Assert.That(data.Housing.Count + data.NonHousing.Count, Is.EqualTo(accepted ? 1 : 0));
            if (!accepted)
                return;

            decimal? value = type == "Wonen" ? data.Housing[0].DwellingCount : data.NonHousing[0].GrossFloorArea;
            Assert.That(value, Is.EqualTo(quantity.Length == 0 ? (decimal?)null : 0m));
        }

        [Test]
        public void PreservesKnownSubtotalsWithoutPresentingThemAsComplete()
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;",
                "Wonen;A;BBP;Sociaal;2;40;;;;;",
                "Niet-wonen;Z;BBP;;;;;Maatschappelijk (IKC);25;;",
                "Niet-wonen;Z;BBP;;;;;Maatschappelijk (IKC);;;");
            var summary = InvokeSummary("Calculate", data);
            var phases = (Array)summary.GetType().GetField("Item1").GetValue(summary);
            var phase = phases.GetValue(0);

            AssertResult(Property(phase, "Dwellings"), 10m, "Partial");
            AssertResult(Property(phase, "GrossFloorArea"), 25m, "Partial");
            AssertResult(Property(phase, "AverageMinimumFloorArea"), 40m, "Partial");
            Assert.That(Property(phase, "HasData"), Is.True);

            foreach (var field in new[] { "Item2", "Item3", "Item4", "Item5" })
            {
                var rows = (Array)summary.GetType().GetField(field).GetValue(summary);
                var values = (Array)Property(rows.GetValue(0), "Values");
                AssertResult(values.GetValue(0), field == "Item2" || field == "Item3" ? 10m : 25m, "Partial");
            }
        }

        [Test]
        public void UsesSeparateBlockCoverageForHousingAndNonHousing()
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;",
                "Wonen;A;SO;Sociaal;2;40;12;;;;",
                "Wonen;B;BBP;Sociaal;2;40;20;;;;",
                "Niet-wonen;Z;SO;;;;;Kantoor;100;;");
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);

            AssertResult(Property(phases.GetValue(0), "Dwellings"), 30m, "Complete");
            AssertResult(Property(phases.GetValue(1), "Dwellings"), 32m, "Complete");
            AssertResult(Property(phases.GetValue(1), "GrossFloorArea"), 100m, "Complete");
            AssertResult(Property(phases.GetValue(2), "Dwellings"), 32m, "Complete");
            AssertResult(Property(phases.GetValue(0), "GrossFloorArea"), null, "Unknown");
            var breakdowns = InvokeSummary("CalculateBreakdowns", data);
            var segments = (Array)breakdowns.GetType().GetField("Item1").GetValue(breakdowns);
            AssertResult(((Array)Property(segments.GetValue(0), "Values")).GetValue(1), 32m, "Complete");
            var blockPhases = (Array)InvokeSummary("CalculatePhases", data, "A");
            AssertResult(Property(blockPhases.GetValue(1), "Dwellings"), 12m, "Complete");
            AssertResult(Property(blockPhases.GetValue(2), "Dwellings"), null, "Unknown");
        }

        [Test]
        public void SelectsWholeVersionsSeparatelyAndPreservesTheirSourcePhases()
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;",
                "Wonen;A;BBP;Vrij;3;80;20;;;;",
                "Wonen;A;SO;Sociaal;2;50;;;;;",
                "Wonen;B;VO;Sociaal;2;60;30;;;;",
                "Niet-wonen;A;BBP;;;;;Kantoor;100;;",
                "Niet-wonen;A;VO;;;;;Kantoor;0;;");
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            AssertResult(Property(phases.GetValue(0), "Dwellings"), 30m, "Partial");
            AssertResult(Property(phases.GetValue(1), "Dwellings"), null, "Unknown");
            AssertResult(Property(phases.GetValue(2), "Dwellings"), 30m, "Partial");
            AssertResult(Property(phases.GetValue(1), "GrossFloorArea"), 100m, "Complete");
            AssertResult(Property(phases.GetValue(2), "GrossFloorArea"), 0m, "Complete");

            var sources = Property(phases.GetValue(2), "HousingSources");
            var blocks = (System.Collections.Generic.IReadOnlyDictionary<string, string>)Property(sources, "BlockPhases");
            Assert.That(blocks["A"], Is.EqualTo("SO"));
            Assert.That(blocks["B"], Is.EqualTo("VO"));
            Assert.That(Property(sources, "EarlierBlocks"), Is.EqualTo(1));
            Assert.That(Property(sources, "ExactBlocks"), Is.EqualTo(1));
            Assert.That(Property(Property(phases.GetValue(0), "HousingSources"), "MissingBlocks"), Is.EqualTo(1));

            var breakdowns = InvokeSummary("CalculateBreakdowns", data);
            var segments = (Array)breakdowns.GetType().GetField("Item1").GetValue(breakdowns);
            var free = segments.Cast<object>().Single(row => (string)Property(row, "Label") == "Vrije sector");
            AssertResult(((Array)Property(free, "Values")).GetValue(1), null, "Unknown");

            var current = InvokeSummary("CalculateCurrent", data);
            AssertResult(Property(current, "Dwellings"), 30m, "Partial");
            AssertResult(Property(current, "GrossFloorArea"), 0m, "Complete");
        }

        [Test]
        public void RecalculatesAreaAveragesFromSelectedRowsAndKeepsActualBlockPhasesEmpty()
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;",
                "Wonen;A;SO;Sociaal;2;50;20;;;;",
                "Wonen;B;BBP;Sociaal;3;80;60;;;;");
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            AssertResult(Property(phases.GetValue(1), "Dwellings"), 80m, "Complete");
            AssertResult(Property(phases.GetValue(1), "AverageMinimumFloorArea"), 72.5m, "Complete");
            AssertResult(Property(phases.GetValue(3), "Dwellings"), 80m, "Complete");
            var blockPhases = (Array)InvokeSummary("CalculatePhases", data, "B");
            AssertResult(Property(blockPhases.GetValue(1), "Dwellings"), null, "Unknown");
            Assert.That(Property(blockPhases.GetValue(1), "HasData"), Is.False);
        }

        [TestCase("10", "40", "30", "60", 55, "Complete")]
        [TestCase("0", "", "10", "40", 40, "Complete")]
        [TestCase("", "40", "10", "40", 40, "Partial")]
        [TestCase("10", "", "10", "40", 40, "Partial")]
        [TestCase("0", "", "0", "", null, "NotApplicable")]
        [TestCase("10", "", "10", "", null, "Unknown")]
        public void CalculatesWeightedAveragesFromKnownWeightsAndAreas(string firstCount, string firstArea, string secondCount, string secondArea,
            int? expected, string status)
        {
            var data = ReadRows(
                $"Wonen;A;BBP;Sociaal;2;{firstArea};{firstCount};;;;",
                $"Wonen;A;BBP;Sociaal;3;{secondArea};{secondCount};;;;");
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            AssertResult(Property(phases.GetValue(0), "AverageMinimumFloorArea"), expected, status);
        }

        [Test]
        public void CalculatesChangesFromKnownValuesAndSharesFromCompleteTotals()
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;;0;;;;",
                "Wonen;A;SO;Sociaal;2;40;12;;;;",
                "Wonen;A;VO;Sociaal;2;40;;;;;",
                "Wonen;A;DO;Sociaal;2;40;6;;;;");
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            var firstResult = Property(phases.GetValue(0), "Dwellings");
            var values = Array.CreateInstance(firstResult.GetType(), phases.Length);
            for (var i = 0; i < phases.Length; i++)
                values.SetValue(Property(phases.GetValue(i), "Dwellings"), i);

            AssertResult(firstResult, 0m, "Complete");
            Assert.That(Property(phases.GetValue(2), "HasData"), Is.True);
            AssertResult(InvokeSummary("CalculateSegmentShare", firstResult, firstResult), null, "NotApplicable");
            AssertResult(InvokeSummary("CalculateSegmentShare", values.GetValue(1), values.GetValue(1)), 1m, "Complete");
            AssertResult(InvokeSummary("CalculateSegmentShare", values.GetValue(1), values.GetValue(2)), null, "Partial");

            var changes = (Array)InvokeSummary("CalculateChanges", (object)values);
            var firstChange = changes.GetValue(1);
            AssertResult(firstChange.GetType().GetField("Item1").GetValue(firstChange), 12m, "Complete");
            AssertResult(firstChange.GetType().GetField("Item2").GetValue(firstChange), null, "NotApplicable");
            foreach (var index in new[] { 2, 3 })
            {
                var change = changes.GetValue(index);
                AssertResult(change.GetType().GetField("Item1").GetValue(change), null, "Partial");
                AssertResult(change.GetType().GetField("Item2").GetValue(change), null, "Partial");
            }

            var completeData = ReadRows("Wonen;A;BBP;Sociaal;2;40;10;;;;", "Wonen;A;SO;Sociaal;2;40;15;;;;");
            var completePhases = (Array)InvokeSummary("CalculatePhases", completeData, null);
            values.SetValue(Property(completePhases.GetValue(0), "Dwellings"), 0);
            values.SetValue(Property(completePhases.GetValue(1), "Dwellings"), 1);
            var completeChange = ((Array)InvokeSummary("CalculateChanges", (object)values)).GetValue(1);
            AssertResult(completeChange.GetType().GetField("Item1").GetValue(completeChange), 5m, "Complete");
            AssertResult(completeChange.GetType().GetField("Item2").GetValue(completeChange), 0.5m, "Complete");

            var partialData = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;", "Wonen;A;BBP;Vrij;3;60;;;;;",
                "Wonen;A;SO;Sociaal;2;50;15;;;;", "Wonen;A;SO;Vrij;3;60;;;;;");
            var partialPhases = (Array)InvokeSummary("CalculatePhases", partialData, null);
            values.SetValue(Property(partialPhases.GetValue(0), "Dwellings"), 0);
            values.SetValue(Property(partialPhases.GetValue(1), "Dwellings"), 1);
            var partialChange = ((Array)InvokeSummary("CalculateChanges", (object)values)).GetValue(1);
            AssertResult(partialChange.GetType().GetField("Item1").GetValue(partialChange), 5m, "Partial");
            AssertResult(partialChange.GetType().GetField("Item2").GetValue(partialChange), 0.5m, "Partial");
        }

        [Test]
        public void RetainsCategoryMappingAndSeparateSocialDetails()
        {
            var data = ReadRows(
                "Niet-wonen;A;BBP;;;;;Commercieel;10;;",
                "Niet-wonen;A;BBP;;;;;Bestaand programma - kantoor;20;;",
                "Niet-wonen;A;BBP;;;;;Kantoor thuiswerk;30;;",
                "Niet-wonen;A;BBP;;;;;Maatschappelijk (IKC);40;;");
            var breakdowns = InvokeSummary("CalculateBreakdowns", data);
            var categories = (Array)breakdowns.GetType().GetField("Item3").GetValue(breakdowns);
            Assert.That(categories.Cast<object>().Select(row => Property(row, "Label")), Is.EquivalentTo(new[]
            {
                "Horeca", "Bestaand programma - kantoor", "Kantoor thuiswerk", "Maatschappelijk"
            }));
            var social = (Array)breakdowns.GetType().GetField("Item4").GetValue(breakdowns);
            Assert.That(Property(social.GetValue(0), "Label"), Is.EqualTo("IKC"));
            AssertResult(((Array)Property(social.GetValue(0), "Values")).GetValue(0), 40m, "Complete");
        }

        [TestCase("SO", "VO")]
        [TestCase("DO", "SO")]
        public void BindsLatestBlockProgramsSeparatelyAndDistinguishesUnknownFromNotApplicable(string housingPhase, string nonHousingPhase)
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;",
                $"Wonen;A;{housingPhase};Sociaal;2;;0;;;;",
                "Niet-wonen;A;BBP;;;;;Kantoor;100;;",
                $"Niet-wonen;A;{nonHousingPhase};;;;;Kantoor;;;");
            var hud = new DashboardHUD();
            var showData = typeof(DashboardHUD).GetMethod("ShowData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            showData.Invoke(hud, new object[] { data });
            var overview = hud.Q<DashboardBlockOverview>();

            Assert.That(overview.TotalDwellings, Is.EqualTo("0"));
            Assert.That(overview.AverageMinimumFloorArea, Is.EqualTo("n.v.t."));
            Assert.That(overview.NonHousingGrossFloorArea, Is.EqualTo("\u2014"));
            Assert.That(overview.HeaderText, Does.Contain($"Wonen: {housingPhase}"));
            Assert.That(overview.HeaderText, Does.Contain($"Niet-wonen: {nonHousingPhase}"));

            var unknownHousing = ReadRows($"Wonen;A;{housingPhase};Sociaal;2;40;;;;;");
            showData.Invoke(hud, new object[] { unknownHousing });
            Assert.That(overview.TotalDwellings, Is.EqualTo("\u2014"));
            Assert.That(overview.AverageMinimumFloorArea, Is.EqualTo("\u2014"));
            Assert.That(overview.HeaderText, Does.Not.Contain("Niet-wonen:"));

            showData.Invoke(hud, new object[] { null });
            Assert.That(overview.HeaderText, Is.EqualTo("Bouwblokoverzicht"));
            Assert.That(overview.AverageMinimumFloorArea, Is.EqualTo("\u2014"));
        }

        [Test]
        public void HousingColumnsMatchAreaTotalsAndUseWholeSourcePhases()
        {
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;",
                "Wonen;A;BBP;Vrij;3;80;20;;;;",
                "Wonen;A;SO;Sociaal;2;40;12;;;;",
                "Wonen;A;SO;Midden;3;60;3;;;;",
                "Wonen;B;BBP;Sociaal;2;40;20;;;;",
                "Wonen;C;VO;Sociaal;2;40;;;;;");
            var blocks = (Array)InvokeSummary("CalculateHousingBlocks", data, "SO");
            var first = blocks.GetValue(0);
            Assert.That(Property(first, "SourcePhase"), Is.EqualTo("SO"));
            AssertResult(Property(first, "Total"), 15m, "Complete");
            Assert.That(Property(first, "Segments"), Is.EqualTo(new[] { "Sociaal", "Middenhuur", "Vrije sector" }));
            AssertResult(((Array)Property(first, "Values")).GetValue(2), null, "Unknown");
            Assert.That(Property(blocks.GetValue(1), "SourcePhase"), Is.EqualTo("BBP"));
            Assert.That(Property(blocks.GetValue(2), "SourcePhase"), Is.Null);

            var knownTotal = blocks.Cast<object>().Sum(block => (decimal?)Property(Property(block, "Total"), "Value"));
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            Assert.That(knownTotal, Is.EqualTo(Property(Property(phases.GetValue(1), "Dwellings"), "Value")));

            var current = (Array)InvokeSummary("CalculateHousingBlocks", data, null);
            Assert.That(Property(current.GetValue(2), "SourcePhase"), Is.EqualTo("VO"));
            AssertResult(Property(current.GetValue(2), "Total"), null, "Unknown");
        }

        [Test]
        public void BarChartScalesStacksAndKeepsZeroAndUnknownCategoriesWithoutInventingSegments()
        {
            var chart = new BarChart { MinimumStep = 1 };
            var series = new[] { new BarChart.Series("First", Color.blue), new BarChart.Series("Second", Color.green) };
            chart.SetData(series, new[]
            {
                new BarChart.Category("A", new[] { new BarChart.Value(10, "First: 10"), new BarChart.Value(20, "Second: 20") }),
                new BarChart.Category("B", new[] { new BarChart.Value(0), new BarChart.Value(null) }, "Zero and unknown")
            });

            var segments = chart.Query(className: "bar-chart__segment").ToList();
            Assert.That(segments.Count, Is.EqualTo(2));
            Assert.That(segments[0].style.height.value.value, Is.EqualTo(31.25f));
            Assert.That(segments[1].style.height.value.value, Is.EqualTo(62.5f));
            Assert.That(segments[0].tooltip, Is.EqualTo("First: 10"));
            Assert.That(chart.Query(className: "bar-chart__tick").ToList().Count, Is.EqualTo(5));
            var gridLines = chart.Q(className: "bar-chart__plot").Query(className: "bar-chart__grid-line").ToList();
            Assert.That(gridLines.Count, Is.EqualTo(5));
            Assert.That(gridLines[4].style.top.value.value, Is.EqualTo(0f));
            var columns = chart.Query(className: "bar-chart__column").ToList();
            Assert.That(columns.Count, Is.EqualTo(2));
            var totals = chart.Query<Label>(className: "bar-chart__total-label").ToList();
            Assert.That(totals[0].text, Is.EqualTo("30"));
            Assert.That(totals[0].parent.style.bottom.value.value, Is.EqualTo(93.75f));
            Assert.That(totals[1].text, Is.EqualTo("\u2014"));
            Assert.That(totals[1].tooltip, Is.EqualTo("Zero and unknown"));
            Assert.That(chart.Query(className: "bar-chart__legend-entry").ToList().Count, Is.EqualTo(2));

            chart.SetData(series, new[]
            {
                new BarChart.Category("Partial", new[] { new BarChart.Value(10), new BarChart.Value(null) }, "Known subtotal", "10*"),
                new BarChart.Category("Zero", new[] { new BarChart.Value(0), new BarChart.Value(0) })
            });
            totals = chart.Query<Label>(className: "bar-chart__total-label").ToList();
            Assert.That(totals[0].text, Is.EqualTo("10*"));
            Assert.That(totals[0].tooltip, Is.EqualTo("Known subtotal"));
            Assert.That(totals[1].text, Is.EqualTo("0"));

            chart.SetData(Array.Empty<BarChart.Series>(), Array.Empty<BarChart.Category>());
            Assert.That(chart.Query(className: "bar-chart__segment").ToList(), Is.Empty);
            Assert.That(chart.Query(className: "bar-chart__legend-entry").ToList(), Is.Empty);
            Assert.That(chart.Q("ChartBody").style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void BarChartCanChangeDirectionWithoutLosingDataTooltipsOrTotals()
        {
            var chart = new BarChart { MinimumStep = 1 };
            chart.SetData(new[] { new BarChart.Series("First", Color.blue), new BarChart.Series("Second", Color.green) }, new[]
            {
                new BarChart.Category("A", new[] { new BarChart.Value(10, "First: 10"), new BarChart.Value(20, "Second: 20") }, "Total: 30"),
                new BarChart.Category("B", new[] { new BarChart.Value(0), new BarChart.Value(null) }, "Unknown")
            });

            chart.Orientation = BarChart.BarChartOrientation.Horizontal;
            var segments = chart.Query(className: "bar-chart__segment").ToList();
            Assert.That(segments[0].style.width.value.value, Is.EqualTo(31.25f));
            Assert.That(segments[1].style.width.value.value, Is.EqualTo(62.5f));
            Assert.That(segments[0].tooltip, Is.EqualTo("First: 10"));
            Assert.That(chart.Q("Columns").parent, Is.SameAs(chart.Q("HorizontalPlotArea")));
            Assert.That(chart.Query<Label>(className: "bar-chart__tick").ToList().Last().style.left.value.value, Is.EqualTo(100f));
            var total = chart.Q<Label>(className: "bar-chart__total-label");
            Assert.That(total.text, Is.EqualTo("30"));
            Assert.That(total.tooltip, Is.EqualTo("Total: 30"));
            Assert.That(total.parent.style.left.value.value, Is.EqualTo(93.75f));

            chart.Orientation = BarChart.BarChartOrientation.Vertical;
            Assert.That(chart.Q("Columns").parent, Is.SameAs(chart.Q<UnityEngine.UIElements.ScrollView>("ChartScrollView").contentContainer));
            Assert.That(chart.Query(className: "bar-chart__segment").ToList()[1].style.height.value.value, Is.EqualTo(62.5f));
            Assert.That(chart.Query(className: "bar-chart__legend-entry").ToList().Count, Is.EqualTo(2));
        }

        [Test]
        public void RingChartKeepsUnknownValuesDistinctFromZeroAndReplacesPreviousData()
        {
            var chart = new RingChart { Unit = "Woningen" };
            chart.SetData(new[]
            {
                new RingChart.Segment("Known", 10, Color.blue, "Known: 10\n100%*", "100%*"),
                new RingChart.Segment("Unknown", null, Color.green, "Unknown")
            }, "10*", "Known subtotal: 10");

            Assert.That(chart.Q("ChartSegments").childCount, Is.EqualTo(1));
            Assert.That(chart.Q("ChartSegments")[0].tooltip, Is.EqualTo("Known: 10\n100%*"));
            var percentageLabel = chart.Q("ChartPercentages").Q<Label>();
            Assert.That(percentageLabel.tooltip, Is.EqualTo(chart.Q("ChartSegments")[0].tooltip));
            Assert.That(percentageLabel.pickingMode, Is.EqualTo(PickingMode.Position));
            Assert.That(percentageLabel.text, Is.EqualTo("100%*"));
            Assert.That(chart.Q("ChartLegend").childCount, Is.EqualTo(2));
            Assert.That(chart.Q<Label>("ChartTotal").text, Is.EqualTo("10*"));
            Assert.That(chart.Q<Label>("ChartTotal").tooltip, Is.EqualTo("Known subtotal: 10"));

            chart.SetData(new[] { new RingChart.Segment("Zero", 0, Color.blue) });
            Assert.That(chart.Q("ChartSegments").childCount, Is.Zero);
            Assert.That(chart.Q("ChartPercentages").childCount, Is.Zero);
            Assert.That(chart.Q("ChartLegend").childCount, Is.EqualTo(1));
            Assert.That(chart.Q<Label>("ChartTotal").text, Is.EqualTo("0"));
            Assert.That(chart.Q<Label>("ChartTotal").tooltip, Is.Null.Or.Empty);

            chart.SetData(new[] { new RingChart.Segment("Unknown", null, Color.blue) });
            Assert.That(chart.Q("ChartSegments").childCount, Is.Zero);
            Assert.That(chart.Q<Label>("ChartTotal").text, Is.EqualTo("\u2014"));

            chart.SetData(Array.Empty<RingChart.Segment>());
            Assert.That(chart.Q("ChartLegend").childCount, Is.Zero);
            Assert.That(chart.Q("ChartPlot").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(chart.Q("EmptyChart").style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.Throws<ArgumentException>(() => chart.SetData(new[] { new RingChart.Segment("Negative", -1, Color.blue) }));
        }

        [Test]
        public void AreaComparisonChartScalesAreasAndKeepsCallerTextZeroAndUnknownValues()
        {
            var chart = new AreaComparisonChart();
            chart.SetData(new[]
            {
                new AreaComparisonChart.Item("Small", 16, Color.blue, "Known area: 16", "16 m\u00b2*"),
                new AreaComparisonChart.Item("Large", 64, Color.green),
                new AreaComparisonChart.Item("Zero", 0, Color.gray),
                new AreaComparisonChart.Item("Unknown", null, Color.gray)
            });

            var circles = chart.Query(className: "area-comparison-chart__circle").ToList();
            Assert.That(circles.Count, Is.EqualTo(2));
            Assert.That(circles[0].style.width.value.value, Is.EqualTo(50f));
            Assert.That(circles[1].style.width.value.value, Is.EqualTo(100f));
            Assert.That(circles[0].tooltip, Is.EqualTo("Known area: 16"));
            var labels = chart.Query<Label>(className: "area-comparison-chart__value-label").ToList();
            Assert.That(labels.Select(label => label.text), Is.EqualTo(new[] { "16 m\u00b2*", "64", "0", "\u2014" }));
            Assert.That(chart.Q("ChartLegend").Query<Label>().ToList().Select(label => label.text), Is.EqualTo(new[] { "Small", "Large", "Zero", "Unknown" }));

            chart.SetData(new[] { new AreaComparisonChart.Item("Small", 16, Color.blue) }, 64);
            Assert.That(chart.Query(className: "area-comparison-chart__circle").ToList().Count, Is.EqualTo(1));
            Assert.That(chart.Q(className: "area-comparison-chart__circle").style.width.value.value, Is.EqualTo(50f));
            Assert.Throws<ArgumentOutOfRangeException>(() => chart.SetData(new[] { new AreaComparisonChart.Item("Large", 64, Color.blue) }, 16));
            Assert.Throws<ArgumentException>(() => chart.SetData(new[] { new AreaComparisonChart.Item("Negative", -1, Color.blue) }));

            chart.SetData(Array.Empty<AreaComparisonChart.Item>());
            Assert.That(chart.Q("ChartItems").childCount, Is.Zero);
            Assert.That(chart.Q("ChartLegend").childCount, Is.Zero);
            Assert.That(chart.Q("ChartItems").style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(chart.Q("EmptyChart").style.display.value, Is.EqualTo(DisplayStyle.Flex));
        }

        [TestCase("9007199254740992", "1", "9007199254740993")]
        [TestCase("0.123456789012345678", "0.000000000000000001", "0.123456789012345679")]
        public void BarChartPreservesDecimalPrecisionInTotalLabels(string first, string second, string expected)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var chart = new BarChart { NumberCulture = culture, ValueFormat = "0.############################" };
            chart.SetData(new[] { new BarChart.Series("First", Color.blue), new BarChart.Series("Second", Color.green) }, new[]
            {
                new BarChart.Category("A", new[] { new BarChart.Value(decimal.Parse(first, culture)), new BarChart.Value(decimal.Parse(second, culture)) })
            });

            Assert.That(chart.Q<Label>(className: "bar-chart__total-label").text, Is.EqualTo(expected));
        }

        [TestCase("9998", "9.998")]
        [TestCase("9999", "10k")]
        [TestCase("10400", "10,4k")]
        [TestCase("999949", "999,9k")]
        [TestCase("999950", "1 mln")]
        [TestCase("10400000", "10,4 mln")]
        [TestCase("10400000000", "10,4 mld")]
        public void BarChartCompactsTotalLabelsUsingTheConfiguredCultureAndSuffixes(string input, string expected)
        {
            var chart = new BarChart
            {
                CompactNumbers = true,
                NumberCulture = System.Globalization.CultureInfo.GetCultureInfo("nl-NL"),
                MillionSuffix = " mln",
                BillionSuffix = " mld"
            };
            chart.SetData(new[] { new BarChart.Series("A", Color.blue) }, new[]
            {
                new BarChart.Category("A", new[] { new BarChart.Value(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)) }, "Full value: " + input)
            });

            var total = chart.Q<Label>(className: "bar-chart__total-label");
            Assert.That(total.text, Is.EqualTo(expected));
            Assert.That(total.tooltip, Is.EqualTo("Full value: " + input));
        }

        [Test]
        public void BarChartCompactsAxisLabelsButKeepsTheFullValueInTooltips()
        {
            var chart = new BarChart { CompactNumbers = true, NumberCulture = System.Globalization.CultureInfo.GetCultureInfo("nl-NL") };
            chart.SetData(new[] { new BarChart.Series("A", Color.blue) }, new[]
            {
                new BarChart.Category("A", new[] { new BarChart.Value(10_400m) })
            });

            var highestTick = chart.Query<Label>(className: "bar-chart__tick").ToList().Last();
            Assert.That(highestTick.text, Is.EqualTo("12k"));
            Assert.That(highestTick.tooltip, Is.EqualTo("12.000"));
        }

        [TestCase("1830", "2000")]
        [TestCase("5014", "6000")]
        [TestCase("150513", "160000")]
        public void BarChartUsesFinerScaleStepsForDifferentMagnitudes(string amount, string expectedMaximum)
        {
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var chart = new BarChart { NumberCulture = culture, ValueFormat = "0.##" };
            chart.SetData(new[] { new BarChart.Series("A", Color.blue) }, new[]
            {
                new BarChart.Category("A", new[] { new BarChart.Value(decimal.Parse(amount, culture)) })
            });
            Assert.That(chart.Query<Label>(className: "bar-chart__tick").ToList().Last().tooltip, Is.EqualTo(expectedMaximum));
        }

        [Test]
        public void SidebarExplanationFollowsVisibleDataAndDoesNotRepeatChartDescriptions()
        {
            var hud = new DashboardHUD();
            var showData = typeof(DashboardHUD).GetMethod("ShowData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var complete = ReadRows("Wonen;A;BBP;Sociaal;2;40;10;;;;", "Wonen;B;SO;Sociaal;2;40;20;;;;");
            showData.Invoke(hud, new object[] { complete });
            var explanation = hud.Q<TextCallOut>("SidebarDataExplanation");
            Assert.That(explanation.parent, Is.SameAs(hud.Q("Sidebar")));
            Assert.That(explanation.Text, Does.Contain("Per blok gebruiken we de laatst beschikbare gegevens."));
            Assert.That(explanation.Text, Does.Not.Contain("*Onvolledig"));
            Assert.That(hud.Q<BarChart>("HousingBlockChart").Description, Is.Empty);
            Assert.That(hud.Q<RingChart>("HousingSegmentChart").Q<Label>("ChartTotal").text, Is.EqualTo("30"));
            Assert.That(hud.Q<RingChart>("HousingSegmentChart").Q("ChartSegments")[0].tooltip.Count(character => character == '%'), Is.EqualTo(1));
            Assert.That(hud.Q<RingChart>("HousingSegmentChart").Q<Label>("ChartTotal").tooltip, Does.Not.Contain("Alle blokken:"));
            Assert.That(hud.Q<AreaComparisonChart>("HousingFloorAreaChart").Q<Label>(className: "area-comparison-chart__value-label").tooltip, Does.Not.Contain("Alle blokken:"));

            var incomplete = ReadRows("Wonen;A;BBP;Sociaal;2;40;10;;;;", "Wonen;B;SO;Sociaal;2;40;;;;;");
            showData.Invoke(hud, new object[] { incomplete });
            Assert.That(explanation.Text, Does.Contain("*Onvolledig"));
            Assert.That(explanation.Text, Does.Not.Contain("* "));
            Assert.That(hud.Q<RingChart>("HousingSegmentChart").Q<Label>("ChartTotal").text, Is.EqualTo("10*"));

            var housingTypes = hud.Q<ToggleButtonGroup>("HousingTypes");
            housingTypes.value = new ToggleButtonGroupState(2, housingTypes.childCount);
            Assert.That(explanation.Text, Does.Contain("*Onvolledig"));
            Assert.That(explanation.ClassListContains("hidden"), Is.False);

            housingTypes.value = new ToggleButtonGroupState(1, housingTypes.childCount);
            Assert.That(explanation.Text, Does.Contain("*Onvolledig"));
            hud.Q<ToggleSwitch>("DashboardViewSwitch").value = true;
            Assert.That(explanation.Text, Does.Contain("Per blok gebruiken we de laatst beschikbare gegevens."));
            Assert.That(explanation.Text.Split(new[] { "*Onvolledig" }, StringSplitOptions.None).Length - 1, Is.EqualTo(1));

            showData.Invoke(hud, new object[] { null });
            Assert.That(explanation.ClassListContains("hidden"), Is.True);
        }

        [Test]
        public void DashboardAreaChartWeightsTheLatestHousingPhasePerBlock()
        {
            var hud = new DashboardHUD();
            var showData = typeof(DashboardHUD).GetMethod("ShowData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var data = ReadRows("Wonen;A;BBP;Sociaal;2;20;100;;;;", "Wonen;A;SO;Sociaal;2;40;10;;;;", "Wonen;B;BBP;Sociaal;2;70;20;;;;");
            showData.Invoke(hud, new object[] { data });
            var chart = hud.Q<AreaComparisonChart>("HousingFloorAreaChart");
            Assert.That(chart.Q<Label>(className: "area-comparison-chart__value-label").text, Is.EqualTo("60 m\u00b2"));
            Assert.That(chart.Query(className: "area-comparison-chart__circle").ToList().Count, Is.EqualTo(1));

            showData.Invoke(hud, new object[] { null });
            Assert.That(chart.Q("ChartItems").childCount, Is.Zero);
        }

        [Test]
        public void DashboardRingTotalsDoNotDependOnPhaseChartFormatting()
        {
            var hud = new DashboardHUD();
            var phaseChart = hud.Q<BarChart>("NonHousingPhaseChart");
            phaseChart.CompactNumbers = false;
            phaseChart.ValueFormat = "0.000";
            var showData = typeof(DashboardHUD).GetMethod("ShowData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            showData.Invoke(hud, new object[] { ReadRows("Niet-wonen;A;BBP;;;;;Kantoor;15000;;") });
            Assert.That(hud.Q<RingChart>("NonHousingCategoryChart").Q<Label>("ChartTotal").text, Is.EqualTo("15k"));
        }

        [Test]
        public void PhaseComparisonKeepsTheSharedWarningForVisibleChartsWhenTheTableChanges()
        {
            var hud = new DashboardHUD();
            var showData = typeof(DashboardHUD).GetMethod("ShowData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            showData.Invoke(hud, new object[] { ReadRows("Wonen;A;BBP;Sociaal;2;;10;;;;") });
            hud.Q<ToggleSwitch>("DashboardViewSwitch").value = true;
            var explanation = hud.Q<TextCallOut>("SidebarDataExplanation");
            Assert.That(explanation.Text, Does.Contain("*Onvolledig"));

            var tabs = hud.Q<ToggleButtonGroup>("ComparisonTabs");
            tabs.value = new ToggleButtonGroupState(8, tabs.childCount);
            Assert.That(explanation.Text, Does.Contain("Per blok gebruiken we de laatst beschikbare gegevens."));
            Assert.That(explanation.Text, Does.Contain("*Onvolledig"));
            tabs.value = new ToggleButtonGroupState(1, tabs.childCount);
            Assert.That(explanation.Text, Does.Contain("*Onvolledig"));
        }

        [Test]
        public void DashboardShowsTheTotalDiagramSetsAndDoesNotAddBlockPhaseIterations()
        {
            var hud = new DashboardHUD();
            var showData = typeof(DashboardHUD).GetMethod("ShowData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var data = ReadRows(
                "Wonen;A;BBP;Sociaal;2;40;10;;;;", "Wonen;A;SO;Sociaal;2;50;12;;;;",
                "Wonen;B;BBP;Midden;3;60;20;;;;", "Niet-wonen;A;BBP;;;;;Kantoor;100;;", "Niet-wonen;A;SO;;;;;Kantoor;120;;");
            showData.Invoke(hud, new object[] { data });

            var program = hud.Q("ProgramOverviewContent");
            var progress = hud.Q("PhaseProgressContent");
            Assert.That(program.Query<BarChart>().ToList().Count, Is.EqualTo(2));
            Assert.That(program.Query<RingChart>().ToList().Count, Is.EqualTo(2));
            Assert.That(program.Query<AreaComparisonChart>().ToList().Count, Is.EqualTo(1));
            Assert.That(progress.Query<BarChart>().ToList().Count, Is.EqualTo(4));
            Assert.That(progress.Query<AreaComparisonChart>().ToList().Count, Is.EqualTo(1));
            Assert.That(hud.Q<RingChart>("NonHousingCategoryChart").Q<Label>("ChartTotal").text, Is.EqualTo("120"));
            Assert.That(hud.Q<BarChart>("HousingRoomChart").Query<Label>(className: "bar-chart__total-label").ToList().Select(label => label.text), Is.EqualTo(new[] { "12", "20" }));
            Assert.That(hud.Q<BarChart>("NonHousingPhaseChart").Query<Label>(className: "bar-chart__total-label").ToList().Select(label => label.text), Is.EqualTo(new[] { "100", "120", "120", "120" }));
            Assert.That(hud.Q<BarChart>("NonHousingBlockPhaseChart").Query<Label>(className: "bar-chart__total-label").ToList().Select(label => label.text), Is.EqualTo(new[] { "100", "120" }));

            var tabs = hud.Q<ToggleButtonGroup>("ComparisonTabs");
            tabs.value = new ToggleButtonGroupState(8, tabs.childCount);
            var housingTypes = hud.Q<ToggleButtonGroup>("HousingTypes");
            housingTypes.value = new ToggleButtonGroupState(2, housingTypes.childCount);
            Assert.That(tabs.value[0], Is.True);
            tabs.value = new ToggleButtonGroupState(2, tabs.childCount);
            hud.Q<ToggleSwitch>("DashboardViewSwitch").value = true;
            Assert.That(tabs.value[0], Is.True);
            Assert.That(program.ClassListContains("hidden"), Is.True);
            Assert.That(progress.ClassListContains("hidden"), Is.False);
            hud.Q<ToggleSwitch>("DashboardViewSwitch").value = false;
            Assert.That(program.ClassListContains("hidden"), Is.False);
            Assert.That(progress.ClassListContains("hidden"), Is.True);

            showData.Invoke(hud, new object[] { null });
            Assert.That(hud.Q<BarChart>("HousingRoomChart").Q("Columns").childCount, Is.Zero);
            Assert.That(hud.Q<BarChart>("NonHousingBlockPhaseChart").Q("Columns").childCount, Is.Zero);
        }

        [Test]
        public void ObjectIndexKeepsAllRecordsAndUsesTheSameLatestHousingPhaseAsTheCharts()
        {
            var data = ReadRows(
                "Wonen;3;BBP;Sociaal;2;40;100;;;;",
                "Wonen;3;SO;Sociaal;2;40;20;;;;",
                "Wonen;3;SO;Midden;3;60;30;;;;",
                "Niet-wonen;3;VO;;;;;Kantoor;200;;",
                "Programmawijziging;3;VO;;;;;;;Wonen;Meer woningen",
                "Wonen;4N;BBP;Vrij;3;60;7;;;;");
            var index = CreateObjectIndex(data);
            var block = FindObjectBlock(index, "3");

            Assert.That(((System.Collections.ICollection)Property(block, "Housing")).Count, Is.EqualTo(3));
            Assert.That(((System.Collections.ICollection)Property(block, "NonHousing")).Count, Is.EqualTo(1));
            Assert.That(((System.Collections.ICollection)Property(block, "ProgramChanges")).Count, Is.EqualTo(1));
            Assert.That(Property(block, "LatestPhase"), Is.EqualTo("VO"));
            var housing = Property(block, "HousingSummary");
            Assert.That(Property(housing, "SourcePhase"), Is.EqualTo("SO"));
            AssertResult(Property(housing, "Total"), 50m, "Complete");
            AssertResult(Property(Property(FindObjectBlock(index, "4N"), "HousingSummary"), "Total"), 7m, "Complete");
            Assert.That(FindObjectBlock(index, "3"), Is.SameAs(block), "Multiple model parts must share the records, not duplicate totals.");
        }

        [Test]
        public void ObjectIndexSupportsMissingIdentifiersAndPreservesLeadingZeroes()
        {
            var index = CreateObjectIndex(ReadRows("Wonen;03;SO;Sociaal;2;40;0;;;;"));

            Assert.That(FindObjectBlock(index, null), Is.Null);
            Assert.That(FindObjectBlock(index, " "), Is.Null);
            Assert.That(FindObjectBlock(index, "3"), Is.Null);
            Assert.That(FindObjectBlock(index, "ONBEKEND"), Is.Null);
            Assert.That(FindObjectBlock(index, " 03 "), Is.Not.Null);
            AssertResult(Property(Property(FindObjectBlock(index, "03"), "HousingSummary"), "Total"), 0m, "Complete");
            Assert.That(FindObjectBlock(CreateObjectIndex(null), "03"), Is.Null);
        }

        [Test]
        public void ObjectIndexRetainsUnknownCountsInTheLatestPhaseInsteadOfUsingEarlierValues()
        {
            var data = ReadRows("Wonen;3;BBP;Sociaal;2;40;100;;;;", "Wonen;3;SO;Sociaal;2;40;;;;;");
            var housing = Property(FindObjectBlock(CreateObjectIndex(data), "3"), "HousingSummary");

            Assert.That(Property(housing, "SourcePhase"), Is.EqualTo("SO"));
            AssertResult(Property(housing, "Total"), null, "Unknown");
        }

        [Test]
        public void ReplacingObjectIndexRemovesOldMatchesAndUsesNewValues()
        {
            var first = CreateObjectIndex(ReadRows("Wonen;3;SO;Sociaal;2;40;10;;;;"));
            var replacement = CreateObjectIndex(ReadRows("Wonen;4N;VO;Sociaal;2;40;30;;;;"));

            Assert.That(FindObjectBlock(first, "3"), Is.Not.Null);
            Assert.That(FindObjectBlock(replacement, "3"), Is.Null);
            AssertResult(Property(Property(FindObjectBlock(replacement, "4n"), "HousingSummary"), "Total"), 30m, "Complete");
        }

        [Test]
        public void ClearingDashboardCsvNotifiesBindingsAndKeepsTheSourceForReloading()
        {
            var source = new Netherlands3D.Twin.Layers.Properties.DashboardCsvPropertyData(new Uri("https://example.invalid/data.csv"),
                ReadRows("Wonen;3;SO;Sociaal;2;40;10;;;;"));
            var notifications = 0;
            source.DataChanged.AddListener(() => notifications++);

            typeof(Netherlands3D.Twin.Layers.Properties.DashboardCsvPropertyData)
                .GetMethod("ClearLoadedData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(source, null);

            Assert.That(source.Data, Is.Null);
            Assert.That(source.Url, Is.EqualTo(new Uri("https://example.invalid/data.csv")));
            Assert.That(notifications, Is.EqualTo(1));
        }

        [TestCase(0, "low")]
        [TestCase(299, "low")]
        [TestCase(300, "medium")]
        [TestCase(599, "medium")]
        [TestCase(600, "high")]
        public void DashboardMapUsesPrototypeDwellingThresholds(int dwellings, string expected)
        {
            var block = FindObjectBlock(CreateObjectIndex(ReadRows($"Wonen;3;SO;Sociaal;2;40;{dwellings};;;;")), "3");

            Assert.That(GetMapColorKey(block, "Dwellings"), Is.EqualTo(expected));
        }

        [Test]
        public void DashboardMapUsesNeutralForUnknownCountsAndTheExistingSegmentOrderForTies()
        {
            var unknown = FindObjectBlock(CreateObjectIndex(ReadRows("Wonen;3;SO;Sociaal;2;40;;;;;")), "3");
            Assert.That(GetMapColorKey(unknown, "Dwellings"), Is.EqualTo("base"));
            Assert.That(GetMapColorKey(unknown, "Segment"), Is.EqualTo("base"));
            Assert.That(GetMapColorKey(null, "Phase"), Is.EqualTo("base"));

            var tied = FindObjectBlock(CreateObjectIndex(ReadRows("Wonen;3;SO;Sociaal;2;40;20;;;;", "Wonen;3;SO;Midden;2;40;20;;;;")), "3");
            Assert.That(GetMapColorKey(tied, "Segment"), Is.EqualTo("social"));
            Assert.That(GetMapColorKey(tied, "Base"), Is.EqualTo("base"));
        }

        [Test]
        public void DashboardMapUsesTheLatestHousingVersionAndTheLatestAvailablePlanPhase()
        {
            var block = FindObjectBlock(CreateObjectIndex(ReadRows("Wonen;3;BBP;Sociaal;2;40;1000;;;;",
                "Wonen;3;SO;Midden;2;40;10;;;;", "Niet-wonen;3;VO;;;;;Kantoor;200;;")), "3");

            Assert.That(GetMapColorKey(block, "Segment"), Is.EqualTo("mid-rent"));
            Assert.That(GetMapColorKey(block, "Dwellings"), Is.EqualTo("low"));
            Assert.That(GetMapColorKey(block, "Phase"), Is.EqualTo("vo"));
        }

        [TestCase("dashboard", true)]
        [TestCase("DashboardSloterdijk", true)]
        [TestCase("dashboardingSloterdijk", true)]
        [TestCase("DASHBOARDING", true)]
        [TestCase("dashboard-kubussen.city", true)]
        [TestCase(" dashboardBlokken", true)]
        [TestCase("Sloterdijk", false)]
        [TestCase("Mijn dashboard", false)]
        [TestCase("3", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void DashboardParticipationDependsOnTheCurrentLayerName(string name, bool expected)
        {
            var type = typeof(DashboardData).Assembly.GetType("Netherlands3D.Twin.DataSets.DashboardObjectBinding");
            var actual = type.GetMethod("IsDashboardLayerName").Invoke(null, new object[] { name });

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void AdditionalCsvSegmentsHaveTheSameStableColorForMapAndCharts()
        {
            var block = FindObjectBlock(CreateObjectIndex(ReadRows("Wonen;3;SO;Koop;2;40;20;;;;")), "3");
            Assert.That(GetMapColorKey(block, "Segment"), Is.EqualTo("segment:KOOP"));

            var type = typeof(DashboardData).Assembly.GetType("Netherlands3D.Twin.DataSets.DashboardColors");
            var method = type.GetMethod("GetColor");
            var palette = new System.Collections.Generic.Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
            var mapColor = method.Invoke(null, new object[] { "Koop", palette });
            var chartColor = method.Invoke(null, new object[] { "koop", palette });
            Assert.That(mapColor, Is.EqualTo(chartColor));
        }

        [TestCase("\"03\"", "03")]
        [TestCase("\"4N\"", "4N")]
        [TestCase("3", "3")]
        [TestCase("3.0", "3")]
        [TestCase("true", null)]
        [TestCase("null", null)]
        [TestCase("[]", null)]
        [TestCase("{}", null)]
        [TestCase("\" \"", null)]
        public void CityJsonBlockIdentifiersAcceptTextAndNumbersButRejectOtherTypes(string jsonValue, string expected)
        {
            var assembly = typeof(DashboardData).Assembly;
            var target = new GameObject("Dashboard block identifier test");
            try
            {
                var cityObject = target.AddComponent(assembly.GetType("Netherlands3D.CityJson.Structure.CityObject"));
                var attributeType = assembly.GetType("Netherlands3D.CityJson.Structure.CityObjectAttribute");
                var nodeType = attributeType.GetConstructors().Single(constructor => constructor.GetParameters().Length == 3).GetParameters()[2].ParameterType;
                var node = nodeType.GetMethod("Parse").Invoke(null, new object[] { jsonValue });
                var attribute = Activator.CreateInstance(attributeType, new[] { cityObject, "Blok", node });
                var attributes = (System.Collections.IDictionary)cityObject.GetType().GetProperty("Attributes").GetValue(cityObject);
                attributes.Add("Blok", attribute);
                var actual = assembly.GetType("Netherlands3D.Twin.DataSets.DashboardObjectBinding")
                    .GetMethod("ReadBlockIdentifier", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new object[] { cityObject });
                Assert.That(actual, Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static object GetMapColorKey(object block, string layer)
        {
            var assembly = typeof(DashboardData).Assembly;
            var mode = Enum.Parse(assembly.GetType("Netherlands3D.Twin.DataSets.DashboardMapLayer"), layer);
            return assembly.GetType("Netherlands3D.Twin.DataSets.DashboardMapStyling")
                .GetMethod("GetColorKey", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, new[] { block, mode });
        }

        private static object CreateObjectIndex(DashboardData data) => Activator.CreateInstance(
            typeof(DashboardData).Assembly.GetType("Netherlands3D.Twin.DataSets.DashboardObjectData"), new object[] { data });

        private static object FindObjectBlock(object index, string identifier) => index.GetType().GetMethod("Find").Invoke(index, new object[] { identifier });

        private static DashboardData ReadRows(params string[] rows)
        {
            const string header = "Type;Blok;Planproduct;Segment;Kamers;Min GBO;Aantal woningen;Categorie;BVO;Programma;Afwijking";
            using var reader = new StringReader(header + "\n" + string.Join("\n", rows));
            var data = DashboardCsv.Read(reader);
            Assert.That(data.SkippedLineNumbers, Is.Empty);
            return data;
        }

        // Keep the dashboard calculation types internal to the runtime assembly.
        private static object InvokeSummary(string method, params object[] arguments) =>
            typeof(DashboardData).Assembly.GetType("Netherlands3D.Twin.DataSets.DashboardSummary").GetMethod(method).Invoke(null, arguments);

        private static object Property(object value, string name) => value.GetType().GetProperty(name).GetValue(value);

        private static void AssertResult(object result, decimal? value, string status)
        {
            Assert.That(Property(result, "Value"), Is.EqualTo(value));
            Assert.That(Property(result, "Status").ToString(), Is.EqualTo(status));
            Assert.That(Property(result, "CompleteValue"), Is.EqualTo(status == "Complete" ? value : null));
        }

        [TestCase("DashboardCsvSample.csv", true, false)]
        [TestCase("BuildingColorCsvSample.csv", false, true)]
        public void RoutesEachCsvToOnlyOneAdapter(string fileName, bool supportsDashboard, bool supportsBuildingColors)
        {
            var dashboardAdapter = ScriptableObject.CreateInstance<DashboardCsvImportAdapter>();
            var buildingColorAdapter = ScriptableObject.CreateInstance<SubObjectColorCSVImportAdapter>();
            try
            {
                var file = new LocalFile { LocalFilePath = GetFixturePath(fileName) };
                Assert.That(dashboardAdapter.Supports(file), Is.EqualTo(supportsDashboard));
                Assert.That(buildingColorAdapter.Supports(file), Is.EqualTo(supportsBuildingColors));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(dashboardAdapter);
                UnityEngine.Object.DestroyImmediate(buildingColorAdapter);
            }
        }

        [TestCase("dashboard#2026.csv")]
        [TestCase("dashboard%23 met spatie.csv")]
        public void PreservesTheFileNameWhenAProjectUriIsRestored(string fileName)
        {
            var relativePath = Path.Combine("csv", fileName);
            var localPath = Path.Combine(Application.persistentDataPath, relativePath);
            var projectUri = AssetUriFactory.CreateProjectAssetUri(localPath);
            var restoredUri = AssetUriFactory.CreateProjectAssetUri(projectUri.ToString());

            Assert.That(restoredUri.Fragment, Is.Empty);
            Assert.That(restoredUri.Query, Is.Empty);
            Assert.That(Path.GetFullPath(AssetUriFactory.GetLocalPath(restoredUri)), Is.EqualTo(Path.GetFullPath(localPath)));
        }

        private static string GetFixturePath(string fileName)
        {
            return Path.Combine(Application.dataPath, "_Application", "Tests", "Editor", fileName);
        }
    }
}
