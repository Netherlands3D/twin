using System;
using System.IO;
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
            DashboardData data;
            try
            {
                using var reader = File.OpenText(localFile.LocalFilePath);
                data = DashboardCsv.Read(reader);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException or FormatException)
            {
                App.Snackbar.DisplayError("Dit CSV-bestand kan niet worden ingeladen. Controleer de kolommen en het bestandsformaat.");
                return null;
            }

            if (data.Housing.Count == 0 && data.NonHousing.Count == 0 && data.ProgramChanges.Count == 0)
            {
                App.Snackbar.DisplayError("Dit CSV-bestand bevat geen bruikbare gegevens." + GetSkippedLinesMessage(data));
                return null;
            }

            if (data.SkippedRowCount > 0)
            {
                App.Snackbar.DisplayError("Het CSV-bestand is ingeladen." + GetSkippedLinesMessage(data));
            }

            var source = AssetUriFactory.ConvertLocalFileToAssetUri(localFile);
            return new DashboardCsvPreset.Args(localFile.FileName, new DashboardCsvPropertyData(source, data));
        }

        private static string GetSkippedLinesMessage(DashboardData data)
        {
            return data.SkippedRowCount switch
            {
                0 => string.Empty,
                1 => $" Regel {data.SkippedLineNumbers[0]} is overgeslagen.",
                _ => $" De regels {string.Join(", ", data.SkippedLineNumbers)} zijn overgeslagen."
            };
        }
    }
}
