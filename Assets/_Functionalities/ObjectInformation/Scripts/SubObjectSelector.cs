using System;
using System.Collections.Generic;
using Netherlands3D.SubObjects;
using UnityEngine;

namespace Netherlands3D.Twin.ObjectInformation
{
    public class SubObjectSelector : MonoBehaviour, IObjectSelector
    {
        public ObjectMapping Object { get => foundObject; }

        [SerializeField] private float hitDistance = 100000f;
        private ColorSetLayer ColorSetLayer { get; set; } = new(0, new());
        private ObjectMapping foundObject;

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

        public bool FindSubObject(Ray ray, out RaycastHit hit, Action<string> onFound)
        {
            foundObject = null;
            if (!Physics.Raycast(ray, out hit, hitDistance)) return false;

            // lets use a capsule cast here to ensure objects are hit (some objects for features are really small) and
            // use a nonalloc to prevent memory allocations
            var objectMapping = hit.collider.gameObject.GetComponent<ObjectMapping>();
            if (!objectMapping) return false;

            foundObject = objectMapping;
            var bagId = objectMapping.getObjectID(hit.triangleIndex);
            onFound?.Invoke(bagId);

            return true;
        }
    }
}