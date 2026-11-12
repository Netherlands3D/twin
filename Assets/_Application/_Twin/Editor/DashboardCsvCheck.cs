using System;
using System.IO;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Editor
{
    public static class DashboardCsvCheck
    {
        [MenuItem("Tools/Dashboard CSV testen")]
        public static void Run()
        {
            var path = EditorUtility.OpenFilePanel("Kies het dashboard-testbestand", @"D:\Data dashboard", "csv");
            if (string.IsNullOrEmpty(path))
                return;

            var csv = File.ReadAllText(path);
            using var reader = new StringReader(csv);
            var data = DashboardCsv.Read(reader);

            var counts = $"{data.SourceRowCount} / {data.Housing.Count} / {data.NonHousing.Count} / {data.ProgramChanges.Count}";
            if (data.SourceRowCount != 324 || data.Housing.Count != 179 || data.NonHousing.Count != 104 || data.ProgramChanges.Count != 6 || data.SkippedRowCount != 0)
                throw new InvalidOperationException($"Onverwachte aantallen: {counts}");

            Debug.Log($"Dashboard-CSV geslaagd: {counts}");

            const string invalidRow = "Wonen;TEST;SO;;Sociaal;ongeldig;25;1;;;;";
            using var invalidReader = new StringReader(csv.TrimEnd('\r', '\n') + Environment.NewLine + invalidRow);

            var mixedData = DashboardCsv.Read(invalidReader);
            if (mixedData.SourceRowCount != 325 || mixedData.SkippedRowCount != 1 || mixedData.SkippedLineNumbers[0] != 326 ||
                mixedData.Housing.Count != data.Housing.Count || mixedData.NonHousing.Count != data.NonHousing.Count ||
                mixedData.ProgramChanges.Count != data.ProgramChanges.Count)
                throw new InvalidOperationException("De CSV met een ongeldige rij gaf onverwachte aantallen of een verkeerd regelnummer.");

            Debug.Log("Dashboard-CSV geslaagd: regel 326 overgeslagen. De dashboardaantallen zijn gelijk gebleven.");
        }

        [MenuItem("Tools/Dashboard CSV in geopend project testen")]
        public static void CheckOpenProject()
        {
            if (!Application.isPlaying || ProjectData.Current == null)
            {
                Debug.LogWarning("Open het NL3D-project in Play Mode om de dashboardgegevens te controleren.");
                return;
            }

            var hasDashboardLayer = false;
            foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
            {
                var csv = layer.GetProperty<DashboardCsvPropertyData>();
                if (csv == null || layer.IsDisposed)
                    continue;

                hasDashboardLayer = true;
                var data = csv.Data;
                if (data == null)
                {
                    Debug.LogError($"Dashboard-CSV ontbreekt voor laag '{layer.Name}'.");
                    continue;
                }

                Debug.Log($"Dashboard-CSV '{layer.Name}' bevat {data.SourceRowCount} bronrijen, {data.Housing.Count} woonrijen, " +
                    $"{data.NonHousing.Count} niet-woonrijen, {data.ProgramChanges.Count} programmawijzigingen en {data.SkippedRowCount} overgeslagen rijen.");
            }

            if (!hasDashboardLayer)
                Debug.LogWarning("Het geopende project bevat geen dashboard-CSV-lagen.");
        }
    }
}
