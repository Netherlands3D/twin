using GeoJSON.Net.Feature;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class ObjectSelector : MonoBehaviour
    {
        public static ObjectSelector ObjectSelection
        {
            get
            {
                if(objectSelector == null)
                {
                    GameObject objectSelectorGameObject = new GameObject("objectselector");
                    objectSelector = objectSelectorGameObject.AddComponent<ObjectSelector>();
                }
                return objectSelector;
            }
        }
        private static ObjectSelector objectSelector;

        private FeatureSelector featureSelector;
        private SubObjectSelector subObjectSelector;
        private List<IMapping> orderedMappings = new List<IMapping>();
        private Vector3 lastWorldClickedPosition;
        private PointerToWorldPosition pointerToWorldPosition;
        private float minClickDistance = 10;
        private float minClickTime = 0.5f;
        private float lastTimeClicked = 0;
        private int currentSelectedMappingIndex = -1;
        private bool filterDuplicateFeatures = true;

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

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
            subObjectSelector = gameObject.AddComponent<SubObjectSelector>();
            featureSelector = gameObject.AddComponent<FeatureSelector>();
            featureSelector.SetMappingTree(MappingTree);

            Interaction.ObjectMappingCheckIn += OnAddObjectMapping;
            Interaction.ObjectMappingCheckOut += OnRemoveObjectMapping;

        }

        private void Start()
        {
            //baginspector could be enabled later on, so it would be missing the already instantiated mappings
            ObjectMapping[] alreadyActiveMappings = FindObjectsByType<ObjectMapping>(FindObjectsSortMode.None);
            foreach (ObjectMapping mapping in alreadyActiveMappings)
            {
                OnAddObjectMapping(mapping);
            }
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
            //the following method calls need to run in order!
            string bagId = subObjectSelector.FindSubObjectAtPointerPosition();
            return bagId;
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
        /// Find objectmapping by raycast and get the BAG ID
        /// </summary>
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
