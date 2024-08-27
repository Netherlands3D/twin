using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class LayerURLPropertyData : LayerPropertyData, ILayerPropertyDataWithAssets
    {
        public string url = "";
        
        public IEnumerable<LayerAsset> GetAssets()
        {
            return new List<LayerAsset>()
            {
                new (this, !string.IsNullOrEmpty(url) ? new Uri(url) : null)
            };
        }
    }
}
