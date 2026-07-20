using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Netherlands3D.Credentials;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Functionalities.AssetBundles;
using Netherlands3D.Services;
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

        [UsedImplicitly] private DataTypeChain fileImporter; // don't remove, this is used in LoadDefaultProject()
        [UsedImplicitly] private CredentialHandler credentialHandler; // don't remove, this is used in LoadDefaultProject()
        [SerializeField] private string defaultProjectFileName = "ProjectTemplate.nl3d";
        [SerializeField] private ProjectDataStore projectDataStore;
        [SerializeField] private InputActionAsset applicationActionMap;

        public UnityEvent OnSaveStarted;
        public UnityEvent OnSaveCompleted;
        public UnityEvent OnLoadStarted;
        public UnityEvent<ProjectData> OnLoadCompleted;
        public UnityEvent OnLoadFailed;

        private static ProjectDataHandler instance;

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
        }

        private void OnDisable()
        {
            credentialHandler.OnAuthorizationHandled.RemoveListener(fileImporter.DetermineAdapter);
            
            ToolService tools = ServiceLocator.GetService<ToolService>();
            tools.GetTool(ToolType.OpenProject).onOpen.RemoveListener(OpenProject);
            tools.GetTool(ToolType.SaveProject).onOpen.RemoveListener(SaveProject);
        }

        private void OpenProject()
        {
            FileOpen fileOpenerService = ServiceLocator.GetService<FileOpen>();
            fileOpenerService.OpenFile("nl3d");
            ToolService tools = ServiceLocator.GetService<ToolService>();
            tools.GetTool(ToolType.OpenProject).Close();
        }

        public void SaveProject()
        {
            Debug.Log("SAVING PROJECT DATA");
            projectDataStore.SaveAsFile(this);
            Debug.Log("SAVING PROJECT DATA Finished");
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
            projectDataStore.LoadFromFile(filePath);
#endif
        }

        public ProjectData LoadFromFile(string filePath)
        {
            OnLoadStarted.Invoke();

            if (filePath.ToLower().EndsWith(".nl3d"))
            {
                Debug.Log("loading nl3d file: " + filePath);
                var project = projectDataStore.LoadFromFile(filePath);
                OnLoadCompleted.Invoke(project);
                return project;
            }

            OnLoadFailed.Invoke();
            return null;
        }
        
        public void ProjectSavedToIndexedDB()
        {
            projectDataStore.ProjectSavedToIndexedDB();
        }
    }
}