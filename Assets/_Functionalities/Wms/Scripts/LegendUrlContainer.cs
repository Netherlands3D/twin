using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.Wms
{
    public class LegendUrlContainer
    {
        public string GetCapabilitiesUrl;
        public Dictionary<string, LegendEntry> LayerNameLegendUrlDictionary = new();
        public int ActiveLayerCount;
        
        public sealed class LegendEntry
        {
            public readonly string LayerName;
            public readonly string Url;

            public Texture2D Texture { get; private set; }
            public bool Active { get; private set; }

            public UnityEvent<string, bool> LayerActiveChanged = new();

            public LegendEntry(string layerName, string url)
            {
                LayerName = layerName;
                Url = url;
                Active = true;
            }

            public void SetTexture(Texture2D texture)
            {
                Texture = texture;
            }

            public void SetActive(bool active)
            {
                Active = active;
                LayerActiveChanged.Invoke(LayerName, active);
            }
        }

        public LegendUrlContainer(string getCapabilitiesUrl, Dictionary<string, string> legendDictionary)
        {
            GetCapabilitiesUrl = getCapabilitiesUrl;
            foreach(KeyValuePair<string, string> kv in  legendDictionary)
                LayerNameLegendUrlDictionary.Add(kv.Key, new LegendEntry(kv.Key, kv.Value));
            ActiveLayerCount = 1; // when creating a new object, we assume it has been created by one layer
        }

        public void IncrementLayerCount()
        {
            ActiveLayerCount++;
        }

        public void DecrementLayerCount()
        {
            ActiveLayerCount--;
        }

        public void RegisterImage(Texture2D texture, string layerName)
        {
            LayerNameLegendUrlDictionary[layerName].SetTexture(texture);
        }

        public void RegisterActiveLayer(string layerName, bool active)
        {
            LayerNameLegendUrlDictionary[layerName].SetActive(active);
        }
    }
    
    public record LegendContainerPayload(LegendUrlContainer container, string layerName)
    {
        public LegendUrlContainer container { get; } = container;
        public string layerName { get; } = layerName;
    }
}