using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.DataTypeAdapters
{
    [Serializable]
    public class FileTypeEvent
    {
        public string Extension;
        [SerializeField] private ScriptableObject Adapter;
        public IDataTypeAdapter DataTypeAdapter => (IDataTypeAdapter)Adapter; //unfortunately interfaces are not serializable
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

        public void ProcessFile(string file)
        {
            if (file.EndsWith(','))
                file = file.Remove(file.Length - 1);

            string fileExtension = Path.GetExtension(file).ToLower();
            if (fileExtension.StartsWith('.'))
                fileExtension = fileExtension.Substring(1);

            var possibleFileTypeEvents = fileTypeEvents.Where(fte => fte.Extension == fileExtension);
            
            var path = Path.Combine(Application.persistentDataPath, file);
            var localFile = new LocalFile()
            {
                SourceUrl = AssetUriFactory.CreateProjectAssetUri(file).ToString(),
                LocalFilePath = path
            };
            
            foreach (var fte in possibleFileTypeEvents)
            {
                if (fte.DataTypeAdapter.Supports(localFile))
                {
                    fte.DataTypeAdapter.Execute(localFile);
                    return;
                }
            }

            Debug.Log("file type {" + fileExtension + "} does not have an associated processing function");
        }
    }
}