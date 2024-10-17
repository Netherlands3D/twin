using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Events;
using Netherlands3D.Twin.UI.LayerInspector;
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
        [SerializeField] private string assetsFolderName = "Assets";
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
            var assetsFolderPath = Path.Combine(Application.persistentDataPath, assetsFolderName);
            
            if (!Directory.Exists(assetsFolderPath))
            {
                Directory.CreateDirectory(assetsFolderPath);
            }
            
            var fileName = Path.GetFileNameWithoutExtension(file);
            var extension = Path.GetExtension(file).ToLower();
            var newFilePathRelative = Path.Combine(assetsFolderName, fileName + extension);
            var newFilePathAbsolute = Path.Combine(assetsFolderPath, fileName + extension);
            
            // Find a unique file name if a file with the same name already exists
            int index = 0;
            while (File.Exists(newFilePathAbsolute))
            {
                index++;
                var newFileName = $"{fileName}({index}){extension}";
                newFilePathRelative = Path.Combine(assetsFolderName, newFileName);
                newFilePathAbsolute = Path.Combine(assetsFolderPath, newFileName);
            }
            
            File.Move(absoluteFilePath, newFilePathAbsolute);
                
            if (extension.StartsWith('.'))
                extension = extension.Substring(1);
            
            var fileTypeEvent = fileTypeEvents.FirstOrDefault(fte => fte.Extension.ToLower() == extension);
            
            Debug.Log("processing file: " + newFilePathRelative);
            
            if(fileTypeEvent != null)
            {
                var localFile = new LocalFile()
                {
                    SourceUrl = newFilePathRelative,
                    LocalFilePath = newFilePathRelative
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
