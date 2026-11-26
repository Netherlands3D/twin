using System;
using System.IO;
using System.Linq;
using CsvHelper;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/Dashboard CSV Import Adapter", fileName = "DashboardCsvImportAdapter")]
    public sealed class DashboardCsvImportAdapter : ScriptableObject, IDataTypeAdapter<LayerPresetArgs>
    {
        public bool Supports(LocalFile localFile)
        {
            if (DashboardXlsx.IsWorkbook(localFile.LocalFilePath))
                return true;

            try
            {
                using var reader = File.OpenText(localFile.LocalFilePath);
                return DashboardCsv.HasDashboardColumns(reader);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException)
            {
                return false;
            }
        }

        public LayerPresetArgs Execute(LocalFile localFile)
        {
            var workbook = DashboardXlsx.IsWorkbook(localFile.LocalFilePath);
            var format = workbook ? "XLSX" : "CSV";
            if (!FileTypeAdapter.CanImportCsv(format))
                return null;

            DashboardData data;
            try
            {
                data = DashboardCsv.ReadFile(localFile.LocalFilePath);
            }
            catch (DashboardImportFormatException exception)
            {
                App.Snackbar.DisplayFailure($"Dit {format}-bestand kan niet worden ingeladen. {exception.Message}");
                return null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException or FormatException)
            {
                App.Snackbar.DisplayFailure($"Dit {format}-bestand kan niet worden ingeladen. Controleer het bestandsformaat.");
                return null;
            }

            if (data.Housing.Count == 0 && data.NonHousing.Count == 0 && data.ProgramChanges.Count == 0)
            {
                App.Snackbar.DisplayFailure($"Dit {format}-bestand bevat geen bruikbare gegevens." + GetSkippedLinesMessage(data));
                return null;
            }

            var source = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);
            var propertyData = new DashboardCsvPropertyData(source, data);
            var preset = new DashboardCsvPreset.Args(localFile.FileName, propertyData);

            foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
            {
                if (layer.IsDisposed || !layer.HasProperty<DashboardCsvPropertyData>())
                    continue;

                if (propertyData.LogoFile == null)
                    propertyData.LogoFile = layer.GetProperty<DashboardCsvPropertyData>().LogoFile;

                var parent = layer.ParentLayer;
                var siblingIndex = layer.SiblingIndex;
                foreach (var child in layer.ChildrenLayers.ToArray())
                    child.SetParent(parent, siblingIndex++);

                App.Layers.Remove(layer);
            }

            return preset;
        }

        internal static string GetSkippedLinesMessage(DashboardData data)
        {
            if (data.SkippedRowCount == 0)
                return string.Empty;

            const int maxVisibleRows = 3;
            var lines = data.SkippedRows.Select(row => $"Rij {row.LineNumber}: {row.Reason}").ToArray();
            var message = "\n" + string.Join("\n", lines.Take(maxVisibleRows));
            if (lines.Length <= maxVisibleRows)
                return message;

            Debug.LogWarning("Overgeslagen dashboardrijen:\n" + string.Join("\n", lines));
            return message + $"\nNog {lines.Length - maxVisibleRows} overgeslagen. Zie de Console voor alle rijen.";
        }
    }
}
