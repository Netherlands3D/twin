using System;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;
using UnityEngine.Assertions;

namespace Netherlands3D.Twin.Projects
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Project", fileName = "Project", order = 0)]
    [Serializable]
    public class ProjectData : ScriptableObject
    {
        [JsonIgnore] private static ProjectData current;
        [JsonIgnore] public static ProjectData Current => current;
        [JsonIgnore, NonSerialized] private bool isLoading = false; //is the project data currently loading? if true don't add the Layers to the root's childList, because this list is stored in the json, if false, a layer was created in app, and it should be initialized 

        [DllImport("__Internal")]
        private static extern void DownloadFromIndexedDB(string filename, string callbackObjectName, string callbackMethodName);

        [DllImport("__Internal")]
        private static extern void SyncFilesToIndexedDB(string callbackObjectName, string callbackMethodName);

        public const string DefaultFileName = "NL3D_Project_";
        public const string ProjectFileExtension = ".nl3d";
        public const string ProjectJsonFileNameInZip = "project.json";

        [Header("Serialized data")] public int Version = 1;
        public string SavedTimestamp = "";
        public string UUID = "";
        public double[] CameraPosition = new double[3]; //X, Y, Z,- Assume RD for now
        public double[] CameraRotation = new double[3];
        [SerializeField, JsonProperty] private RootLayer rootLayer;
        [JsonIgnore] public PrefabLibrary PrefabLibrary; //for some reason this cannot be a field backed property because it will still try to serialize it even with the correct tags applied

        [JsonIgnore]
        public RootLayer RootLayer
        {
            get => rootLayer;
            private set
            {
                rootLayer = value;
                rootLayer.ReconstructParentsRecursive();
            }
        }

        [NonSerialized] private ProjectDataHandler projectDataHandler;
        [NonSerialized] private ZipOutputStream zipOutputStream;
        [NonSerialized] private string lastSavePath = "";

        [NonSerialized] public UnityEvent<ProjectData> OnDataChanged = new();
        [NonSerialized] public UnityEvent<LayerData> LayerAdded = new();
        [NonSerialized] public UnityEvent<LayerData> LayerDeleted = new();

        [NonSerialized] private JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };

        public void RefreshUUID()
        {
            UUID = Guid.NewGuid().ToString();
        }

        public void CopyUndoFrom(ProjectData project)
        {
            //TODO: Implement undo copy with just the data we want to move between undo/redo states
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                OnDataChanged.Invoke(this);
        }

#endif

        public void LoadFromFile(string fileName)
        {
            isLoading = true;
            RootLayer.DestroyLayer();

            // Open the zip file
            using (FileStream fs = File.OpenRead(Path.Combine(Application.persistentDataPath, fileName)))
            {
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
                        isLoading = false;

                        //todo add failed loading event
                    }
                }
            }
        }

        public static void LoadProjectData(ProjectData data)
        {
            var jsonProject = JsonConvert.SerializeObject(data, Current.serializerSettings);
            Current.LoadJSON(jsonProject);
        }

        private void LoadJSON(string json)
        {
            JsonConvert.PopulateObject(json, Current, serializerSettings);
            RootLayer.ReconstructParentsRecursive();
            Debug.Log("loaded project with uuid: " + UUID);
            OnDataChanged.Invoke(this);
            isLoading = false;
        }

        public void SaveAsFile(ProjectDataHandler projectDataHandler)
        {
            RefreshUUID();

            this.projectDataHandler = projectDataHandler;

            // Set the timestamp when the data was saved
            SavedTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var readableTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HHmm");

            // Start the zip output stream
            lastSavePath = Application.persistentDataPath + $"/{DefaultFileName}{readableTimeStamp}{ProjectFileExtension}";
            zipOutputStream = new ZipOutputStream(File.Create(lastSavePath));
            zipOutputStream.SetLevel(9); // 0-9 where 9 means best compression

            var jsonProject = JsonConvert.SerializeObject(this, serializerSettings);
            var entry = new ZipEntry(ProjectJsonFileNameInZip);
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

            // Make sure indexedDB is synced
#if !UNITY_EDITOR && UNITY_WEBGL
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

        public void AddStandardLayer(LayerData layer)
        {
            if (!isLoading)
            {
                RootLayer.AddChild(layer);
            }

            LayerAdded.Invoke(layer);
        }

        public static void AddReferenceLayer(LayerGameObject referencedLayer)
        {
            var referenceName = referencedLayer.name.Replace("(Clone)", "").Trim();

            var proxyLayer = new ReferencedLayerData(referenceName, referencedLayer);
            referencedLayer.LayerData = proxyLayer;

            //add properties to the new layerData
            var layersWithPropertyData = referencedLayer.GetComponents<ILayerWithPropertyData>();
            foreach (var layerWithPropertyData in layersWithPropertyData)
            {
                referencedLayer.LayerData.AddProperty(layerWithPropertyData.PropertyData);
                proxyLayer.PropertiesChanged.AddListener(layerWithPropertyData.LoadProperties);
            }
        }

        public void RemoveLayer(LayerData layer)
        {
            LayerDeleted.Invoke(layer);
        }

        public static void SetCurrentProject(ProjectData initialProjectTemplate)
        {
            Assert.IsNull(current);
            current = initialProjectTemplate;
            current.RootLayer = new RootLayer("RootLayer");
        }
    }
}