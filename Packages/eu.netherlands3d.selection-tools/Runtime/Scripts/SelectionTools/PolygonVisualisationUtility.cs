using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using System.Collections.Generic;
using UnityEngine;


namespace Netherlands3D.SelectionTools
{
    public static class PolygonVisualisationUtility
    {
        private static NtsGeometryServices instance;
        private static GeometryFactory geometryFactory;
       

        private static void SetUpInstance()
        {
            if (instance != null)
                return;

            instance = new NtsGeometryServices(
            // default CoordinateSequenceFactory
            CoordinateArraySequenceFactory.Instance,
            // default precision model
            new PrecisionModel(1000d),
            // default SRID
            4326,
            /********************************************************************
             * Note: the following arguments are only valid for NTS >= v2.2
             ********************************************************************/
            // Geometry overlay operation function set to use (Legacy or NG)
            GeometryOverlay.NG,
            // Coordinate equality comparer to use (CoordinateEqualityComparer or PerOrdinateEqualityComparer)
            new CoordinateEqualityComparer());

            geometryFactory = instance.CreateGeometryFactory();
        }

        #region UnityComponents
        //Treat first contour as outer contour, and extra contours as holes
        public static PolygonVisualisation CreateAndReturnPolygonObject(List<List<Vector3>> contours,
            float meshExtrusionHeight,
            bool addMeshColliders,
            bool createInwardMesh = false,
            bool addBottomToMesh = true,
            Material meshMaterial = null,
            Material lineMaterial = null,
            Color lineColor = default,
            Vector2 uvCoordinate = new Vector2(),
            bool receiveShadows = true
        )
        {
            var newPolygonObject = new GameObject();
            newPolygonObject.name = "PolygonVisualisation";
            
            var meshFilter = newPolygonObject.AddComponent<MeshFilter>(); //mesh is created by the PolygonVisualisation script
            var meshRenderer = newPolygonObject.AddComponent<MeshRenderer>();
            meshRenderer.material = meshMaterial;
            meshRenderer.receiveShadows = receiveShadows;

            if (addMeshColliders)
                newPolygonObject.AddComponent<MeshCollider>();

            var polygonVisualisation = newPolygonObject.AddComponent<PolygonVisualisation>();
            polygonVisualisation.Initialize(contours, meshExtrusionHeight, addBottomToMesh, createInwardMesh, lineMaterial, lineColor, uvCoordinate);
            newPolygonObject.transform.Translate(0, meshExtrusionHeight, 0);

            return polygonVisualisation;
        }

        #endregion

        #region PolygonMesh
        /// <summary>
        /// 
        /// </summary>
        /// <param name="contours"></param>
        /// <param name="extrusionHeight"></param>
        /// <param name="addBottom"></param>
        /// <param name="uvCoordinate"></param>
        /// <returns></returns>
        public static Mesh CreatePolygonMesh(List<List<Vector3>> contours, float extrusionHeight, bool addBottom, Vector2 uvCoordinate = new Vector2())
        {
            if (contours.Count == 0)
                return null;

            SetUpInstance();


            // Convert the first ring to the shell
            var shellCoords = ConvertToCoordinateArray(contours[0]);
            var shell = geometryFactory.CreateLinearRing(shellCoords);

            // Convert remaining rings to holes
            LinearRing[] holes = null;
            if (contours.Count > 1)
            {
                holes = new LinearRing[contours.Count - 1];
                for (int i = 1; i < contours.Count; i++)
                {
                    holes[i - 1] = geometryFactory.CreateLinearRing(ConvertToCoordinateArray(contours[i]));
                }
            }

            Polygon polygon = geometryFactory.CreatePolygon(shell, holes);
            return null;

            //var polygon = new Poly2Mesh.Polygon();
            //var outerContour = (List<Vector3>)contours[0];

            //if (outerContour.Count < 3)
            //    return null;

            //polygon.outside = outerContour;

            //for (int i = 0; i < polygon.outside.Count; i++)
            //{
            //    polygon.outside[i] = new Vector3(polygon.outside[i].x, polygon.outside[i].y, polygon.outside[i].z);
            //}

            //if (contours.Count > 1)
            //{
            //    for (int i = 1; i < contours.Count; i++)
            //    {
            //        var holeContour = (List<Vector3>)contours[i];
            //        PolygonCalculator.FixSequentialDoubles(holeContour); //todo: put this function in this class or is it a usefull calculation tool in general?

            //        if (holeContour.Count > 2)
            //        {
            //            polygon.holes.Add(holeContour);
            //        }
            //    }
            //}
            //var newPolygonMesh = Poly2Mesh.CreateMesh(polygon, extrusionHeight, addBottom);
            //if (newPolygonMesh)
            //{
            //    newPolygonMesh.RecalculateNormals();
            //    SetUVCoordinates(newPolygonMesh, uvCoordinate);
            //}
            //return newPolygonMesh;
        }

        private static Coordinate[] ConvertToCoordinateArray(List<Vector3> points)
        {
            var coords = new Coordinate[points.Count + 1];
            for (int i = 0; i < points.Count; i++)
            {
                coords[i] = new Coordinate(points[i].x, points[i].z); // use .y if working in 2D
            }
            coords[points.Count] = coords[0]; // close the ring
            return coords;
        }

        #endregion

        #region PolygonLine
        public static List<LineRenderer> CreateLineRenderers(List<List<Vector3>> polygons, Material lineMaterial, Color lineColor, Transform parent = null)
        {
            var list = new List<LineRenderer>();
            foreach (var contour in polygons)
            {
                list.Add(CreateAndReturnPolygonLine((List<Vector3>)contour, lineMaterial, lineColor, parent)); //todo: require explicit List<List<Vector3>> as argument?
            }
            return list;
        }

        public static LineRenderer CreateAndReturnPolygonLine(List<Vector3> contour, Material lineMaterial, Color lineColor, Transform parent = null)
        {
            var newPolygonObject = new GameObject();
            newPolygonObject.transform.SetParent(parent);
            newPolygonObject.name = "PolygonOutline";
            var lineRenderer = newPolygonObject.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            lineRenderer.loop = true;

            lineRenderer.positionCount = contour.Count;
            lineRenderer.SetPositions(contour.ToArray()); //does not work for some reason

            return lineRenderer;
        }
        #endregion

        public static void SetUVCoordinates(Mesh newPolygonMesh, Vector2 uvCoordinate)
        {
            var uvs = new Vector2[newPolygonMesh.vertexCount];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = uvCoordinate;
            }
            newPolygonMesh.uv = uvs;
        }
    }
}
