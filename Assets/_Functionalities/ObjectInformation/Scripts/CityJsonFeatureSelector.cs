using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using UnityEngine;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class CityJsonFeatureSelector : MonoBehaviour, IObjectSelector
    {      
        public bool HasFeatureMapping => featureMappings.Count > 0;
        public List<CityJSONMapping> FeatureMappings => featureMappings.SelectMany(entry => entry.Value).ToList();
        
        private GameObject testHitPosition;
        private GameObject testGroundPosition;
        private Dictionary<CityJSONLayerGameObject, List<CityJSONMapping>> featureMappings = new();
        private Vector3 blockingObjectMappingHitPoint;
        private ObjectMapping blockingObjectMapping;
        private PointerToWorldPosition pointerToWorldPosition;

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        public void Select(CityJSONMapping mapping)
        {
            mapping.Select(null);
        }

        public void Deselect()
        {
            if (featureMappings.Count > 0)
            {
                foreach (var mapping in featureMappings.SelectMany(pair => pair.Value))
                {
                    mapping.Deselect();
                }
            }
        }

        public void FindFeatureByPosition(Vector3 position)
        {
            featureMappings.Clear();
            Coordinate targetCoordinate = new Coordinate(position);
            List<IMapping> foundMappings = SelectionService.MappingTree.QueryMappingsContainingNode<CityJSONMapping>(targetCoordinate);
            foreach (CityJSONMapping map in foundMappings)
            {
                if (map.IsPositionHit(position))
                {
                    featureMappings.TryAdd(map.VisualisationParent, new List<CityJSONMapping>());
                    featureMappings[map.VisualisationParent].Add(map);
                }
            }
        }

        public void FindFeatureAtPointerPosition()
        {            
            Vector3 groundPosition = pointerToWorldPosition.GetWorldPointUsingOpticalRaycaster();
            FindFeatureByPosition(groundPosition);            
        }

        public void TryAddMappings(Dictionary<IMapping, int> mappings, bool filterDuplicateFeatures)
        {
            if (HasFeatureMapping)
            {
                List<CityObject> filterDuplicates = new List<CityObject>();
                foreach (CityJSONMapping feature in FeatureMappings)
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
                        mappings.TryAdd(feature, feature.VisualisationParent.LayerData.RootId);
                    }
                }
            }
        }
    }    
}