using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    /// <summary>
    /// This class manages the state of the project (undo/redo) and handles saving and loading of the project as a file
    /// </summary>
    public class ProjectDataHandler : MonoBehaviour
    {
        [DllImport("__Internal")] private static extern void PreventDefaultShortcuts();
        private DataTypeChain fileImporter; // don't remove, this is used in LoadDefaultProject()
        [SerializeField] private string defaultProjectFileName = "ProjectTemplate.nl3d";

        public List<ProjectData> undoStack = new();
        public List<ProjectData> redoStack = new();

        public int undoStackSize = 10;

        private static ProjectDataHandler instance;
        public static ProjectDataHandler Instance 
        { 
            get{
                if(instance == null)
                    instance = FindObjectOfType<ProjectDataHandler>();

                return instance;
            }
            set
            {
                instance = value;
            }
        }

        private void Awake() {
            if(ProjectData.Current == null) {
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
            // newProject.CopyFrom(projectData);
            undoStack.Add(newProject);

            // Clear the redo stack
            redoStack.Clear();
        }

        private void Update()
        {
            var ctrlModifier = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

            if (Input.GetKeyDown(KeyCode.S) && ctrlModifier)
                SaveProject();

            if (Input.GetKeyDown(KeyCode.Z) && ctrlModifier)
                Undo();

            if (Input.GetKeyDown(KeyCode.Y) && ctrlModifier)
                Redo();
        }

        public void SaveProject()
        {
            ProjectData.Current.SaveAsFile(this);
        }

        private void LoadDefaultProject()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var url = Path.Combine(Application.streamingAssetsPath, defaultProjectFileName);
            Debug.Log("loading default project file: " + url);
            fileImporter.DetermineAdapter(url);
#else
            var filePath = Path.Combine(Application.streamingAssetsPath, defaultProjectFileName);
            Debug.Log("loading default project file: " + filePath);
            ProjectData.Current.LoadFromFile(filePath);
#endif
        }
        
        public void LoadFromFile(string filePaths)
        {
            var files = filePaths.Split(',');
            print("processing " + files.Length + " files");
            foreach (var filePath in files)
            {
                print("attempting to load file: " + filePath);
                if(filePath.ToLower().EndsWith(".nl3d"))
                {
                    Debug.Log("loading nl3d file: " + filePath);
                    ProjectData.Current.LoadFromFile(filePath);
                    return;
                }
            }  
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
            ProjectData.Current.ProjectSavedToIndexedDB();
        }
        public void DownloadedProject()
        {
            Debug.Log("Downloading project file succeeded");
        }
    }
}