using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.DataSets;
using Netherlands3D.Twin.Layers.LayerPresets;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [Serializable]
    public class FileTypeEvent
    {
        public string Extension;
        [SerializeField] private ScriptableObject Adapter;
        public IDataTypeAdapter<object> DataTypeAdapter => (IDataTypeAdapter<object>)Adapter; //unfortunately interfaces are not serializable
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/FileTypeAdapter", fileName = "FileTypeAdapter", order = 0)]
    public class FileTypeAdapter : ScriptableObject
    {
        [SerializeField] private List<FileTypeEvent> fileTypeEvents;

        public void ProcessFiles(string files)
        {
            var filesAsArray = files.Split(",");
            foreach (string file in filesAsArray)
            {
                ProcessFile(file);
            }
        }

        //the void signature is needed for event listeners
        public void ProcessFile(string file)
        {
            if (file.EndsWith(','))
                file = file.Remove(file.Length - 1);

            string fileExtension = Path.GetExtension(file).ToLowerInvariant();
            if (fileExtension.StartsWith('.'))
                fileExtension = fileExtension.Substring(1);

            var dashboardFile = fileExtension is "csv" or "xlsx";
            if (dashboardFile && !CanImportCsv(fileExtension.ToUpperInvariant()))
                return;

            var possibleFileTypeEvents = fileTypeEvents.Where(fte => fte.Extension == fileExtension);

            var path = Path.Combine(Application.persistentDataPath, file);
            var localFile = new LocalFile()
            {
                SourceUrl = AssetUriFactory.CreateProjectAssetUri(file).ToString(),
                LocalFilePath = path
            };

            var matched = AdapterChain(localFile, possibleFileTypeEvents);
            if (!matched && dashboardFile)
                DisplayUnmatchedFile(path, fileExtension);
        }

        private static void DisplayUnmatchedFile(string path, string extension)
        {
            var details = "Controleer de kolommen en het bestandsformaat.";
            if (extension == "csv")
            {
                try
                {
                    using var reader = File.OpenText(path);
                    DashboardCsv.ValidateHeaders(reader);
                }
                catch (DashboardImportFormatException exception)
                {
                    details = exception.Message;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or CsvHelperException)
                {
                    details = "Controleer of het bestand leesbaar is en als CSV is opgeslagen.";
                }
            }

            App.Snackbar.DisplayFailure($"Dit {extension.ToUpperInvariant()}-bestand kan niet worden ingeladen. {details}");
        }

        internal static bool CanImportCsv(string format = "CSV")
        {
            if (Configurator.IsDashboardingEnabled)
                return true;

            App.Snackbar.DisplayFailure($"{format}-import is niet beschikbaar.");
            return false;
        }

        private bool AdapterChain(LocalFile localFile, IEnumerable<FileTypeEvent> possibleFileTypeEvents)
        {
            var matched = false;

            // Get our interface references
            foreach (var fte in possibleFileTypeEvents)
            {
                var adapter = fte.DataTypeAdapter;
                if (adapter.Supports(localFile))
                {
                    matched = true;
                    var preset = adapter.Execute(localFile);
                    
                    if (preset is LayerPresetResult result)
                    {
                        App.Layers.Add(result);
                    }
                    else if (preset is LayerPresetArgs layerPresetArgs) 
                    {
                        App.Layers.Add(layerPresetArgs);
                    }
                }
            }

            return matched;
        }
    }
}
