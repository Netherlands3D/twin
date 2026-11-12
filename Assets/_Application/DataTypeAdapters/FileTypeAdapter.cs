using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Configuration;
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

            string fileExtension = Path.GetExtension(file).ToLower();
            if (fileExtension.StartsWith('.'))
                fileExtension = fileExtension.Substring(1);

            if (fileExtension == "csv" && !CanImportCsv())
                return;

            var possibleFileTypeEvents = fileTypeEvents.Where(fte => fte.Extension == fileExtension);

            var path = Path.Combine(Application.persistentDataPath, file);
            var localFile = new LocalFile()
            {
                SourceUrl = AssetUriFactory.CreateProjectAssetUri(file).ToString(),
                LocalFilePath = path
            };

            var matched = AdapterChain(localFile, possibleFileTypeEvents);
            if (!matched && fileExtension == "csv")
                App.Snackbar.DisplayError("Dit CSV-bestand kan niet worden ingeladen. Controleer de kolommen en het bestandsformaat.");
        }

        internal static bool CanImportCsv()
        {
            if (Configurator.IsDashboardingEnabled)
                return true;

            App.Snackbar.DisplayError("CSV-import is niet beschikbaar.");
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
