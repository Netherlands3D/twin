using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.SubObjects
{
    public static class Interaction
    {
        public static readonly Color NO_OVERRIDE_COLOR = new Color(0, 0, 1, 0);
        private static readonly Color32 NO_OVERRIDE_COLOR32 = NO_OVERRIDE_COLOR;

        public delegate void ObjectMappingHandler(ObjectMapping mapping);
        public static event ObjectMappingHandler ObjectMappingCheckIn;
        public static event ObjectMappingHandler ObjectMappingCheckOut;

        private static List<ObjectMapping> mappings;
        private static Color32[] colorBuffer = new Color32[0];
        private static readonly Dictionary<string, Color32> overrideColors = new(); 
        private static readonly Dictionary<string, Dictionary<string, Color32>> layerColors = new();
        private static readonly Dictionary<string, Dictionary<string, Color32>> selectionColors = new();
        private static readonly Dictionary<string, Dictionary<string, Color32>> resolvedCache = new();

        internal static void CheckIn(ObjectMapping mapping)
        {
            mappings ??= new List<ObjectMapping>();
            mappings.Add(mapping);
            ObjectMappingCheckIn?.Invoke(mapping);
            //apply after objectmappingcheckin invoke!
            ApplyColors(mapping);
        }

        internal static void CheckOut(ObjectMapping mapping)
        {
            if (mappings != null && mappings.Remove(mapping))
                ObjectMappingCheckOut?.Invoke(mapping);
        }
        
        //todo solve duplicate colors
        public static void AddOverrideColors(Dictionary<string, Color> colorMap)
        {
            foreach (var kv in colorMap) overrideColors[kv.Key] = kv.Value;
        }

        public static void RemoveOverrideColors(Dictionary<string, Color> colorMap)
        {
            foreach (var kv in colorMap) overrideColors.Remove(kv.Key);
        }
        
        private static void Set(Dictionary<string, Dictionary<string, Color32>> store, string layerId, string objectId, Color32 color)
        {
            if (!store.TryGetValue(layerId, out var dict))
                store[layerId] = dict = new Dictionary<string, Color32>();
            dict[objectId] = color;
            resolvedCache.Remove(layerId);
        }

        private static void Remove(Dictionary<string, Dictionary<string, Color32>> store, string layerId, string objectId)
        {
            if (store.TryGetValue(layerId, out var dict) && dict.Remove(objectId))
                resolvedCache.Remove(layerId);
        }

        public static void AddLayerColor(string layerId, string objectId, Color color) => Set(layerColors, layerId, objectId, color);
        public static void RemoveLayerColor(string layerId, string objectId) => Remove(layerColors, layerId, objectId);
        public static void AddSelectionColor(string layerId, string objectId, Color color) => Set(selectionColors, layerId, objectId, color);
        public static void RemoveSelectionColor(string layerId, string objectId) => Remove(selectionColors, layerId, objectId);

        private static Dictionary<string, Color32> GetResolved(string layerId)
        {
            if (resolvedCache.TryGetValue(layerId, out var resolved))
                return resolved;

            layerColors.TryGetValue(layerId, out var layer);
            selectionColors.TryGetValue(layerId, out var selection);

            resolved = new Dictionary<string, Color32>((layer?.Count ?? 0) + (selection?.Count ?? 0));
            if (layer != null)
                foreach (var kv in layer) resolved[kv.Key] = kv.Value;
            if (selection != null)
                foreach (var kv in selection) resolved[kv.Key] = kv.Value; 

            resolvedCache[layerId] = resolved;
            return resolved;
        }

        //TODO we will need a cascading coloring system to apply colors, when multiple colors are registered to one bagid, is this still needed?
        /// <summary>
        /// This will color the final result from styling, by its objectmapping it will find the corresponding bagids in the override color dictionary and apply mesh vertex coloring.
        /// If no object in the mapping has a color, no vertex color channel is kept on the mesh.
        /// </summary>
        public static void ApplyColors(ObjectMapping mapping, string layerId = null)
        {
            bool hasOverrides = overrideColors.Count > 0;
            
            if (layerId == null && !hasOverrides) return;

            if (!mapping.TryGetComponent(out MeshFilter meshFilter)) return;
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) return;

            int vertexCount = mesh.vertexCount;
            if (colorBuffer.Length < vertexCount)
                colorBuffer = new Color32[Mathf.NextPowerOfTwo(vertexCount)];

            Dictionary<string, Color32> resolved = layerId != null ? GetResolved(layerId) : null;
            bool hasResolved = resolved != null;

            bool anyColor = false;
            int offset = 0;
            var buffer = colorBuffer;

            foreach (var item in mapping.items)
            {
                string key = item.Key;
                Color32 color;

                if (hasOverrides && overrideColors.TryGetValue(key, out color))
                    anyColor = true;
                else if (hasResolved && resolved.TryGetValue(key, out color))
                    anyColor = true;
                else
                    color = NO_OVERRIDE_COLOR32;

                int end = offset + item.Value.verticesLength;
                for (int j = offset; j < end; j++)
                    buffer[j] = color;
                offset = end;
            }

            if (anyColor)
            {
                mesh.SetColors(buffer, 0, offset); 
            }
            else if (mesh.HasVertexAttribute(VertexAttribute.Color))
            {
                mesh.colors32 = null;
            }
        }
    }
}