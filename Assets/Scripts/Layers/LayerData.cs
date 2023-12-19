using System.Collections;
using System.Collections.Generic;
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
            foreach (var layer in SelectedLayers)
            {
                GameObject.Destroy(layer.Layer.gameObject);
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