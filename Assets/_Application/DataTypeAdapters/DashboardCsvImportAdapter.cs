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

            if (!string.Equals(Path.GetExtension(localFile.LocalFilePath), ".csv", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using var reader = File.OpenText(localFile.LocalFilePath);
                return !DashboardCsv.HasBuildingColorColumns(reader);
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
                App.Snackbar.DisplayError($"Dit {format}-bestand kan niet worden ingeladen. {exception.Message}");
                return null;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException or FormatException)
            {
                App.Snackbar.DisplayError($"Dit {format}-bestand kan niet worden ingeladen. Controleer het bestandsformaat.");
                return null;
            }

            if (data.Housing.Count == 0 && data.NonHousing.Count == 0 && data.ProgramChanges.Count == 0)
            {
                App.Snackbar.DisplayError($"Dit {format}-bestand bevat geen bruikbare gegevens." + GetSkippedLinesMessage(data));
                return null;
            }

            if (data.SkippedRowCount > 0)
                App.Snackbar.DisplayError($"Het {format}-bestand is ingeladen met overgeslagen rijen." + GetSkippedLinesMessage(data));

            var source = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);
            var preset = new DashboardCsvPreset.Args(localFile.FileName, new DashboardCsvPropertyData(source, data));

            foreach (var layer in ProjectData.Current.RootLayer.GetFlatHierarchy())
            {
                if (layer.IsDisposed || !layer.HasProperty<DashboardCsvPropertyData>())
                    continue;

                var parent = layer.ParentLayer;
                var siblingIndex = layer.SiblingIndex;
                foreach (var child in layer.ChildrenLayers.ToArray())
                    child.SetParent(parent, siblingIndex++);

                App.Layers.Remove(layer);
            }

            return preset;
        }

        private static string GetSkippedLinesMessage(DashboardData data)
        {
            if (data.SkippedRowCount == 0)
                return string.Empty;

            return "\n" + string.Join("\n", data.SkippedRows.Select(row => $"Rij {row.LineNumber}: {row.Reason}"));
        }
    }
}
