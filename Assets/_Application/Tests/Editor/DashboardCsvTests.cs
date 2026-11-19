using System;
using System.IO;
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

            Assert.That(data.SourceRowCount, Is.EqualTo(5));
            Assert.That(data.SkippedLineNumbers, Is.EqualTo(new[] { 6 }));
            Assert.That(data.Housing.Count, Is.EqualTo(2));
            Assert.That(data.NonHousing.Count, Is.EqualTo(1));
            Assert.That(data.ProgramChanges.Count, Is.EqualTo(1));

            var housing = data.Housing[0];
            Assert.That(housing.Block, Is.EqualTo("01"));
            Assert.That(housing.Phase, Is.EqualTo("SO"));
            Assert.That(housing.SubmissionDate, Is.EqualTo(new DateTime(2026, 1, 15)));
            Assert.That(housing.Segment, Is.EqualTo("Sociaal"));
            Assert.That(housing.Rooms, Is.EqualTo(2));
            Assert.That(housing.MinimumUsableFloorArea, Is.EqualTo(42.5m));
            Assert.That(housing.DwellingCount, Is.EqualTo(12));
            Assert.That(data.Housing[1].DwellingCount, Is.Zero);

            var nonHousing = data.NonHousing[0];
            Assert.That(nonHousing.Block, Is.EqualTo("01"));
            Assert.That(nonHousing.Phase, Is.EqualTo("VO"));
            Assert.That(nonHousing.Category, Is.EqualTo("Maatschappelijk"));
            Assert.That(nonHousing.GrossFloorArea, Is.EqualTo(320.5m));

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
            const string header = "Type;Blok;Planproduct;Datum indiening;Segment;Kamers;Min GBO;Aantal woningen;Categorie;BVO;Programma;Afwijking";
            var row = type == "Wonen"
                ? $"Wonen;01;SO;;Sociaal;2;42.5;{quantity};;;;"
                : $"Niet-wonen;01;SO;;;;;;Kantoor;{quantity};;";
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
