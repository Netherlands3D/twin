using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Cameras.Input;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Projects;
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
    public class ObjectSelectorService : MonoBehaviour
    {
        public SubObjectSelector SubObjectSelector => subObjectSelector;

        public UnityEvent<MeshMapping, string> SelectSubObjectWithBagId;
        public UnityEvent<FeatureMapping> SelectFeature;
        public UnityEvent OnDeselect = new();
        public UnityEvent OnSelectDifferentLayer = new();

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
        private LayerData lastSelectedMappingLayerData = null;
        private LayerData lastSelectedLayerData = null;

        private void Awake()
        {
            cameraInputSystemProvider = Camera.main.GetComponent<CameraInputSystemProvider>();
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
            subObjectSelector = gameObject.AddComponent<SubObjectSelector>();
            featureSelector = gameObject.AddComponent<FeatureSelector>();
            featureSelector.SetMappingTree(MappingTree);
            
            Interaction.ObjectMappingCheckIn += OnAddObjectMapping;
            Interaction.ObjectMappingCheckOut += OnRemoveObjectMapping;
        }

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(OnProjectChanged);

            foreach (Tool tool  in activeForTools) 
                tool.onClose.AddListener(Deselect);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(OnProjectChanged);

            foreach (Tool tool  in activeForTools) 
                tool.onClose.RemoveListener(Deselect);
        }

        private void OnProjectChanged(ProjectData data)
        {
            ProjectData.Current.RootLayer.AddedSelectedLayer.AddListener(OnAddSelectedLayer);
            ProjectData.Current.RootLayer.RemovedSelectedLayer.AddListener(OnRemoveSelectedLayer);
        }

        private void OnAddSelectedLayer(LayerData data)
        {
            //we need to check this before Isclicked because it checks if its over the ui
            if (ProjectData.Current.RootLayer.SelectedLayers.Count > 0 && ProjectData.Current.RootLayer.SelectedLayers.Last() != data)
            {
                Deselect();
                OnSelectDifferentLayer.Invoke();
            }
            lastSelectedLayerData = data;
        }

        private void OnRemoveSelectedLayer(LayerData data)
        {
            //we need to check this before Isclicked because it checks if its over the ui
            if(ProjectData.Current.RootLayer.SelectedLayers.Count == 0)
            {
                if (lastSelectedLayerData != null || lastSelectedMappingLayerData != null)
                {
                    Deselect();
                    lastSelectedLayerData = null;
                    lastSelectedMappingLayerData = null;
                }
            }
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

        public LayerGameObject GetLayerGameObjectFromMapping(IMapping mapping)
        {
            if (mapping is FeatureMapping featureMapping)
            {
                return featureMapping.VisualisationParent;
            }

            if (mapping is MeshMapping meshMapping)
            {
                MeshMapping map = meshMapping;
                if (meshMapping.ObjectMapping == null)                    
                {
                    //when tile is replacing lod the objectmapping can be missing
                    map = GetReplacedMapping(meshMapping);
                }
                if (map == null) return null;
                
                Transform parent = map.ObjectMapping.gameObject.transform.parent;
                LayerGameObject layerGameObject = parent.GetComponent<LayerGameObject>();
                return layerGameObject;
            }
            return null;
        }

        public T GetReplacedMapping<T>(T mapping) where T : IMapping
        {
            List<IMapping> mappings = MappingTree.QueryMappingsContainingNode<T>(mapping.BoundingBox.Center);
            if (mappings.Count == 0)
                return default;

            foreach (IMapping map in mappings)
            {
                if (map.MappingObject == null || map.Id != mapping.Id) continue;

                return (T)map;
            }
            return default;
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
            if (IsAnyToolActive())
            {    
                if (IsClicked())
                {
                    Deselect();
                    //the following method calls need to run in order!
                    string bagId = FindBagId(); //for now this seems to be better than an out param on findobjectmapping
                    IMapping mapping = FindObjectMapping();
                    bool mappingVisible = IsMappingVisible(mapping, bagId);
                    if ((mapping == null || !mappingVisible) && lastSelectedMappingLayerData != null)
                    {
                        //when nothing is selected but there was something selected, deselect the current active layer
                        lastSelectedMappingLayerData.DeselectLayer();
                        lastSelectedMappingLayerData = null;
                    }
                    if (mapping is MeshMapping map)
                    {
                        LayerData layerData = subObjectSelector.GetLayerDataForSubObject(map.ObjectMapping);
                        if (!mappingVisible)
                            return;

                        layerData.SelectLayer(true);
                        lastSelectedMappingLayerData = layerData;
                        SelectBagId(bagId);
                        SelectSubObjectWithBagId?.Invoke(map, bagId);
                    }
                    else if (mapping is FeatureMapping feature)
                    {
                        LayerData layerData = feature.VisualisationParent.LayerData;
                        layerData.SelectLayer(true);
                        lastSelectedMappingLayerData = layerData;
                        SelectFeatureMapping(feature);
                        SelectFeature?.Invoke(feature);
                    }
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

        private bool IsMappingVisible(IMapping mapping, string bagId)
        {
            if (mapping is MeshMapping map)
            {
                //TODO maybe to the best place here to have a dependency to the cartesianlayerstyler, needs a better implementation
                LayerFeature feature = GetLayerFeatureFromBagID(bagId, map, out LayerGameObject layer);
                if (feature != null)
                {
                    bool? v = (layer.Styler as CartesianTileLayerStyler).GetVisibilityForSubObject(feature);
                    if (v != true) return false;
                }
            }
            return true;
        }

        private void OnAddObjectMapping(ObjectMapping mapping)
        {
            MeshMapping objectMapping = new MeshMapping(mapping.name);
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

        public ObjectMappingItem GetMappingItemForBagID(string bagID, IMapping selectedMapping, out LayerGameObject layer)
        {
            layer = null;
            if (selectedMapping is not MeshMapping mapping) return null;

            layer = GetLayerGameObjectFromMapping(selectedMapping);
            return mapping.ObjectMapping.items.FirstOrDefault(item => bagID == item.objectID);
        }

        public LayerFeature GetLayerFeatureFromBagID(string bagID, IMapping selectedMapping, out LayerGameObject layer)
        {
            ObjectMappingItem item = GetMappingItemForBagID(bagID, selectedMapping, out layer);
            if (layer == null || !layer.LayerFeatures.ContainsKey(item))
                return null;
            
            return layer.LayerFeatures[item]; 
        }

        /// <summary>
        /// Finds a Mapping in the world by the current optical raycaster worldposition
        /// </summary>
        /// <returns></returns>
        public IMapping FindObjectMapping()
        {
            bool clickedSamePosition = Vector3.Distance(lastWorldClickedPosition, pointerToWorldPosition.WorldPoint.ToUnity()) < minClickDistance;
            lastWorldClickedPosition = pointerToWorldPosition.WorldPoint.ToUnity();

            bool refreshSelection = Time.time - lastTimeClicked > minClickTime;
            lastTimeClicked = Time.time;

            if (!clickedSamePosition || refreshSelection)
            {
                //when a geojson point is located on top of a feature in an objectmapping,
                //we need to find the blocked objectmapping and find the hitpoint to find the geojson feature position beneath it
                if (subObjectSelector.Object != null)
                    featureSelector.SetBlockingObjectMapping(subObjectSelector.Object.ObjectMapping, lastWorldClickedPosition);
                //the blocking objectmapping should be cleared when trying to select the next feature
                else
                    featureSelector.SetBlockingObjectMapping(null, Vector3.zero);

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

            IMapping selection = orderedMappings[currentSelectedMappingIndex];
            return selection;
        }

        public void Deselect()
        {
            subObjectSelector.Deselect();
            featureSelector.Deselect();
            OnDeselect.Invoke();
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
