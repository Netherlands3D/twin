using System.Collections;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class FeatureMapping : MonoBehaviour
    {
        public string FeatureID => feature.Id;
        public IGeoJsonVisualisationLayer VisualisationLayer { get { return visualisationLayer; } }
        public GeoJsonLayerGameObject VisualisationParent { get { return geoJsonLayerParent; } }
        public List<Mesh> FeatureMeshes { get { return visualisationLayer.GetMeshData(feature); } }
        public Feature Feature { get { return feature; } }
        public int LayerOrder { get { return geoJsonLayerParent.LayerData.RootIndex; } }
        public Coordinate Position;
        public BoundingBox BoundingBox => boundingBox;

        private Feature feature;
        private List<Mesh> meshes;
        private IGeoJsonVisualisationLayer visualisationLayer;
        private GeoJsonLayerGameObject geoJsonLayerParent;
        private BoundingBox boundingBox;

        public void SetGeoJsonLayerParent(GeoJsonLayerGameObject parentLayer)
        {
            geoJsonLayerParent = parentLayer;
        }

        public void SetPosition(Coordinate position)
        {
            this.Position = position;
        }

        public void SetFeature(Feature feature)
        {
            this.feature = feature; 
        }

        public void SetMeshes(List<Mesh> meshes)
        {
            this.meshes = meshes;       
        }

        public void SetVisualisationLayer(IGeoJsonVisualisationLayer visualisationLayer)
        {
            this.visualisationLayer = visualisationLayer;
            
        }

        //maybe this should be automated and called within the set visualisation layer
        public void UpdateBoundingBox()
        {
            if (feature == null)
            {
                Debug.LogError("must have feature for boundingbox");
                return;
            }
            if (visualisationLayer == null)
            {
                Debug.LogError("must have a geojson visualisation layer for feature");
                return;
            }

            boundingBox = CreateBoundingBoxForFeature(feature, visualisationLayer);
        }

        public static BoundingBox CreateBoundingBoxForFeature(Feature feature, IGeoJsonVisualisationLayer layer)
        {
            Bounds featureBounds = layer.GetFeatureBounds(feature);
            Coordinate bottomLeft = new Coordinate(CoordinateSystem.Unity, featureBounds.min.x, featureBounds.min.y, featureBounds.min.z);
            Coordinate topRight = new Coordinate(CoordinateSystem.Unity, featureBounds.max.x, featureBounds.max.y, featureBounds.max.z);
            Coordinate blWgs84 = bottomLeft.Convert(CoordinateSystem.WGS84_LatLon);
            Coordinate trWgs84 = topRight.Convert(CoordinateSystem.WGS84_LatLon);
            BoundingBox boundingBox = new BoundingBox(blWgs84, trWgs84);
            return boundingBox;
        }

        public void SelectFeature()
        {
            Color selectionColor = Color.blue;
            visualisationLayer.SetVisualisationColor(transform, meshes, selectionColor);
            foreach (Mesh mesh in meshes)
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; i++)
                    colors[i] = THUMBNAIL_COLOR;
                mesh.SetColors(colors);
            }
        }

        public void DeselectFeature()
        {
            visualisationLayer.SetVisualisationColorToDefault();
            foreach (Mesh mesh in meshes)
            {
                Color[] colors = new Color[mesh.vertexCount];
                for (int i = 0; i < mesh.vertexCount; i++)
                    colors[i] = NO_OVERRIDE_COLOR;
                mesh.SetColors(colors);
            }
        }

        private static readonly Color NO_OVERRIDE_COLOR = new Color(0, 0, 1, 0);
        private static readonly Color THUMBNAIL_COLOR = new Color(1, 0, 0, 0);
    }
}
