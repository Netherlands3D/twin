using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class LayerData : MonoBehaviour
    {
        public static LayerData Instance { get; private set; }
        public static HashSet<LayerNL3DBase> AllLayers { get; set; } = new HashSet<LayerNL3DBase>();
        public static List<LayerUI> LayersVisibleInInspector { get; set; } = new List<LayerUI>();
        public static List<LayerUI> SelectedLayers { get; set; } = new();

        public static UnityEvent<LayerNL3DBase> LayerAdded = new();
        public static UnityEvent<LayerNL3DBase> LayerDeleted = new();

        private void Awake()
        {
            if (Instance)
                Debug.LogError("Another LayerData Object already exists, there should be only one LayerData object. The existing object will be overwritten", Instance.gameObject);

            Instance = this;
        }

        public static void AddStandardLayer(LayerNL3DBase newLayer)
        {
            Debug.Log("adding layer: " + newLayer.name);
            AllLayers.Add(newLayer);
            newLayer.transform.SetParent(Instance.transform);
            LayerAdded.Invoke(newLayer);
        }

        public static void RemoveLayer(LayerNL3DBase layer)
        {
            AllLayers.Remove(layer);
            LayerDeleted.Invoke(layer);
        }

        public static void AddReferenceLayer(ReferencedLayer referencedLayer)
        {
            Debug.Log("adding reference layer: " + referencedLayer.name);
            var referenceLayerObject = new GameObject(referencedLayer.name);
            var proxyLayer = referenceLayerObject.AddComponent<ReferenceLayer>(); 
            proxyLayer.Reference = referencedLayer;
            referencedLayer.Reference = proxyLayer;
        }
    }
}