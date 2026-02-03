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
using UnityEngine.UI;

namespace Netherlands3D
{
    public class PolygonFeatureToLayer : MonoBehaviour
    {
        [SerializeField] private Button button;
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
            button.onClick.AddListener(ConvertToLayer);
        }

        private void ConvertToLayer()
        {
            if (feature == null)
            {
                Debug.LogError("Feature mapping not set, cannot convert anything to layer.");
                return;
            }
            
            if (feature.Geometry is MultiPolygon multiPolygon)
            {
                if (multiPolygon.Coordinates.Count == 1) //no folder for a single polygon
                {
                    CreatePolygonLayer(multiPolygon.Coordinates[0], feature.Id);
                    return;
                }

                var builder = new LayerBuilder().OfType("folder").NamedAs(feature.Id);
                var folder = App.Layers.Add(builder);
                foreach (var polygon in multiPolygon.Coordinates)
                {
                    var layer = CreatePolygonLayer(polygon, feature.Id);
                    layer.LayerData.SetParent(folder.LayerData);
                }
            }
            else if (feature.Geometry is Polygon polygon)
            {
                CreatePolygonLayer(polygon,  feature.Id);
            }
        }

        private Layer CreatePolygonLayer(Polygon polygon, string name)
        {
            var solidPolygon = polygon.Coordinates[0];
            var list = GeometryVisualizationFactory.ConvertToUnityCoordinates(solidPolygon, GeoJSONParser.GetCoordinateSystem(feature.CRS));

            var polygonPropertyData = new PolygonSelectionLayerPropertyData();
            polygonPropertyData.OriginalPolygon = list;
            
            var preset = new PolygonLayerPreset.Args(
                name,
                ShapeType.Polygon,
                list
            );
            var layer = App.Layers.Add(preset);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            return layer;
        }

        private void OnDisable()
        {
            objectSelector.SelectFeature.RemoveListener(OnFeatureSelected);
            objectSelector.OnDeselect.RemoveListener(OnFeatureDeselected);
            button.onClick.RemoveListener(ConvertToLayer);
        }

        private void OnFeatureSelected(FeatureMapping featureMapping)
        {
            button.gameObject.SetActive(true);
            feature = featureMapping.Feature;
        }

        private void OnFeatureDeselected()
        {
            button.gameObject.SetActive(false);
            feature = null;
        }
    }
}