using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D
{
    public class PolygonFeatureToLayer : MonoBehaviour
    {
        private ObjectSelectorService objectSelector;
        private PolygonSelectionService polygonSelectionService;
        private Feature feature;

        private void Awake()
        {
            objectSelector = ServiceLocator.GetService<ObjectSelectorService>();
            polygonSelectionService = ServiceLocator.GetService<PolygonSelectionService>();
        }

        private void OnEnable()
        {
            objectSelector.SelectFeature.AddListener(OnFeatureSelected);
            objectSelector.OnDeselect.AddListener(OnFeatureDeselected);
        }

       

        private void OnDisable()
        {
            objectSelector.SelectFeature.RemoveListener(OnFeatureSelected);
            objectSelector.OnDeselect.RemoveListener(OnFeatureDeselected);
        }

        private void OnFeatureSelected(FeatureMapping featureMapping)
        {
            if (featureMapping.Feature.Geometry is Polygon || featureMapping.Feature.Geometry is MultiPolygon)
            {
                feature = featureMapping.Feature;
            }
        }

        private void OnFeatureDeselected()
        {
            feature = null;
        }
    }
}