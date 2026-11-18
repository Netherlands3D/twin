using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.SubObjects
{
    public struct Color24
    {
        public byte r, g, b;

        public Color24(Color color)
        {
            r = (byte)Mathf.RoundToInt(color.r * 255f);
            g = (byte)Mathf.RoundToInt(color.g * 255f);
            b = (byte)Mathf.RoundToInt(color.b * 255f);
        }

        public static implicit operator Color24(Color color) => new(color);
        public bool Equals(Color24 other) => r == other.r && g == other.g && b == other.b;
        public override bool Equals(object obj) => obj is Color24 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(r, g, b);
        public static bool operator ==(Color24 left, Color24 right) => left.Equals(right);
        public static bool operator !=(Color24 left, Color24 right) => !left.Equals(right);
    }

    public static class Interaction
    {
        public static readonly Color NO_OVERRIDE_COLOR = new Color(0, 0, 1, 0);
        private static readonly Color24 NO_OVERRIDE_COLOR24 = NO_OVERRIDE_COLOR;

        public delegate void ObjectMappingHandler(ObjectMapping mapping);
        public static event ObjectMappingHandler ObjectMappingCheckIn;
        public static event ObjectMappingHandler ObjectMappingCheckOut;

        private static List<ObjectMapping> mappings;
        private static Color24[] colorBuffer = new Color24[0];
        private static readonly Dictionary<string, Color24> overrideColors = new();
        private static readonly Dictionary<string, Dictionary<string, Color24>> layerColors = new();
        private static readonly Dictionary<string, Dictionary<string, Color24>> selectionColors = new();

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

        private static void Set(Dictionary<string, Dictionary<string, Color24>> store, string layerId, string objectId, Color24 color)
        {
            if (!store.TryGetValue(layerId, out var dict))
                store[layerId] = dict = new Dictionary<string, Color24>();
            dict[objectId] = color;
        }

        private static void Remove(Dictionary<string, Dictionary<string, Color24>> store, string layerId, string objectId)
        {
            if (store.TryGetValue(layerId, out var dict))
            {
                dict.Remove(objectId);
                if (dict.Count == 0)
                    store.Remove(layerId);
            }
        }

        public static void AddLayerColor(string layerId, string objectId, Color color) => Set(layerColors, layerId, objectId, color);
        public static void RemoveLayerColor(string layerId, string objectId) => Remove(layerColors, layerId, objectId);
        public static void AddSelectionColor(string layerId, string objectId, Color color) => Set(selectionColors, layerId, objectId, color);
        public static void RemoveSelectionColor(string layerId, string objectId) => Remove(selectionColors, layerId, objectId);

        public static bool TryGetLayerColor(string layerId, string objectId, out Color24 color)
        {
            color = default;

            if (layerColors.TryGetValue(layerId, out var dict))
            {
                return dict.TryGetValue(objectId, out color);
            }

            return false;
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
                colorBuffer = new Color24[Mathf.NextPowerOfTwo(vertexCount)];

            Dictionary<string, Color24> layer = null;
            Dictionary<string, Color24> selection = null;
            if (layerId != null)
            {
                layerColors.TryGetValue(layerId, out layer);
                selectionColors.TryGetValue(layerId, out selection);
            }

            bool anyColor = false;
            int offset = 0;
            var buffer = colorBuffer;

            foreach (var item in mapping.items)
            {
                string key = item.Key;
                Color24 color;

                if (hasOverrides && overrideColors.TryGetValue(key, out color))
                {
                    anyColor = true;
                }
                else if (selection != null && selection.TryGetValue(key, out color))
                {
                    anyColor = true;
                }
                else if (layer != null && layer.TryGetValue(key, out color))
                {
                    anyColor = true;
                }
                else
                {
                    color = NO_OVERRIDE_COLOR24;
                }

                int end = offset + item.Value.verticesLength;
                for (int j = offset; j < end; j++)
                    buffer[j] = color;
                offset = end;
            }

            if (anyColor)
            {
                // Color24 is converted to Color32 for Unity's mesh API
                var color32Buffer = new Color32[offset];
                for (int i = 0; i < offset; i++)
                    color32Buffer[i] = new Color32(buffer[i].r, buffer[i].g, buffer[i].b, 0);

                mesh.SetColors(color32Buffer, 0, offset);
            }
            else if (mesh.HasVertexAttribute(VertexAttribute.Color))
            {
                mesh.colors32 = null;
            }
        }
    }
}