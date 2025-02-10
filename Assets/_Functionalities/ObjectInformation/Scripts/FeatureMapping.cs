using System.Collections;
using System.Collections.Generic;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class FeatureMapping : MonoBehaviour, IMapping
    {
        public string DebugID;

        public string FeatureID => feature.Id;
        public IGeoJsonVisualisationLayer VisualisationLayer { get { return visualisationLayer; } }
        public GeoJsonLayerGameObject VisualisationParent { get { return geoJsonLayerParent; } }
        public List<Mesh> FeatureMeshes { get { return visualisationLayer.GetMeshData(feature); } }
        public Feature Feature { get { return feature; } }
        public int LayerOrder { get { return geoJsonLayerParent.LayerData.RootIndex; } }
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

        public void SetFeature(Feature feature)
        {
            this.feature = feature; 
            
            DebugID = feature.Id;
        }

        public void SetMeshes(List<Mesh> meshes)
        {
            this.meshes = meshes;       
        }

        public void SetVisualisationLayer(IGeoJsonVisualisationLayer visualisationLayer)
        {
            this.visualisationLayer = visualisationLayer;
            
        }

        /// <summary>
        /// calculating a hit on the XZ plane
        /// </summary>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <param name="hitPoint"></param>
        /// <returns></returns>
        public bool IsPositionHit(Coordinate position, float range, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            List<Mesh> meshes = FeatureMeshes;
            Vector3 unityPos = position.ToUnity();
            Vector2 xzPos = new Vector2(unityPos.x, unityPos.z);
            for(int i = 0; i < meshes.Count; i++)
            {
                if (feature.Geometry is MultiLineString || feature.Geometry is LineString)
                {
                    Vector3[] vertices = meshes[i].vertices;
                    for (int j = 0; j < meshes[i].vertexCount - 1; j++)
                    {
                        Vector3 xzStart = new Vector3(vertices[j].x, unityPos.y, vertices[j].z);
                        Vector3 xzEnd = new Vector3(vertices[j + 1].x, unityPos.y, vertices[j + 1].z);
                        Vector3 nearest = FeatureSelector.NearestPointOnFiniteLine(xzStart, xzEnd, unityPos);
                        float dist = Vector2.SqrMagnitude(xzPos - new Vector2(nearest.x, nearest.z));
                        if (dist < range * range)
                        {
                            hitPoint = nearest;
                            return true;
                        }
                    }
                }
                else if (feature.Geometry is MultiPolygon || feature.Geometry is Polygon)
                {
                    Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
                    GeoJSONPolygonLayer polygonLayer = visualisationLayer as GeoJSONPolygonLayer;
                    PolygonVisualisation pv = polygonLayer.GetPolygonVisualisationByMesh(meshes);
                    bool isSelected = FeatureSelector.ProcessPolygonSelection(meshes[i], pv.transform, frustumPlanes, unityPos);
                    if (isSelected)
                    {
                        hitPoint = unityPos;
                        return true;
                    }
                }
                else if (feature.Geometry is Point || feature.Geometry is MultiPoint)
                {
                    Vector3[] vertices = meshes[i].vertices;
                    for (int j = 0; j < meshes[i].vertexCount; j++)
                    {
                        Vector2 xzVertex = new Vector2(vertices[j].x, vertices[j].z);
                        float dist = Vector2.SqrMagnitude(xzPos - xzVertex);
                        if (dist < range * range)
                        {
                            hitPoint = new Vector3(xzVertex.x, unityPos.y, xzVertex.y);
                            return true;
                        }
                    }
                }
            }            
            return false;
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
