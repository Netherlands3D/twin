using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
using Netherlands3D.Credentials;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.AssetBundles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Projects
{
    /// <summary>
    /// This class manages the state of the project (undo/redo) and handles saving and loading of the project as a file
    /// </summary>
    public class ProjectDataHandler : MonoBehaviour
    {
        [DllImport("__Internal")]
        [UsedImplicitly]
        private static extern void PreventDefaultShortcuts();
        
        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        [UsedImplicitly]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        [UsedImplicitly] private DataTypeChain fileImporter; // don't remove, this is used in LoadDefaultProject()
        [UsedImplicitly] private CredentialHandler credentialHandler; // don't remove, this is used in LoadDefaultProject()
        [SerializeField] private string defaultProjectFileName = "ProjectTemplate.nl3d";
       [SerializeField] private InputActionAsset applicationActionMap;

        public UnityEvent OnSaveStarted;
        public UnityEvent OnSaveCompleted;
        public UnityEvent OnLoadStarted;
        public UnityEvent<ProjectData> OnLoadCompleted;
        public UnityEvent OnLoadFailed;

        private static ProjectDataHandler instance;
        
        private string DefaultFileName = "NL3D_Project_";
        private string ProjectFileExtension = "nl3d";
        private string ProjectJsonFileNameInZip = "project.json";

        private ZipOutputStream zipOutputStream;

        private string lastSavePath;

        public static ProjectDataHandler Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<ProjectDataHandler>();

                return instance;
            }
            set { instance = value; }
        }

        private void Awake()
        {
            gameObject.name = gameObject.name + Guid.NewGuid();
            
            if (ProjectData.Current == null)
            {
                Debug.LogError("Current ProjectData object reference is not set in ProjectData", this.gameObject);
                return;
            }

            fileImporter = GetComponent<DataTypeChain>();
            credentialHandler = GetComponent<CredentialHandler>();

#if !UNITY_EDITOR && UNITY_WEBGL
            //Prevent default browser shortcuts for saving and undo/redo
            PreventDefaultShortcuts();
#endif
            //LoadDefaultProject();
            //TODO this is a quite dirty solution to postpone the loading flow of the application, but now its needed to preload assets 
            //for when a default project contains assetbundle assets from start, needing a solid preloader in the future
            FindObjectOfType<AssetBundleLoader>().OnAssetsLoaded.AddListener(OnPreloadedAssets);
        }

        private void OnPreloadedAssets()
        {
            LoadDefaultProject();
        }

        private void OnDestroy()
        {
            AssetBundleLoader loader = FindObjectOfType<AssetBundleLoader>();
            if (loader)
                loader.OnAssetsLoaded.RemoveListener(OnPreloadedAssets);
        }

        private void OnEnable()
        {
            credentialHandler.OnAuthorizationHandled.AddListener(fileImporter.DetermineAdapter);
            
            ToolService tools = ServiceLocator.GetService<ToolService>();
            tools.GetTool(ToolType.OpenProject).onOpen.AddListener(OpenProject);
            tools.GetTool(ToolType.SaveProject).onOpen.AddListener(SaveProject);
            
            InputService input =  ServiceLocator.GetService<InputService>();
            input.OpenProjectAction.performed += OpenProject;
            input.SaveProjectAction.performed += SaveProject;
        }

        private void OnDisable()
        {
            credentialHandler.OnAuthorizationHandled.RemoveListener(fileImporter.DetermineAdapter);
            
            ToolService tools = ServiceLocator.GetService<ToolService>();
            tools.GetTool(ToolType.OpenProject).onOpen.RemoveListener(OpenProject);
            tools.GetTool(ToolType.SaveProject).onOpen.RemoveListener(SaveProject);
            
            InputService input =  ServiceLocator.GetService<InputService>();
            input.OpenProjectAction.performed -= OpenProject;
            input.SaveProjectAction.performed -= SaveProject;
        }

        private void OpenProject(InputAction.CallbackContext ctx) => OpenProject();
        private void SaveProject(InputAction.CallbackContext ctx) => SaveProject();

        private void OpenProject()
        {
            FileOpen fileOpenerService = ServiceLocator.GetService<FileOpen>();
            fileOpenerService.OpenFile("nl3d");
            ToolService tools = ServiceLocator.GetService<ToolService>();
            tools.GetTool(ToolType.OpenProject).Close();
        }

        public void SaveProject()
        {
            SaveAsFile();
            ToolService tools = ServiceLocator.GetService<ToolService>();
            tools.GetTool(ToolType.SaveProject).Close();
        }

        private void LoadDefaultProject()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var url = Path.Combine(Application.streamingAssetsPath, defaultProjectFileName);
            Debug.Log("loading default project file: " + url);
            credentialHandler.Uri = new Uri(url); //url should never be empty and if it is we expect an exception

            credentialHandler.ApplyCredentials();
#else
            var filePath = Path.Combine(Application.persistentDataPath, Application.streamingAssetsPath, defaultProjectFileName);
            Debug.Log("loading default project file: " + filePath);
            LoadFromFile(filePath);
#endif
        }

        public ProjectData LoadFromFile(string filePath)
        {
            OnLoadStarted.Invoke();

            if (filePath.ToLower().EndsWith(".nl3d"))
            {
                Debug.Log("loading nl3d file: " + filePath);
                App.Layers.Remove(ProjectData.Current.RootLayer);
                ProjectData.Current.ClearFunctionalityData();
            
                Resources.UnloadUnusedAssets();

                // Open the zip file
                using FileStream fs = File.OpenRead(filePath);

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
                }
                var project = ProjectData.Current;
                OnLoadCompleted.Invoke(project);
                return project;
            }

            OnLoadFailed.Invoke();
            return null;
        }

        private readonly JsonSerializerSettings serializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            SerializationBinder = new DataContractSerializationBinder(new DefaultSerializationBinder())
        };
        
        private void LoadJSON(string json)
        {
            try
            {
                JsonConvert.PopulateObject(json, ProjectData.Current, serializerSettings);
            }
            catch(Exception e)
            {
                Debug.LogException(e);
            }

            ProjectData.Current.RootLayer.ReconstructParentsRecursive();

            ProjectData.Current.RootLayer.UpdateLayerTreeOrder(0);
            Debug.Log("Loaded project with uuid: " + ProjectData.Current.UUID);
            ProjectData.Current.OnDataChanged.Invoke(ProjectData.Current);
            ProjectData.Current.LoadVisualizations();
        }

        public void SaveAsFile()
        {
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
            Debug.Log("SAVING PROJECT DATA path " + lastSavePath);
            SaveFile(lastSavePath);
        }

        private void WriteProjectAssetsToZipfile(ProjectData projectData, ZipOutputStream zipOutputStream)
        {
            var projectAssets = projectData
                .GetAssets().Where(asset => asset.IsStoredInProject)
                .ToList();
            
            foreach (var layerAsset in projectAssets)
            {
                WriteProjectAssetToZipFile(layerAsset, zipOutputStream);
            }
        }

        private void WriteProjectAssetToZipFile(LayerAsset layerAsset, ZipOutputStream zipOutputStream)
        {
            var relativePath = layerAsset.Uri.LocalPath.TrimStart('\\', '/');
            var absolutePath = Path.Combine(Application.persistentDataPath, relativePath);
            
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
            SyncFilesToIndexedDB(gameObject.name, "ProjectSavedToIndexedDB");
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
            DownloadFromIndexedDB($"{fileName}", gameObject.name, "DownloadedProject");
        }
        
        public void DownloadedProject()
        {
            Debug.Log("Downloading project file succeeded");
        }
    }
}