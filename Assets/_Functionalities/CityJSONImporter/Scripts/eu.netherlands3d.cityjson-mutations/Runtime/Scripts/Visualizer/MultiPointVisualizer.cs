using System;
using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.CityJson.Visualisation
{
    [RequireComponent(typeof(CityObject))]
    [RequireComponent(typeof(PointRenderer3D))]
    public class MultiPointVisualizer : CityObjectVisualizer
    {
        private CityObject cityObject;
        private PointRenderer3D  pointRenderer3D;
        [SerializeField] private Mesh visualizationMesh;
        [SerializeField] private CityMaterialConverter materialConverter;

        private void Awake()
        {
            cityObject = GetComponent<CityObject>();
            pointRenderer3D = GetComponent<PointRenderer3D>();
        }

        private void OnEnable()
        {
            cityObject.CityObjectParsed.AddListener(Visualize);
        }

        private void OnDisable()
        {
            cityObject.CityObjectParsed.RemoveListener(Visualize);
        }

        protected override void Visualize()
        {
            materialConverter.Initialize(cityObject.Appearance);
            pointRenderer3D.PointMesh =  visualizationMesh;
                
            foreach (var geometry in cityObject.Geometries)
            {
                if (!(geometry.BoundaryObject is CityMultiPoint multiPoint))
                    throw new NotSupportedException("Boundary is not of Type MultiPoint, use CityObjectVisualiser instead.");

                var coordinates = new List<Coordinate>(multiPoint.VertexCount);
                foreach (var vert in multiPoint.Points.Vertices)
                {
                    var coord = new Coordinate(cityObject.CoordinateSystem, vert.x, vert.y, vert.z);
                    coordinates.Add(coord);
                }
                
                pointRenderer3D.SetPositionCollections(new List<List<Coordinate>>(){coordinates});
            }
        }
    }
}