using System;
using System.Collections.Generic;
using Netherlands3D.SubObjects;
using UnityEngine;

namespace Netherlands3D.Twin.ObjectInformation
{
    public class SubObjectSelector : MonoBehaviour, IObjectSelector
    {
        public bool HasObjectMapping { get => foundObject != null; }
        public ObjectMapping Object { get => foundObject; }
        public string ObjectID { get => foundId; }

        [SerializeField] private float hitDistance = 100000f;
        private ColorSetLayer ColorSetLayer { get; set; } = new(0, new());
        private ObjectMapping foundObject;
        private string foundId;

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

        public string FindSubObject(Ray ray, out RaycastHit hit)
        {
            foundObject = null;
            if (!Physics.Raycast(ray, out hit, hitDistance)) return null;

            // lets use a capsule cast here to ensure objects are hit (some objects for features are really small) and
            // use a nonalloc to prevent memory allocations
            var objectMapping = hit.collider.gameObject.GetComponent<ObjectMapping>();
            if (!objectMapping) return null;

            foundObject = objectMapping;
            var bagId = objectMapping.getObjectID(hit.triangleIndex);
            return bagId;
        }
    }
}