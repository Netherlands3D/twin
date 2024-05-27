using System;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;

namespace Netherlands3D.Twin.Projects
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Project", fileName = "Project", order = 0)]
    public class Project : ScriptableObject
    {
        [DllImport("__Internal")] private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);
        [DllImport("__Internal")] private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);
        public const string DefaultFileName = "NL3D_Project_";
        public const string ProjectFileExtension = ".nl3d";
        
        [Header("Serialized data")]
        public int Version = 1;
        public string SavedTimestamp;
        public string UUID = "";
        public double[] CameraStartPosition; //X, Y, Z,- Assume RD for now
        public Vector3 CameraStartRotation; //Euler angles


        private ProjectStateHandler projectStateHandler;
        private ZipOutputStream zipOutputStream;
        private string lastSavePath = "";

        private bool isDirty = false;
        public bool IsDirty { 
            get => isDirty; 
            set{
                isDirty = value;
            }
        }

        public UnityEvent<Project> OnDataChanged = new UnityEvent<Project>();

        public void CopyFrom(Project project)
        {
            // Explicit copy of fields. Will be more complex once bin. files are saved
            Version = project.Version;
            SavedTimestamp = project.SavedTimestamp;
            UUID = project.UUID;
            CameraStartPosition = project.CameraStartPosition;
            CameraStartRotation = project.CameraStartRotation;

            IsDirty = true;
        }

        public void LoadFromFile(string filePath)
        {
            // Open the zip file
            using (FileStream fs = File.OpenRead(filePath))
            {
                using ZipInputStream zipInputStream = new ZipInputStream(fs);
                ZipEntry entry;
                while ((entry = zipInputStream.GetNextEntry()) != null)
                {
                    if (entry.Name == "project.json")
                    {
                        using StreamReader reader = new(zipInputStream);
                        string json = reader.ReadToEnd();
                        JsonConvert.PopulateObject(json, this);
                    }
                    else
                    {
                        // Extract the additional project files to our indexedDB
                        string persistentDataPath = Application.persistentDataPath + "/" + entry.Name;
                        using FileStream output = File.Create(persistentDataPath);
                        byte[] buffer = new byte[4096];
                        StreamUtils.Copy(zipInputStream, output, buffer);
                    }
                }
            }

            IsDirty = true;
            OnDataChanged.Invoke(this);
        }

        public void SaveAsFile(ProjectStateHandler projectStateHandler)
        {
            RefreshUUID();
            
            this.projectStateHandler = projectStateHandler;

            // Set the timestamp when the data was saved
            SavedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var readableTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH:mm");

            // Start the zip output stream with best compression
            lastSavePath = Application.persistentDataPath + $"/{DefaultFileName}{readableTimeStamp}{ProjectFileExtension}";
            zipOutputStream = new ZipOutputStream(File.Create(lastSavePath));
            zipOutputStream.SetLevel(9); // 0 - store only to 9 - means best compression

            // Generate the JSON data and add it to the project zip as the first file
            var jsonProject = JsonConvert.SerializeObject(this);
            var entry = new ZipEntry("project.json");
            zipOutputStream.PutNextEntry(entry);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonProject.ToString());
            zipOutputStream.Write(jsonBytes, 0, jsonBytes.Length);         

            // For now we can directly download the zip file (in the future we want to append more files to the zip, like meshes and textures etc.)
            FinishProjectFile();
        }

        private void FinishProjectFile()
        {
            // Finish the zip
            zipOutputStream.Finish();
            zipOutputStream.Close();

            IsDirty = false;

            // Make sure indexedDB is synced
            #if !UNITY_EDITOR && UNITY_WEBGL
            SyncFilesToIndexedDB(projectStateHandler.name, "ProjectReadyInIndexedDB");
            
            #elif UNITY_EDITOR
            //Request using file write dialog of unity editor where to copy the file from lastSavePath path
            Debug.Log("Project saved to: " + lastSavePath);
            var fileTargetPath = EditorUtility.SaveFilePanel("Save project", Application.persistentDataPath, DefaultFileName, ProjectFileExtension);
            if(fileTargetPath.Length > 0)
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

        public void ProjectReadyInIndexedDB()
        {
            DownloadFromIndexedDB($"{UUID}", projectStateHandler.name, "DoneDownloadZip");
        }  
        public void DoneDownloadProject()
        {
            Debug.Log("Downloading project file succeeded");
        }
        public void RefreshUUID()
        {
            UUID = Guid.NewGuid().ToString();
        }
    }
}