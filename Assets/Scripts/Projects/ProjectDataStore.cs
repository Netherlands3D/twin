using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    [CreateAssetMenu(fileName = "ProjectDataStore", menuName = "Netherlands3D/Twin/ProjectDataStore", order = 1)]
    public class ProjectDataStore : ScriptableObject
    {
        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        [UsedImplicitly]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        private readonly JsonSerializerSettings serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        [SerializeField] private string DefaultFileName = "NL3D_Project_";
        [SerializeField] private string ProjectFileExtension = "nl3d";
        [SerializeField] private string ProjectJsonFileNameInZip = "project.json";

        private ProjectDataHandler projectDataHandler;
        private ZipOutputStream zipOutputStream;

        private string lastSavePath;

        public void LoadFromFile(string fileName)
        {
            ProjectData.Current.RootLayer.DestroyLayer();
            ProjectData.Current.ClearFunctionalityData();
            
            // Open the zip file
            using FileStream fs = File.OpenRead(Path.Combine(Application.persistentDataPath, fileName));

            //Extract specific project.json from zip using CsharpLib
            using ZipFile zf = new(fs);

            foreach (ZipEntry zipEntry in zf)
            {
                // TODO: this does not yet support directories
                if (!zipEntry.IsFile) continue;

                using Stream zipStream = zf.GetInputStream(zipEntry);
                if (zipEntry.Name == ProjectJsonFileNameInZip)
                {
                    using StreamReader sr = new(zipStream);
                    string json = sr.ReadToEnd();

                    LoadJSON(json);
                    continue;
                }
                
                string fullZipToPath = Path.Combine(Application.persistentDataPath, zipEntry.Name);
                using FileStream streamWriter = File.Create(fullZipToPath);
                zipStream.CopyTo(streamWriter);
        
                Console.WriteLine("Extracted: " + zipEntry.Name);
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
            this.projectDataHandler = projectDataHandler;
            ProjectData.Current.RefreshUUID();

            // Set the timestamp when the data was saved
            ProjectData.Current.SavedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var readableTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");

            // Start the zip output stream
            var lastSavePath = Application.persistentDataPath + $"/{DefaultFileName}{readableTimeStamp}.{ProjectFileExtension}";
            zipOutputStream = new ZipOutputStream(File.Create(lastSavePath));
            zipOutputStream.SetLevel(9); // 0-9 where 9 means best compression

            var projectData = ProjectData.Current;
            WriteProjectToZip(projectData, zipOutputStream);
            WriteProjectAssetsToZipfile(projectData, zipOutputStream);

            zipOutputStream.Finish();
            zipOutputStream.Close();

            SaveFile(lastSavePath);
        }

        private void WriteProjectAssetsToZipfile(ProjectData projectData, ZipOutputStream zipOutputStream)
        {
            var projectAssets = projectData
                .GetAssets().Where(asset => asset.IsStoredInProject)
                .ToList();
            Debug.Log("Found " + projectAssets.Count() + " project assets in project");
            
            foreach (var layerAsset in projectAssets)
            {
                WriteProjectAssetToZipFile(layerAsset, zipOutputStream);
            }
        }

        private void WriteProjectAssetToZipFile(LayerAsset layerAsset, ZipOutputStream zipOutputStream)
        {
            var relativePath = layerAsset.Uri.LocalPath.TrimStart('\\', '/');
            var absolutePath = Path.Combine(Application.persistentDataPath, relativePath);
            Debug.Log("Saving asset from " + relativePath);

            var entry = new ZipEntry(relativePath);
            zipOutputStream.PutNextEntry(entry);
            byte[] fileBytes = File.ReadAllBytes(absolutePath);
            zipOutputStream.Write(fileBytes, 0, fileBytes.Length);
        }

        private void WriteProjectToZip(ProjectData projectData, ZipOutputStream zipOutputStream)
        {
            var jsonProject = JsonConvert.SerializeObject(projectData, serializerSettings);
            var entry = new ZipEntry(ProjectJsonFileNameInZip);
            zipOutputStream.PutNextEntry(entry);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonProject.ToString());
            zipOutputStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        private void SaveFile(string lastSavePath)
        {
            // Make sure indexedDB is synced
#if !UNITY_EDITOR && UNITY_WEBGL
            this.lastSavePath = lastSavePath; 
            SyncFilesToIndexedDB(projectDataHandler.name, "ProjectSavedToIndexedDB");
#elif UNITY_EDITOR
            //Request using file write dialog of unity editor where to copy the file from lastSavePath path
            var fileName = Path.GetFileName(lastSavePath);
            var fileTargetPath = EditorUtility.SaveFilePanel("Save project", Application.persistentDataPath, fileName, ProjectFileExtension);
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