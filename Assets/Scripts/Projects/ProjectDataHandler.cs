using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Configuration;
using JetBrains.Annotations;
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
        [SerializeField] private string defaultProjectFileName = "ProjectTemplate.nl3d";
        [SerializeField] private Configuration.Configuration configuration;
        [SerializeField] private ProjectDataStore projectDataStore;

        public List<ProjectData> undoStack = new();
        public List<ProjectData> redoStack = new();

        [SerializeField] private InputActionAsset applicationActionMap;
        [SerializeField] private FileOpen fileOpener;

        public int undoStackSize = 10;

        [Header("Progress events")] [Tooltip("called when the save action is started")]
        public UnityEvent OnSaveStarted;

        [Tooltip("called when the save action completed successfully")]
        public UnityEvent OnSaveCompleted;

        [Tooltip("called when the load action is started")]
        public UnityEvent OnLoadStarted;

        [Tooltip("called when the load action completed successfully")]
        public UnityEvent OnLoadCompleted;

        [Tooltip("called when the load action failed")]
        public UnityEvent OnLoadFailed;

        private static ProjectDataHandler instance;
        public static ProjectDataHandler Instance 
        { 
            get 
            {
                if(instance == null)
                    instance = FindObjectOfType<ProjectDataHandler>();

                return instance;
            }
            set { instance = value; }
        }
        
        private InputAction openProjectAction;
        private InputAction saveProjectAction;
        private InputAction undoAction;
        private InputAction redoAction;
        
        private void Awake()
        {
            openProjectAction = applicationActionMap.FindAction("Projects/Open");
            saveProjectAction = applicationActionMap.FindAction("Projects/Save");
            undoAction = applicationActionMap.FindAction("Projects/Undo");
            redoAction = applicationActionMap.FindAction("Projects/Redo");
            if (ProjectData.Current == null)
            {
                Debug.LogError("Current ProjectData object reference is not set in ProjectData", this.gameObject);
                return;
            }

            fileImporter = GetComponent<DataTypeChain>();
            ProjectData.Current.OnDataChanged.AddListener(OnProjectDataChanged);

#if !UNITY_EDITOR && UNITY_WEBGL
            //Prevent default browser shortcuts for saving and undo/redo
            PreventDefaultShortcuts();
#endif
        }

        private void OnEnable()
        {
            openProjectAction.Enable();
            openProjectAction.performed += OnOpenProjectAction;

            saveProjectAction.Enable();
            saveProjectAction.performed += OnSaveProjectAction;

            undoAction.Enable();
            undoAction.performed += OnUndoAction;

            redoAction.Enable();
            redoAction.performed += OnRedoAction;
        }

        private void OnDisable()
        {
            openProjectAction.performed -= OnOpenProjectAction;
            openProjectAction.Disable();

            saveProjectAction.performed -= OnSaveProjectAction;
            saveProjectAction.Disable();

            undoAction.performed -= OnUndoAction;
            undoAction.Disable();

            redoAction.performed -= OnRedoAction;
            redoAction.Disable();
        }

        private void Start()
        {
            LoadDefaultProject(); //todo: when undo is implemented, assign the listener after loading this, so the initial load cannot be undone
        }

        private void OnProjectDataChanged(ProjectData project)
        {
            // Add new undo state
            if (undoStack.Count == undoStackSize)
                undoStack.RemoveAt(0);

            // Copy the current projectData to a new project instance for our undo history
            var newProject = ScriptableObject.CreateInstance<ProjectData>();
            undoStack.Add(newProject);

            // Clear the redo stack
            redoStack.Clear();
        }

        private void OnOpenProjectAction(InputAction.CallbackContext obj)
        {
            fileOpener.OpenFile();
        }

        private void OnSaveProjectAction(InputAction.CallbackContext obj)
        {
            SaveProject();
        }

        private void OnUndoAction(InputAction.CallbackContext obj)
        {
            Undo();
        }

        private void OnRedoAction(InputAction.CallbackContext obj)
        {
            Redo();
        }

        public void SaveProject()
        {
            OnSaveStarted.Invoke();
            projectDataStore.SaveAsFile(this);
            OnSaveCompleted.Invoke();
        }

        private void LoadDefaultProject()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var url = Path.Combine(Application.streamingAssetsPath, defaultProjectFileName);
            Debug.Log("loading default project file: " + url);
            fileImporter.LoadFromUrl(url, ApplyOverridesFromConfiguration);
