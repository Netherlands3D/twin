using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.DataTypeAdapters;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.DataTypeAdapters
{
    [Serializable]
    public class FileTypeEvent
    {
        public string Extension;
        public UnityEvent<LocalFile> FileReceived;
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
            
            var fileTypeEvent = fileTypeEvents.FirstOrDefault(fte => fte.Extension == fileExtension);
            
            Debug.Log("file: " + file);
            
            if(fileTypeEvent != null)
            {
                var localFile = new LocalFile()
                {
                    SourceUrl = file,
                    LocalFilePath = file
                };

                fileTypeEvent.FileReceived.Invoke(localFile);
            }
            else
            {
                Debug.Log("file type {" + fileExtension + "} does not have an associated processing function");
            }
        }
    }
}
