using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
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
        // [SerializeField] private string assetsFolderName = "Assets";
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
            
            var absoluteFilePath = Path.Combine(Application.persistentDataPath, file);
            
            var fileName = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file).ToLower();

            var guid = Guid.NewGuid();
            var newFilePathRelative = guid + extension;
            var newFilePathAbsolute = Path.Combine(Application.persistentDataPath, guid + extension);
            
            Debug.Log(absoluteFilePath + " will be copied to: " + newFilePathAbsolute);
            File.Copy(absoluteFilePath, newFilePathAbsolute);
                
            if (extension.StartsWith('.'))
                extension = extension.Substring(1);
            
            var fileTypeEvent = fileTypeEvents.FirstOrDefault(fte => fte.Extension.ToLower() == extension);
            
            Debug.Log("processing file: " + newFilePathRelative);
            
            if(fileTypeEvent != null)
            {
                var localFile = new LocalFile()
                {
                    SourceUrl = newFilePathRelative,
                    LocalFilePath = newFilePathRelative,
                    OriginalFileName = fileName
                };

                fileTypeEvent.FileReceived.Invoke(localFile);
            }
            else
            {
                Debug.Log("file type {" + extension + "} does not have an associated processing function");
            }
        }
    }
}
