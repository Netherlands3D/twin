using System.Collections;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public static class GeometryColorizer
    {
        public static Dictionary<string, Color> PrioritizedColors { get; private set; } = new();
        private static Dictionary<int, Dictionary<string, Color>> customColors = new();
        
        public static UnityEvent<Dictionary<string, Color>> ColorsChanged = new();

        //positive number priority is the layer order, negative numbers are reserved for the system to override user settings
        public static void AddAndMergeCustomColorSet(int priorityIndex, Dictionary<string, Color> colorSet)
        {
            if (customColors.ContainsKey(priorityIndex))
            {
                var dict = customColors[priorityIndex];
                foreach (var kvp in colorSet)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                customColors.Add(priorityIndex, colorSet);
            }

            RecalculatePrioritizedColors();

            CalculateChangedColors(colorSet);
        }

        private static void CalculateChangedColors(Dictionary<string, Color> changedColorSet)
        {
            var changedColors = new Dictionary<string, Color>();
            foreach (var key in changedColorSet.Keys)
            {
                //if the value in the prioritized Colors list is the same as the one we just added, this is a changed color (or this set contains the same color as the previous prioritized color) 
                if (changedColorSet[key] == PrioritizedColors[key])
                    changedColors.Add(key, PrioritizedColors[key]);
            }

            ColorsChanged.Invoke(changedColors);
        }

        public static void RemoveCustomColorSet(int priorityIndex)
        {
            var changedColors = new Dictionary<string, Color>(customColors[priorityIndex]);
            customColors.Remove(priorityIndex);
            RecalculatePrioritizedColors();

            CalculateChangedColors(changedColors);
        }

        private static void RecalculatePriorities()
        {
            var keys = new List<int>();
            foreach (var colorSet in customColors)
            {
                keys.Add(colorSet.Key);
            }

            keys.Sort();
            var newCollection = new Dictionary<int, Dictionary<string, Color>>();
            for (var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                newCollection[i] = customColors[key];
            }

            customColors = newCollection;
        }

        public static int GetLowestPriorityIndex()
        {
            return customColors.Count;
        }

        public static void RecalculatePrioritizedColors()
        {
            RecalculatePriorities();
            PrioritizedColors = new Dictionary<string, Color>();
            for (var i = customColors.Count - 1; i >= 0; i--)
            {
                var dict = customColors[i];
                foreach (var kvp in dict)
                {
                    PrioritizedColors[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}