#else
            var filePath = Path.Combine(Application.streamingAssetsPath, defaultProjectFileName);
            Debug.Log("loading default project file: " + filePath);
            projectDataStore.LoadFromFile(filePath);
            ApplyOverridesFromConfiguration();
#endif
        }

        /// <summary>
        /// In the earlier version of the application, the application was configuration driven instead of using
        /// project files. When we introduced the project data solution we hadn't removed the configuration yet for
        /// backwards compatibility reasons, and this method allows for a Project's settings to be overridden by
        /// the Configuration when that was loaded from file or URL.
        ///
        /// Usually, this method is called after loading the default project so that the defaults defined in that
        /// project are overridden by the intended user-specific changes. Subsequent loads of projects should _not_
        /// be overridden by the configuration as these are more specific than the loaded configuration.
        ///
        /// Chain of specificity:
        /// 
        /// Default Project -> Configuration -> User-loaded Project
        ///
        /// This method should not be called outside of this system as we intend to replace the way configuration is
        /// loaded by something that directly interacts with projects instead of this in-between Scriptable Object. 
        /// </summary>
        public void ApplyOverridesFromConfiguration()
        {
            Debug.Log("Configuration was provided, update camera position to match");
            // if no alternate configuration was loaded, we do not apply the settings as we intend for the project
            // to provide the defaults; instead of picking these from the configuration.
            if (configuration.Source == ConfigurationSource.None) return;
            
            // Ensure the configuration's origin is in RDNAP because CameraPosition is in RDNAP
            ProjectData.Current.CameraPosition = configuration.Origin.Convert(CoordinateSystem.RDNAP).Points;
            
            // TODO: Override functionalities and their settings once we support that in the ProjectData
        }
        
        public void LoadFromFile(string filePaths)
        {
            OnLoadStarted.Invoke();

            var files = filePaths.Split(',');
            print("processing " + files.Length + " files");
            foreach (var filePath in files)
            {
                print("attempting to load file: " + filePath);
                if (filePath.ToLower().EndsWith(".nl3d"))
                {
                    Debug.Log("loading nl3d file: " + filePath);
                    projectDataStore.LoadFromFile(filePath);
                    OnLoadCompleted.Invoke();
                    return;
                }
            }

            OnLoadFailed.Invoke();
        }

        public void Redo()
        {
            // Overwrite current projectData with the one from the redostack copy
            if (redoStack.Count > 0)
            {
                var lastState = redoStack[redoStack.Count - 1];
                ProjectData.Current.CopyUndoFrom(lastState);
                redoStack.RemoveAt(redoStack.Count - 1);
            }
        }

        public void Undo()
        {
            // Overwrite current projectData with the one from the undostack copy
            if (undoStack.Count > 0)
            {
                var lastState = undoStack[undoStack.Count - 1];
                ProjectData.Current.CopyUndoFrom(lastState);
                undoStack.RemoveAt(undoStack.Count - 1);
            }
        }

        /// <summary>
        /// Receiver for the ProjectData to notify the ProjectDataHandler that the project has been saved to IndexedDB
        /// </summary>
        public void ProjectSavedToIndexedDB()
        {
            projectDataStore.ProjectSavedToIndexedDB();
        }

        public void DownloadedProject()
        {
            Debug.Log("Downloading project file succeeded");
        }
    }
}