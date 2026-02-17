using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers;
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
        private Dictionary<string, GameObject> selectedMeshes = new();

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }
        
        public void Select(string bagId)
        {
            GameObject visual = foundObject.Select(bagId);
            if(visual != null)
                selectedMeshes.Add(bagId, visual);
        }

        public void Deselect(string bagId)
        {
            if(!selectedMeshes.ContainsKey(bagId)) return;
            
            GameObject visual = selectedMeshes[bagId];
            selectedMeshes.Remove(bagId);
            Destroy(visual);
        }
        
        public void Deselect()
        {
            foundObject?.Deselect();
            foreach(GameObject selectedMesh in selectedMeshes.Values) 
                Destroy(selectedMesh); 
            selectedMeshes.Clear();
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
            Vector3 groundPosition = pointerToWorldPosition.WorldPointSync;
            Coordinate coord = new Coordinate(groundPosition);
            List<IMapping> mappings = ObjectSelectorService.MappingTree.QueryMappingsContainingNode<MeshMapping>(coord);
            if (mappings.Count == 0)
                return bagId;

            foreach (MeshMapping mapping in mappings)
            { 
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
    }
}