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

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
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
            Dictionary<string, IMapping> selectedMappings = selector.SelectedMappings;
            
            foreach(KeyValuePair<string, IMapping> selectedMapping in selectedMappings)
            {
                LayerGameObject layer;
                if (selectedMapping.Value is MeshMapping mapping)
                {
                    if (mapping.ObjectMapping == null)
                        mapping = selector.GetReplacedMapping(mapping);

                    LayerFeature feature = selector.GetLayerFeatureFromBagID(selectedMapping.Key, mapping, out layer);
                    if (layer != null)
                    {   
                        Coordinate coord = mapping.GetCoordinateForObjectMappingItem(mapping.ObjectMapping, (ObjectMappingItem)feature.Geometry);
                        HiddenObjectsPropertyData hiddenPropertyData = layer.LayerData.GetProperty<HiddenObjectsPropertyData>();
                        hiddenPropertyData.SetVisibilityForSubObject(feature, false, coord);
                            
                        //when the object gets hidden, deselect the selection mesh.
                        Deselect();
                    }
                }
            }
        }
    }
}