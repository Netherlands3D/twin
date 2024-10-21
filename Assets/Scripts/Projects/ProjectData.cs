using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using ICSharpCode.SharpZipLib.Zip;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Functionalities;
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
        [JsonIgnore, NonSerialized] public bool isLoading = false; //is the project data currently loading? if true don't add the Layers to the root's childList, because this list is stored in the json, if false, a layer was created in app, and it should be initialized 

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
        public DateTime CurrentDateTime = new(2024, 08, 19, 13, 0, 0); //default time
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

        [NonSerialized] public UnityEvent<ProjectData> OnDataChanged = new();
        [NonSerialized] public UnityEvent<Coordinate> OnCameraPositionChanged = new();
        [NonSerialized] public UnityEvent<LayerData> LayerAdded = new();
        [NonSerialized] public UnityEvent<LayerData> LayerDeleted = new();

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

        public void AddStandardLayer(LayerData layer)
        {
            if (!isLoading)
            {
                RootLayer.AddChild(layer, 0);
            }
            LayerAdded.Invoke(layer);           
        }

        public static void AddReferenceLayer(LayerGameObject referencedLayer)
        {
            var referenceName = referencedLayer.name.Replace("(Clone)", "").Trim();

            var proxyLayer = new ReferencedLayerData(referenceName, referencedLayer);
            referencedLayer.LayerData = proxyLayer;

            // Add properties to the new layerData
            var layersWithPropertyData = referencedLayer.GetComponents<ILayerWithPropertyData>();
            foreach (var layerWithPropertyData in layersWithPropertyData)
            {
                referencedLayer.LayerData.AddProperty(layerWithPropertyData.PropertyData);
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
            current.functionalities = new();
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
            if (!functionalities.Contains(data))
                functionalities.Add(data);
            else
                Debug.LogWarning("Not adding " + data.Id + " to ProjectData. A functionality with this ID already exists.");
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