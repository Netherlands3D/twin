using System.Collections.Generic;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Rendering;
using UnityEngine;

namespace Netherlands3D.CityJson.Visualisation
{
    [RequireComponent(typeof(CityObject))]
    [RequireComponent(typeof(PointRenderer3D))]
    public class CityObjectPointVisualizer : CityObjectVisualizer
    {
        private PointRenderer3D  pointRenderer3D;
        [SerializeField] private Mesh visualizationMesh;
        [SerializeField] private CityMaterialConverter materialConverter;

        protected override void Awake()
        {
            base.Awake();
            pointRenderer3D = GetComponent<PointRenderer3D>();
        }
        
        protected override void Visualize()
        {
            materialConverter.Initialize(cityObject.Appearance);
            pointRenderer3D.PointMesh =  visualizationMesh;
                
            foreach (var geometry in cityObject.Geometries)
            {
                if (!(geometry.BoundaryObject is CityMultiPoint multiPoint))
                    continue; // other types have their own visualizer and don't create meshes

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