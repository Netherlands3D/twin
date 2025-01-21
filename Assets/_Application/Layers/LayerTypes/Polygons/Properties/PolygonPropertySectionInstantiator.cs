using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties
{
    public class PolygonPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        public void AddToProperties(RectTransform properties)
        {
            if (!PolygonInputToLayer.PolygonPropertySectionPrefab) return;

            var settings = Instantiate(PolygonInputToLayer.PolygonPropertySectionPrefab, properties);
            settings.PolygonLayer = GetComponent<PolygonSelectionVisualisation>().LayerData as PolygonSelectionLayer;
        }
    }
}
