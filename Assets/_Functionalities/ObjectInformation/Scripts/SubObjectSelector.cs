using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class SubObjectSelector : MonoBehaviour, IObjectSelector
    {
        public bool HasObjectMapping => foundObject != null;
        public ObjectMapping Object => foundObject; 
        public string ObjectID => foundId; 

        [SerializeField] private float hitDistance = 100000f;
        private ColorSetLayer ColorSetLayer { get; set; } = new(0, new());
        private ObjectMapping foundObject;
        private string foundId;

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

        public void Deselect()
        {
            GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
            ColorSetLayer = null;
        }

        public string FindSubObject()
        {
            foundObject = null;
            string bagId = null;
            //if (!Physics.Raycast(ray, out hit, hitDistance)) return null;

            //// lets use a capsule cast here to ensure objects are hit (some objects for features are really small) and
            //// use a nonalloc to prevent memory allocations
            //var objectMapping = hit.collider.gameObject.GetComponent<ObjectMapping>();
            //if (!objectMapping) return null;

            Vector3 groundPosition = pointerToWorldPosition.WorldPoint;
            Coordinate coord = new Coordinate(groundPosition);
            List<IMapping> mappings = BagInspector.MappingTree.QueryMappingsContainingNode<MeshMapping>(coord);
            if (mappings.Count == 0)
                return bagId;

            foreach (MeshMapping mapping in mappings)
            {                
                ObjectMapping objectMapping = mapping.ObjectMapping;
                //var bagId = objectMapping.getObjectID(hit.triangleIndex);
                MeshMappingItem item = mapping.FindItemForPosition(groundPosition);
                if (item != null)
                {
                    foundObject = objectMapping;
                    bagId = item.ObjectMappingItem.objectID;
                    break;
                }
            }
            return bagId;
        }
    }
}