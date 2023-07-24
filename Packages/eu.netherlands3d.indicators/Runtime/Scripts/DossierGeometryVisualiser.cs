using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands.GeoJSON;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Indicators
{
    public class DossierGeometryVisualiser : MonoBehaviour
    {
        [SerializeField] private Material meshMaterial;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float meshExtrusionHeight = 10f;

        private readonly List<PolygonVisualisation> areas = new();
        public List<PolygonVisualisation> Areas => areas;

        private FeatureCollection geometry;

        public FeatureCollection Geometry
        {
            get => geometry;
            set
            {
                geometry = value;
                Refresh();
            }
        }

        public UnityEvent<PolygonVisualisation> onAreaVisualised = new();
        public UnityEvent<PolygonVisualisation> onAreaRemoved = new();

        public void Refresh()
        {
            Clear();

            if (geometry == null)
            {
                return;
            }

            for (var featureIndex = 0; featureIndex < geometry.Features.Count; featureIndex++)
            {
                var feature = geometry.Features[featureIndex];

                // if the feature's geometry is a single polygon, convert it immediately
                Polygon polygon = feature.Geometry as Polygon;
                if (polygon != null)
                {
                    VisualizeArea(feature, featureIndex, 0, polygon);
                    continue;
                }

                // if the feature's geometry is a multi-polygon, convert each individual polygon
                MultiPolygon multiPolygon = feature.Geometry as MultiPolygon;
                if (multiPolygon == null) continue;

                for (var polygonIndex = 0; polygonIndex < multiPolygon.Coordinates.Count; polygonIndex++)
                {
                    VisualizeArea(feature, featureIndex, polygonIndex, multiPolygon.Coordinates[polygonIndex]);
                }
            }
        }

        private void VisualizeArea(Feature feature, int featureIndex, int polygonIndex, Polygon polygon)
        {
            string id = feature.TryGetIdentifier("Feature" + featureIndex);
            
            var visualisationFromPolygon = CreateVisualisationFromPolygon(polygon);
            visualisationFromPolygon.gameObject.name = $"{id}::{polygonIndex}";
            Areas.Add(visualisationFromPolygon);
            onAreaVisualised.Invoke(visualisationFromPolygon);
        }

        private PolygonVisualisation CreateVisualisationFromPolygon(Polygon polygon)
        {
            var visualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(
                CreateContoursFromPolygon(polygon),
                meshExtrusionHeight,
                true,
                true,
                true,
                meshMaterial,
                lineMaterial
            );
            
            visualisation.transform.SetParent(transform);
            return visualisation;
        }

        private List<List<Vector3>> CreateContoursFromPolygon(Polygon poly)
        {
            return poly.Coordinates.Select(CreateContourFromLineString).ToList();
        }

        private List<Vector3> CreateContourFromLineString(LineString line)
        {
            var contour = new List<Vector3>();
            foreach (var position in line.Coordinates)
            {
                var coordinate = new Coordinate(
                    CoordinateSystem.EPSG_3857, // TODO: Remove this assumption
                    position.Longitude,
                    position.Latitude,
                    position.Altitude.GetValueOrDefault(0)
                );

                contour.Add(CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.Unity).ToVector3());
            }

            // Close polygon
            contour.Add(contour[0]);

            return contour;
        }

        private void Clear()
        {
            foreach (var polygon in Areas)
            {
                onAreaRemoved.Invoke(polygon);

                Destroy(polygon.gameObject);
            }
            Areas.Clear();
        }
    }
}