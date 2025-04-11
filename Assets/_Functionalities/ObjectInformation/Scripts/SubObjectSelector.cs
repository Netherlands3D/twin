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

        private ColorSetLayer ColorSetLayer { get; set; } = new(0, new());
        private MeshMapping foundObject;

        private PointerToWorldPosition pointerToWorldPosition;

        private void Awake()
        {
            pointerToWorldPosition = FindAnyObjectByType<PointerToWorldPosition>();
        }

        public void Select(string bagId)
        {
            Deselect();
            ColorSetLayer = GeometryColorizer.InsertCustomColorSet(
                -1, 
                new Dictionary<string, Color> 
                {
                    { bagId, new Color(1, 0, 0, 0) }
                }
            );
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

        public void Deselect()
        {
            GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
            ColorSetLayer = null;
        }

        public string FindSubObjectAtPointerPosition()
        {
            foundObject = null;
            string bagId = null;
            Vector3 groundPosition = pointerToWorldPosition.WorldPoint;
            Coordinate coord = new Coordinate(groundPosition);
            List<IMapping> mappings = ObjectSelector.MappingTree.QueryMappingsContainingNode<MeshMapping>(coord);
            if (mappings.Count == 0)
                return bagId;

            foreach (MeshMapping mapping in mappings)
            {                
                ObjectMapping objectMapping = mapping.ObjectMapping;
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