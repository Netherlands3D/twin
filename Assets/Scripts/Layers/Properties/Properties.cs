using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class Properties : MonoBehaviour
    {
        public void Show(ILayerWithProperties layer)
        {
            transform.ClearAllChildren();
            Debug.Log("Showing properties for " + layer);
            foreach (var propertySection in layer.GetPropertySections())
            {
                propertySection.AddToProperties(this);
            }
        }

        public void Hide()
        {
            Debug.Log("Hiding properties");
            transform.ClearAllChildren();
        }
    }
}