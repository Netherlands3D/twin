using System.Collections.Generic;
using System.Linq;
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
        private Dictionary<MeshMapping, List<string>> selectedMappings = new();

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        public void UpdateReplacedSelectedMappings()
        {
            var keysToReplace = new List<MeshMapping>();
            foreach (var kvp in selectedMappings)
            {
                if (kvp.Key.ObjectMapping == null)
                    keysToReplace.Add(kvp.Key);
            }
            foreach (var oldKey in keysToReplace)
            {
                MeshMapping replacedMapping = ServiceLocator.GetService<ObjectSelectorService>().GetReplacedMapping(oldKey);
                if(replacedMapping == null) continue;
                
                if (selectedMappings.TryGetValue(oldKey, out var oldList))
                {
                    selectedMappings.Remove(oldKey);
                    selectedMappings[replacedMapping] = oldList;
                }
            }
        }
        
        public void BlockBagId(string bagId, bool block)
        {
            blockedBagIds[bagId] = block;
        }
        
        public void Select(string bagId)
        {
            foundObject.Select(bagId);
            LayerData data = foundObject.LayerData;
            if(data == null || !data.ActiveInHierarchy)
                return;

            string layerId = data.Id.ToString();
            Interaction.AddSelectionColor(layerId, bagId, new Color(1, 0, 0, 0));
            if(!selectedMappings.ContainsKey(foundObject))
                selectedMappings.TryAdd(foundObject, new List<string> { bagId });
            selectedMappings[foundObject].Add(bagId);
            
            Interaction.ApplyColors(foundObject.ObjectMapping, layerId);
        }

        public void Deselect(string bagId)
        {
            foreach (var kvp in selectedMappings.ToList())
            {
                List<string> bagIds = kvp.Value;
                if (bagIds.Remove(bagId))
                {
                    LayerData data = kvp.Key.LayerData;
                    Interaction.RemoveSelectionColor(data.Id.ToString(), bagId);

                    if (bagIds.Count == 0)
                        selectedMappings.Remove(kvp.Key);
                    
                    Interaction.ApplyColors(kvp.Key.ObjectMapping, data.Id.ToString());
                }
            }
        }
        
        public void Deselect()
        {
            foundObject?.Deselect();
            foreach (var kvp in selectedMappings)
            {
                LayerData data = kvp.Key.LayerData;
                foreach (string bagId in kvp.Value)
                {
                    Interaction.RemoveSelectionColor(data.Id.ToString(), bagId);
                }
                if(kvp.Key.ObjectMapping != null)
                    Interaction.ApplyColors(kvp.Key.ObjectMapping, data.Id.ToString());
            }
            selectedMappings.Clear();
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
            if(mapping.ObjectMapping == null) return null;

            layer = GetLayerGameObjectFromMapping(selectedMapping);
            mapping.ObjectMapping.items.TryGetValue(bagID, out var item);
            return item;
        }

        public LayerGameObject GetLayerGameObjectFromMapping(IMapping mapping)
        {
            if (mapping is FeatureMapping featureMapping)
            {
                return featureMapping.VisualisationLayer as LayerGameObject;
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

                LayerGameObject layerGameObject = map.ObjectMapping.GetComponentInParent<LayerGameObject>();
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
            Vector3 groundPosition = pointerToWorldPosition.WorldPointSync.ToUnity();
            Coordinate coord = new Coordinate(groundPosition);
            List<IMapping> mappings = ObjectSelectorService.MappingTree.QueryMappingsContainingNode<MeshMapping>(coord);
            if (mappings.Count == 0)
                return bagId;
            
            foreach (MeshMapping mapping in mappings)
            { 
                LayerData data = mapping.LayerData;
                if (data == null || !data.ActiveInHierarchy)
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

        public void HideSelectedMappings()
        {
            ObjectSelectorService selector = ServiceLocator.GetService<ObjectSelectorService>();
            foreach(KeyValuePair<MeshMapping, List<string>> selectedMapping in selectedMappings)
            {
                LayerGameObject layer;
                MeshMapping mapping = selectedMapping.Key;
                if (mapping.ObjectMapping == null)
                    mapping = selector.GetReplacedMapping(mapping);

                foreach (string bagId in selectedMapping.Value)
                {
                    //try to get the existing layerfeature if the feature was already styled, if not create a new and add to the visualisation
                    LayerFeature feature = selector.SubObjectSelector.GetLayerFeatureFromBagID(bagId, mapping, out layer);
                    if (feature == null)
                    {
                        ObjectMappingItem item = selector.SubObjectSelector.GetMappingItemForBagID(bagId, mapping, out layer);
                        feature = layer.CreateFeature(item);
                        layer.LayerFeatures.Add(feature.Geometry, feature);
                    }

                    Coordinate coord = mapping.GetCoordinateForObjectMappingItem(mapping.ObjectMapping, (ObjectMappingItem)feature.Geometry);
                    HiddenObjectsPropertyData hiddenPropertyData = layer.LayerData.GetProperty<HiddenObjectsPropertyData>();
                    hiddenPropertyData.SetVisibilityForSubObject(feature, false, coord);
                }
            }
            //when the object gets hidden, deselect the selection mesh.
            Deselect();
        }
    }
}