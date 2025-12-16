using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Layers.LayerTypes;
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

        [Header("Serialized data")] public int Version = 1;
        public string SavedTimestamp = "";
        public string UUID = "";
        private double[] cameraPosition = new double[3]; //X, Y, Z,- Assume RD for now

        public double[] CameraPosition
        {
            get => cameraPosition;
            set
            {
                cameraPosition = value;
                OnCameraPositionChanged.Invoke(new Coordinate(CoordinateSystem.RDNAP, cameraPosition));
            }
        }

        public double[] CameraRotation = new double[3];
        private DateTime currentDateTime = new DateTime(2024, 08, 19, 13, 0, 0); // Default time
        public DateTime CurrentDateTime
        {
            get => currentDateTime;
            set
            {
                currentDateTime = value;
                OnCurrentDateTimeChanged.Invoke(value);
            }
        }
        public bool UseCurrentTime = false;
        [SerializeField, JsonProperty] public List<FunctionalityData> functionalities = new();
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

        [NonSerialized] public UnityEvent<DateTime> OnCurrentDateTimeChanged = new();
        [NonSerialized] public UnityEvent<ProjectData> OnDataChanged = new();
        [NonSerialized] public UnityEvent<Coordinate> OnCameraPositionChanged = new();

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

        public static void SetCurrentProject(ProjectData initialProjectTemplate)
        {
            Assert.IsNull(current);
            current = initialProjectTemplate;
            current.RootLayer = new RootLayer("RootLayer");
            current.functionalities = new();
        }

        public void LoadVisualizations()
        {    
            foreach (var layer in rootLayer.GetFlatHierarchy())
            {
                if (layer is RootLayer) continue;
                App.Layers.VisualizeData(layer);
            }
        }
        
        /// <summary>
        /// Recursively collect all assets from each of the property data elements of every layer for loading and
        /// saving purposes. 
        /// </summary>
        /// <returns>A list of assets</returns>
        public IEnumerable<LayerAsset> GetAssets()
        {
            return rootLayer.GetAssets();
        }

        public void AddFunctionality(FunctionalityData data)
        {
            if (functionalities.Contains(data))
            {
                Debug.LogWarning($"Not adding {data.Id} to ProjectData. A functionality with this ID already exists.");
                return;
            }

            functionalities.Add(data);
        }

        public void RemoveFunctionality(FunctionalityData data)
        {
            functionalities.Remove(data);
        }

        public void ClearFunctionalityData()
        {
            functionalities.Clear();
        }
    }
}