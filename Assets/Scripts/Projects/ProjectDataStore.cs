using System;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    public class ProjectDataStore
    {
        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);
        [DllImport("__Internal")]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        private readonly JsonSerializerSettings serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        private const string DefaultFileName = "NL3D_Project_";
        private const string ProjectFileExtension = ".nl3d";
        private const string ProjectJsonFileNameInZip = "project.json";

        private ProjectDataHandler projectDataHandler;
        private ZipOutputStream zipOutputStream;

        private string lastSavePath;

        public void LoadFromFile(string fileName)
        {
            ProjectData.Current.RootLayer.DestroyLayer();

            // Open the zip file
            using FileStream fs = File.OpenRead(Path.Combine(Application.persistentDataPath, fileName));

            //Extract specific project.json from zip using CsharpLib
            using ZipFile zf = new(fs);

            foreach (ZipEntry zipEntry in zf)
            {
                if (!zipEntry.IsFile) continue;

                if (zipEntry.Name == ProjectJsonFileNameInZip)
                {
                    using Stream zipStream = zf.GetInputStream(zipEntry);
                    using StreamReader sr = new(zipStream);
                    string json = sr.ReadToEnd();

                    LoadJSON(json);
                }
                else
                {
                    //TODO: Future Project files can have more files in the zip, like meshes and textures etc.
                    Debug.Log("Other file found in Project zip. Ignoring for now.");

                    // todo add failed loading event
                }
            }
        }
        
        private void LoadJSON(string json)
        {
            ProjectData.Current.isLoading = true;
            JsonConvert.PopulateObject(json, ProjectData.Current, serializerSettings);
            ProjectData.Current.RootLayer.ReconstructParentsRecursive();
            Debug.Log("loaded project with uuid: " + ProjectData.Current.UUID);
            ProjectData.Current.OnDataChanged.Invoke(ProjectData.Current);
            ProjectData.Current.isLoading = false;
        }
        
        public void SaveAsFile(ProjectDataHandler projectDataHandler)
        {
            ProjectData.Current.RefreshUUID();

            // Set the timestamp when the data was saved
            ProjectData.Current.SavedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var readableTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");

            // Start the zip output stream
            var lastSavePath = Application.persistentDataPath + $"/{DefaultFileName}{readableTimeStamp}{ProjectFileExtension}";
            zipOutputStream = new ZipOutputStream(File.Create(lastSavePath));
            zipOutputStream.SetLevel(9); // 0-9 where 9 means best compression

            var jsonProject = JsonConvert.SerializeObject(this, serializerSettings);
            var entry = new ZipEntry(ProjectJsonFileNameInZip);
            zipOutputStream.PutNextEntry(entry);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonProject.ToString());
            zipOutputStream.Write(jsonBytes, 0, jsonBytes.Length);

            // For now we can directly download the zip file (in the future we want to append more files to the zip, like meshes and textures etc.)
            FinishProjectFile(lastSavePath);
        }

        private void FinishProjectFile(string lastSavePath)
        {
            // Finish the zip
            zipOutputStream.Finish();
            zipOutputStream.Close();

            // Make sure indexedDB is synced
#if !UNITY_EDITOR && UNITY_WEBGL
            this.lastSavePath = lastSavePath; 
            SyncFilesToIndexedDB(projectDataHandler.name, "ProjectSavedToIndexedDB");
#elif UNITY_EDITOR
            //Request using file write dialog of unity editor where to copy the file from lastSavePath path
            var fileName = Path.GetFileNameWithoutExtension(lastSavePath);
            var fileExtention = Path.GetExtension(lastSavePath).Replace(".", "");
            var fileTargetPath = EditorUtility.SaveFilePanel("Save project", Application.persistentDataPath, fileName, fileExtention);
            if (fileTargetPath.Length > 0)
            {
                File.Copy(lastSavePath, fileTargetPath, true);
            }

            //Open the folder where the file is saved
            EditorUtility.RevealInFinder(fileTargetPath);
#endif
        }

        public void AppendFileToZip(string fileName)
        {
            var persistentDataPath = Application.persistentDataPath + "/" + fileName;
            Debug.Log("Appending file to zip: " + persistentDataPath);

            byte[] buffer = new byte[4096];
            var randomFileTag = DateTime.Now.ToString("yyyyMMddHHmmss");
            ZipEntry entry = new ZipEntry(randomFileTag + "_" + fileName);
            zipOutputStream.PutNextEntry(entry);

            using FileStream fs = File.OpenRead(persistentDataPath);
            StreamUtils.Copy(fs, zipOutputStream, buffer);
        }
        
        public void ProjectSavedToIndexedDB()
        {
            var fileName = Path.GetFileName(lastSavePath);
            DownloadFromIndexedDB($"{fileName}", projectDataHandler.name, "DownloadedProject");
        }
    }
}