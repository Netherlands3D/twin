using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class SubObjectSelector : MonoBehaviour, IObjectSelector
    {
        public bool HasObjectMapping => foundObject != null;
        public MeshMapping Object => foundObject; 

        private MeshMapping foundObject;

        private PointerToWorldPosition pointerToWorldPosition;
        private Dictionary<string, bool> blockedBagIds = new Dictionary<string, bool>();

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }
        
        public void BlockBagId(string bagId, bool block)
        {
            blockedBagIds[bagId] = block;
        }
        
        public void Select(string bagId)
        {
            Deselect();
            foundObject.Select(bagId);
        }
        
        public void Deselect()
        {
            foundObject?.Deselect();
        }
        
        public LayerData GetLayerDataForSubObject(ObjectMapping subObject)
        {
            Transform parent = subObject.gameObject.transform.parent;
            LayerGameObject layerGameObject = parent.GetComponent<LayerGameObject>();
            if (layerGameObject)
            {
                return layerGameObject.LayerData;   
            }
            return null;
        }
        
        public bool IsMappingVisible(MeshMapping mapping, string bagId)
        {
            LayerFeature feature = GetLayerFeatureFromBagID(bagId, mapping, out LayerGameObject layer);
            if (feature != null)
            {
                HiddenObjectsPropertyData hiddenPropertyData = layer.LayerData.GetProperty<HiddenObjectsPropertyData>();
                bool? v = hiddenPropertyData.GetVisibilityForSubObject(feature);
                if (v != true) return false;
            }
            if (bagId == null || blockedBagIds.ContainsKey(bagId))
                return false;
            return true;
        }
        
        public ObjectMappingItem GetMappingItemForBagID(string bagID, IMapping selectedMapping, out LayerGameObject layer)
        {
            layer = null;
            if (selectedMapping is not MeshMapping mapping) return null;

            layer = GetLayerGameObjectFromMapping(selectedMapping);
            mapping.ObjectMapping.items.TryGetValue(bagID, out var item);
            return item;
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
                    map = ServiceLocator.GetService<ObjectSelectorService>().GetReplacedMapping(meshMapping);
                }
                if (map == null) return null;
                
                Transform parent = map.ObjectMapping.gameObject.transform.parent;
                LayerGameObject layerGameObject = parent.GetComponent<LayerGameObject>();
                return layerGameObject;
            }
            return null;
        }
        
        
        
        public LayerFeature GetLayerFeatureFromBagID(string bagID, IMapping selectedMapping, out LayerGameObject layer)
        {
            ObjectMappingItem item = GetMappingItemForBagID(bagID, selectedMapping, out layer);
            if (layer == null)
                return null;

            return layer.GetLayerFeatureByGeometry(item);
        }

        public string FindSubObjectAtPointerPosition()
        {
            foundObject = null;
            string bagId = null;
            Vector3 groundPosition = pointerToWorldPosition.WorldPoint.ToUnity();
            Coordinate coord = new Coordinate(groundPosition);
            List<IMapping> mappings = ObjectSelectorService.MappingTree.QueryMappingsContainingNode<MeshMapping>(coord);
            if (mappings.Count == 0)
                return bagId;
            
            foreach (MeshMapping mapping in mappings)
            { 
                LayerGameObject subObjectParent = mapping.ObjectMapping.transform.GetComponentInParent<LayerGameObject>();
                if (subObjectParent == null || !subObjectParent.LayerData.ActiveInHierarchy)
                    continue;
                
                MeshMappingItem item = mapping.FindItemForPosition(groundPosition);
                if (item != null)
                {
                    if(IsMappingVisible(mapping, item.ObjectMappingItem.objectID) == false)
                        continue;
                    
                    foundObject = mapping;
                    bagId = item.ObjectMappingItem.objectID;
                    break;
                }
            }
            return bagId;
        }
    }
}