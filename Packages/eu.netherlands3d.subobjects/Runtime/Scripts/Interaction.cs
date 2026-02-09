using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.SubObjects
{
    public static class Interaction
    {
        public static readonly Color NO_OVERRIDE_COLOR = new Color(0, 0, 1, 0);
        public delegate void ObjectMappingHandler(ObjectMapping mapping);
        public static event ObjectMappingHandler ObjectMappingCheckIn;
        public static event ObjectMappingHandler ObjectMappingCheckOut;

        private static List<Color> vertexcolors = new();
        static List<ObjectMapping> mappings;
        
        private static Dictionary<string, Color> overrideColors = new();

        internal static void CheckIn(ObjectMapping mapping)
        {
            if (mappings == null)
            {
                mappings = new List<ObjectMapping>();
            }
            mappings.Add(mapping);
            ObjectMappingCheckIn?.Invoke(mapping);
        }

        internal static void CheckOut(ObjectMapping mapping)
        {
            if (mappings.Contains(mapping))
            {
                mappings.Remove(mapping);
                ObjectMappingCheckOut?.Invoke(mapping);
            }
        }


        public static void AddOverrideColors(Dictionary<string, Color> colorMap)
        {
            foreach (var kv in colorMap)
                overrideColors[kv.Key] = kv.Value; 
        }
        
        public static void AddOverrideColor(string key, Color color) => overrideColors[key] = color;
        
        public static void RemoveOverrideColors(Dictionary<string, Color> colorMap)
        {
            foreach (var kv in colorMap)
                overrideColors.Remove(kv.Key);
        }
        
        public static void RemoveOverrideColor(string key) => overrideColors.Remove(key);

        public static void ApplyColors(ObjectMapping mapping)
        {
            GameObject gameobject = mapping.gameObject;
            if (gameobject == null) return;
            Mesh mesh = gameobject.GetComponent<MeshFilter>().mesh;
            if (mesh == null)   return;
          
            if (vertexcolors.Capacity < mesh.vertexCount)
                vertexcolors.Capacity = mesh.vertexCount;
            
            bool applied = false;
            foreach(KeyValuePair<string, ObjectMappingItem> item in mapping.items)
            {
                Color color;
                if (overrideColors.ContainsKey(item.Key))
                {
                    color = overrideColors[item.Key];
                    applied = true;
                }
                else
                    color = NO_OVERRIDE_COLOR;
                
                int vertexcount = item.Value.verticesLength;
                for (int j = 0; j < vertexcount; j++)
                    vertexcolors.Add(color);
            }
            if(applied)
                mesh.SetColors(vertexcolors);
            vertexcolors.Clear();
        }
    }
}