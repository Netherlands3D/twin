using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Legend
{
    public class LegendUrlContainer
    {
        public string GetCapabilitiesUrl;
        public Dictionary<string, LegendEntry> LayerNameLegendUrlDictionary = new();
        
        public sealed class LegendEntry
        {
            public readonly string LayerName;
            public string ImageUrl { get; private set; }
            public Texture2D Texture { get; private set; }
            public bool Active { get; private set; }

            public UnityEvent<string, bool> LayerActiveChanged = new();

            public LegendEntry(string layerName, string imageUrl, bool active)
            {
                LayerName = layerName;
                ImageUrl = imageUrl;
                Active = active;
            }
            
            public void SetImageUrl(string imageUrl)
            {
                ImageUrl = imageUrl;
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

        public LegendUrlContainer(string getCapabilitiesUrl)
        {
            GetCapabilitiesUrl = getCapabilitiesUrl;
        }
        
        public void RegisterImage(Texture2D texture, string layerName)
        {
            LayerNameLegendUrlDictionary[layerName].SetTexture(texture);
        }

        public void SetLayerActive(string layerName, bool active)
        {
            if (LayerNameLegendUrlDictionary.TryGetValue(layerName, out var entry))
                entry.SetActive(active);
            else
                Debug.LogWarning($"[LegendUrlContainer] SetLayerActive: layer '{layerName}' not found.");
        }

        public void RegisterLayer(string layerName, bool isActive)
        {
            if(LayerNameLegendUrlDictionary.ContainsKey(layerName))
                LayerNameLegendUrlDictionary[layerName].SetActive(isActive);
            else
               LayerNameLegendUrlDictionary.Add(layerName, new LegendEntry(layerName, null, isActive));
        }
        
        public void UnregisterLayer(string layerName)
        {
            LayerNameLegendUrlDictionary.Remove(layerName);
        }
        
        public void PopulateUrls(Dictionary<string, string> legendDictionary)
        {
            foreach (KeyValuePair<string, string> kv in legendDictionary)
            {
                if (LayerNameLegendUrlDictionary.TryGetValue(kv.Key, out var entry))
                    entry.SetImageUrl(kv.Value); // layer already registered with active state, just set the url
                else
                    LayerNameLegendUrlDictionary.Add(kv.Key, new LegendEntry(kv.Key, kv.Value, true)); // layer not yet known, add with default active state
            }
        }
    }
    
    public record LegendContainerPayload(LegendUrlContainer container, string layerName)
    {
        public LegendUrlContainer container { get; } = container;
        public string layerName { get; } = layerName;
    }
}