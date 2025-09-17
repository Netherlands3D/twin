using UnityEngine;

namespace Netherlands3D.Twin.Layers.ExtensionMethods
{
    public class LayerColor
    {
        public static Color Random()
        {
            var randomLayerColor = Color.HSVToRGB(
                UnityEngine.Random.value, 
                UnityEngine.Random.Range(0.5f, 1f), 
                1
            );
            randomLayerColor.a = 0.5f;
            
            return randomLayerColor;
        }
    }
}