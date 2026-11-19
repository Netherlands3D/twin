using System;
using System.IO;
using System.Linq;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Projects;
using NUnit.Framework;
using UnityEngine;

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
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            var phase = phases.GetValue(0);

            AssertResult(Property(phase, "Dwellings"), 10m, "Partial");
            AssertResult(Property(phase, "GrossFloorArea"), 25m, "Partial");
            AssertResult(Property(phase, "AverageMinimumFloorArea"), null, "Partial");
            Assert.That(Property(phase, "HasData"), Is.True);

            var breakdowns = InvokeSummary("CalculateBreakdowns", data);
            foreach (var field in new[] { "Item1", "Item2", "Item3", "Item4" })
            {
                var rows = (Array)breakdowns.GetType().GetField(field).GetValue(breakdowns);
                var values = (Array)Property(rows.GetValue(0), "Values");
                AssertResult(values.GetValue(0), field == "Item1" || field == "Item2" ? 10m : 25m, "Partial");
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
            AssertResult(Property(phases.GetValue(1), "Dwellings"), 12m, "Partial");
            AssertResult(Property(phases.GetValue(1), "GrossFloorArea"), 100m, "Complete");
            AssertResult(Property(phases.GetValue(2), "Dwellings"), null, "Unknown");
            var breakdowns = InvokeSummary("CalculateBreakdowns", data);
            var segments = (Array)breakdowns.GetType().GetField("Item1").GetValue(breakdowns);
            AssertResult(((Array)Property(segments.GetValue(0), "Values")).GetValue(1), 12m, "Partial");
            var blockPhases = (Array)InvokeSummary("CalculatePhases", data, "A");
            AssertResult(Property(blockPhases.GetValue(1), "Dwellings"), 12m, "Complete");
        }

        [TestCase("10", "40", "30", "60", 55, "Complete")]
        [TestCase("0", "", "10", "40", 40, "Complete")]
        [TestCase("", "40", "10", "40", null, "Partial")]
        [TestCase("10", "", "10", "40", null, "Partial")]
        [TestCase("0", "", "0", "", null, "NotApplicable")]
        [TestCase("10", "", "10", "", null, "Unknown")]
        public void RequiresCompleteWeightsAndAreasForTheWeightedAverage(string firstCount, string firstArea, string secondCount, string secondArea,
            int? expected, string status)
        {
            var data = ReadRows(
                $"Wonen;A;BBP;Sociaal;2;{firstArea};{firstCount};;;;",
                $"Wonen;A;BBP;Sociaal;3;{secondArea};{secondCount};;;;");
            var phases = (Array)InvokeSummary("CalculatePhases", data, null);
            AssertResult(Property(phases.GetValue(0), "AverageMinimumFloorArea"), expected, status);
        }

        [Test]
        public void OnlyCalculatesSharesAndChangesFromCompleteResults()
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
