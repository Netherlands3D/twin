using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Cameras.Input;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class ObjectSelector : MonoBehaviour
    {
        public UnityEvent<MeshMapping, string> SelectSubObjectWithBagId;
        public UnityEvent<FeatureMapping> SelectFeature;

        private FeatureSelector featureSelector;
        private SubObjectSelector subObjectSelector;
        private List<IMapping> orderedMappings = new List<IMapping>();
        private Vector3 lastWorldClickedPosition;
        private PointerToWorldPosition pointerToWorldPosition;
        private float minClickDistance = 10;
        private float minClickTime = 0.5f;
        private float lastTimeClicked = 0;
        private bool draggedBeforeRelease = false;
        private bool waitingForRelease = false;
        private int currentSelectedMappingIndex = -1;
        private bool filterDuplicateFeatures = true;
        private CameraInputSystemProvider cameraInputSystemProvider;

        [SerializeField] private Tool[] activeForTools;

        public static MappingTree MappingTree
        {
            get
            {
                if (mappingTreeInstance == null)
                {
                    BoundingBox bbox = StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds;
                    MappingTree tree = new MappingTree(bbox, 4, 12);
                    mappingTreeInstance = tree;
                }
                return mappingTreeInstance;
            }
        }
        public bool debugMappingTree = false;
        private static MappingTree mappingTreeInstance;
        private LayerData lastSelectedLayerData = null;

        private void Awake()
        {
            cameraInputSystemProvider = Camera.main.GetComponent<CameraInputSystemProvider>();
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
            subObjectSelector = gameObject.AddComponent<SubObjectSelector>();
            featureSelector = gameObject.AddComponent<FeatureSelector>();
            featureSelector.SetMappingTree(MappingTree);

            foreach(Tool tool  in activeForTools) 
                tool.onClose.AddListener(() => Deselect());

            Interaction.ObjectMappingCheckIn += OnAddObjectMapping;
            Interaction.ObjectMappingCheckOut += OnRemoveObjectMapping;
        }

        private void Start()
        {
            //objectselector could be enabled later on, so it would be missing the already instantiated mappings
            ObjectMapping[] alreadyActiveMappings = FindObjectsByType<ObjectMapping>(FindObjectsSortMode.None);
            foreach (ObjectMapping mapping in alreadyActiveMappings)
            {
                OnAddObjectMapping(mapping);
            }
        }

        private bool IsAnyToolActive()
        {
            foreach (Tool tool in activeForTools)
                if (tool.Open)
                    return true;
            return false;
        }

        private void Update()
        {            
            if (IsAnyToolActive() && IsClicked())
            {
                Deselect();
                //the following method calls need to run in order!
                string bagId = FindBagId(); //for now this seems to be better than an out param on findobjectmapping
                IMapping mapping = FindObjectMapping();
                if(mapping == null && lastSelectedLayerData != null)
                {
                    //when nothing is selected but there was something selected, deselect the current active layer
                    lastSelectedLayerData.DeselectLayer();
                    lastSelectedLayerData = null;
                }
                if (mapping is MeshMapping map)
                {
                    SelectBagId(bagId);
                    LayerData layerData = subObjectSelector.GetLayerDataForSubObject(map.ObjectMapping);
                    layerData.SelectLayer(true);
                    lastSelectedLayerData = layerData;
                    SelectSubObjectWithBagId?.Invoke(map, bagId);
                }
                else if(mapping is FeatureMapping feature) 
                {
                    LayerData layerData = feature.VisualisationParent.LayerData;
                    layerData.SelectLayer(true);    
                    lastSelectedLayerData = layerData;
                    SelectFeatureMapping(feature);
                    SelectFeature?.Invoke(feature);
                }
            }
        }

        private bool IsClicked()
        {
            var click = Pointer.current.press.wasPressedThisFrame;

            if (click)
            {
                waitingForRelease = true;
                draggedBeforeRelease = false;
                return false;
            }

            if (waitingForRelease && !draggedBeforeRelease)
            {
                //Check if next release should be ignored ( if we dragged too much )
                draggedBeforeRelease = Pointer.current.delta.ReadValue().sqrMagnitude > 0.5f;
            }

            if (Pointer.current.press.wasReleasedThisFrame == false) return false;

            waitingForRelease = false;

            if (draggedBeforeRelease) return false;

            return cameraInputSystemProvider.OverLockingObject == false;
        }

        private void OnAddObjectMapping(ObjectMapping mapping)
        {
            MeshMapping objectMapping = new MeshMapping();
            objectMapping.SetMeshObject(mapping);
            objectMapping.UpdateBoundingBox();
            MappingTree.RootInsert(objectMapping);
        }

        private void OnRemoveObjectMapping(ObjectMapping mapping)
        {
            //the getcomponent is unfortunate, if its performanc heavy maybe use cellcaching
            BoundingBox queryBoundingBox = new BoundingBox(mapping.GetComponent<MeshRenderer>().bounds);
            queryBoundingBox.Convert(CoordinateSystem.WGS84_LatLon);
            List<IMapping> mappings = MappingTree.Query<MeshMapping>(queryBoundingBox);
            foreach (MeshMapping map in mappings)
            {
                if (map.ObjectMapping == mapping)
                {
                    //destroy featuremapping object, there should be no references anywhere else to this object!
                    MappingTree.Remove(map);
                }
            }
        }

        public string FindBagId()
        {            
            return subObjectSelector.FindSubObjectAtPointerPosition();            
        }

        public void SelectBagId(string bagId)
        {
            subObjectSelector.Select(bagId);
        }

        public void SelectFeatureMapping(FeatureMapping feature)
        {
            featureSelector.Select(feature);
        }

        /// <summary>
        /// Finds a Mapping in the world by the current optical raycaster worldposition
        /// </summary>
        /// <returns></returns>
        public IMapping FindObjectMapping()
        {
            bool clickedSamePosition = Vector3.Distance(lastWorldClickedPosition, pointerToWorldPosition.WorldPoint) < minClickDistance;
            lastWorldClickedPosition = pointerToWorldPosition.WorldPoint;

            bool refreshSelection = Time.time - lastTimeClicked > minClickTime;
            lastTimeClicked = Time.time;

            if (!clickedSamePosition || refreshSelection)
            {
                if (subObjectSelector.Object != null)
                    featureSelector.SetBlockingObjectMapping(subObjectSelector.Object.ObjectMapping, lastWorldClickedPosition);

                //no features are imported yet if mappingTreeInstance is null
                if (mappingTreeInstance != null)
                    featureSelector.FindFeatureAtPointerPosition();

                orderedMappings.Clear();
                Dictionary<IMapping, int> mappings = new Dictionary<IMapping, int>();
                //lets order all mappings by layerorder (rootindex) from layerdata
                if (featureSelector.HasFeatureMapping)
                {
                    List<Feature> filterDuplicates = new List<Feature>();
                    foreach (FeatureMapping feature in featureSelector.FeatureMappings)
                    {
                        if (feature.VisualisationParent.LayerData.ActiveInHierarchy)
                        {
                            if (filterDuplicateFeatures)
                            {
                                if (!filterDuplicates.Contains(feature.Feature))
                                    filterDuplicates.Add(feature.Feature);
                                else
                                    continue;
                            }

                            mappings.TryAdd(feature, feature.VisualisationParent.LayerData.RootIndex);
                        }
                    }
                }
                if (subObjectSelector.HasObjectMapping)
                {
                    LayerGameObject subObjectParent = subObjectSelector.Object.ObjectMapping.transform.GetComponentInParent<LayerGameObject>();
                    if (subObjectParent != null)
                    {
                        if (subObjectParent.LayerData.ActiveInHierarchy)
                            mappings.TryAdd(subObjectSelector.Object, subObjectParent.LayerData.RootIndex);
                    }
                }

                orderedMappings = mappings.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToList();

                currentSelectedMappingIndex = 0;
            }
            else
            {
                //clicking at same position so lets toggle through the list
                currentSelectedMappingIndex++;
                if (currentSelectedMappingIndex >= orderedMappings.Count)
                    currentSelectedMappingIndex = 0;
            }

            if (orderedMappings.Count == 0) return null;

            //Debug.Log(orderedMappings[currentSelectedMappingIndex]);

            IMapping selection = orderedMappings[currentSelectedMappingIndex];
            return selection;
        }

        public void Deselect()
        {
            subObjectSelector.Deselect();
            featureSelector.Deselect();
        }

#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (debugMappingTree)
                MappingTree.DebugTree();
        }
#endif

    }
}
