using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public static class LayerData
    {
        public static HashSet<LayerNL3DBase> AllLayers { get; set; } = new HashSet<LayerNL3DBase>();
        public static List<LayerUI> LayersVisibleInInspector { get; set; } = new List<LayerUI>();
        public static List<LayerUI> SelectedLayers { get; set; } = new();

        public static UnityEvent<LayerNL3DBase> LayerAdded = new();
        public static UnityEvent<LayerNL3DBase> LayerDeleted = new();

        public static void AddLayer(LayerNL3DBase newLayer)
        {
            AllLayers.Add(newLayer);
            LayerAdded.Invoke(newLayer);
        }

        public static void RemoveLayer(LayerNL3DBase layer)
        {
            AllLayers.Remove(layer);
            LayerDeleted.Invoke(layer);
        }

        public static void DeleteSelectedLayers()
        {
            var layersToBeDeleted = GetAllNestedChildren(SelectedLayers);
            foreach (var layer in layersToBeDeleted)
            {
                GameObject.Destroy(layer.gameObject);
            }
        }

        // Function to get all nested children from a list of root transforms without duplicates
        private static List<LayerNL3DBase> GetAllNestedChildren(List<LayerUI> rootTransforms, bool includeParent = true)
        {
            List<LayerNL3DBase> allChildren = new List<LayerNL3DBase>();
            HashSet<LayerNL3DBase> uniqueChildren = new HashSet<LayerNL3DBase>();

            foreach (LayerUI root in rootTransforms)
            {
                if (includeParent)
                    uniqueChildren.Add(root.Layer);

                GetAllNestedChildrenRecursive(root, uniqueChildren);
            }

            allChildren.AddRange(uniqueChildren);
            return allChildren;
        }

        private static void GetAllNestedChildrenRecursive(LayerUI parent, HashSet<LayerNL3DBase> uniqueChildren)
        {
            foreach (var child in parent.ChildrenUI)
            {
                // Add the current child to the set to ensure uniqueness
                uniqueChildren.Add(child.Layer);

                // Recursively call the function for each child
                GetAllNestedChildrenRecursive(child, uniqueChildren);
            }
        }


        public static void RemoveUI(LayerUI layerUI)
        {
            if (SelectedLayers.Contains(layerUI))
                SelectedLayers.Remove(layerUI);

            if (LayersVisibleInInspector.Contains(layerUI))
                LayersVisibleInInspector.Remove(layerUI);
        }
    }
}