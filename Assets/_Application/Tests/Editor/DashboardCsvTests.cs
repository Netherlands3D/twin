using System.IO;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataTypeAdapters;
using Netherlands3D.Twin.Projects;
using NUnit.Framework;
using UnityEngine;

namespace Netherlands3D.Twin.Tests
{
    public class DashboardCsvTests
    {
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
