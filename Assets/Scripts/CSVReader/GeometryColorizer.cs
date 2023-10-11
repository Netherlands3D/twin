using System.Collections;
using System.Collections.Generic;
using System.Linq;
using netDxf.Entities;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class ColorSetLayer
    {
        public int PriorityIndex { get; set; }
        public Dictionary<string, Color> ColorSet { get; set; }
        public bool Enabled { get; set; } = true;

        public ColorSetLayer(int priorityIndex, Dictionary<string, Color> colorSet)
        {
            PriorityIndex = priorityIndex;
            ColorSet = colorSet;
        }
    }

    public enum IndexCollisionAction
    {
        Increment,
        Swap,
    }

    public static class GeometryColorizer
    {
        public static Dictionary<string, Color> PrioritizedColors { get; private set; } = new();
        private static List<ColorSetLayer> customColors = new();

        public static UnityEvent<Dictionary<string, Color>> ColorsChanged = new();

        public static void ReorderColorSet(int oldIndex, int newIndex, IndexCollisionAction collisionAction)
        {
            ColorSetLayer oldLayer = customColors[oldIndex];

            ColorSetLayer newLayer = null;
            if (newIndex >= customColors.Count)
            {
                customColors.Add(oldLayer);
                customColors.RemoveAt(oldIndex);
                RecalculatePrioritizedColors();
                return;
            }

            newLayer = customColors[newIndex];

            switch (collisionAction)
            {
                case IndexCollisionAction.Increment:
                    InsertCustomColorSet(oldLayer.PriorityIndex, oldLayer.ColorSet);
                    break;
                case IndexCollisionAction.Swap:
                    int oldPriorityIndex = oldLayer.PriorityIndex;
                    int newPriorityIndex = newLayer.PriorityIndex;
                    oldLayer.PriorityIndex = newPriorityIndex;
                    newLayer.PriorityIndex = oldPriorityIndex;
                    break;
            }

            RecalculatePrioritizedColors();
        }

        public static void InsertCustomColorSet(int priorityIndex, Dictionary<string, Color> colorSet)
        {
            IncrementPriorityIndices(priorityIndex);
            AddAndMergeCustomColorSet(priorityIndex, colorSet);
        }

        private static void IncrementPriorityIndices(int fromPriorityIndex)
        {
            ColorSetLayer activeLayer = customColors.FirstOrDefault(l => l.PriorityIndex == fromPriorityIndex);
            while (activeLayer != null)
            {
                fromPriorityIndex++;
                var nextLayer = customColors.FirstOrDefault(l => l.PriorityIndex == fromPriorityIndex); //get next layer before changing current layer to avoid getting it again in the next loop iteration

                activeLayer.PriorityIndex++;
                activeLayer = nextLayer;
            }
        }

        public static void AddAndMergeCustomColorSet(int priorityIndex, Dictionary<string, Color> colorSet)
        {
            if (customColors.FirstOrDefault(l => l.PriorityIndex == priorityIndex) != null)
            {
                var colorSetLayer = customColors[priorityIndex];
                foreach (var kvp in colorSet)
                {
                    colorSetLayer.ColorSet[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                var newLayer = new ColorSetLayer(priorityIndex, colorSet);
                customColors.Add(newLayer);
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

        public static void RemoveCustomColorSet(int layerIndex)
        {
            var colorSetLayer = customColors[layerIndex];

            var changedColors = new Dictionary<string, Color>(colorSetLayer.ColorSet);
            customColors.RemoveAt(layerIndex);
            RecalculatePrioritizedColors();

            CalculateChangedColors(changedColors);
        }

        private static void ReorderColorMaps()
        {
            customColors = customColors.OrderBy(layer => layer.PriorityIndex).ToList();
        }

        public static int GetLowestPriorityIndex()
        {
            return customColors.Max(l => l.PriorityIndex);
        }

        public static void RecalculatePrioritizedColors()
        {
            ReorderColorMaps();
            PrioritizedColors = new Dictionary<string, Color>();
            for (var i = customColors.Count - 1; i >= 0; i--)
            {
                var layer = customColors[i];
                if(!layer.Enabled)
                    continue;
                
                foreach (var kvp in layer.ColorSet)
                {
                    PrioritizedColors[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}