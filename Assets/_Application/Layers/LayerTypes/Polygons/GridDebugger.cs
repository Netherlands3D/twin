using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class GridDebugger : MonoBehaviour
    {
        private ObjectScatterLayerGameObject layerGameObject;
        private CompoundPolygon poly;
        private Vector2[] grid;
        private int startIndex = 0;
        private float angle;
        private Bounds gridBounds;
        private float cellSize = 1000000;
        
        private void Start()
        {
            layerGameObject = GetComponent<ObjectScatterLayerGameObject>();
            UpdateGrid();
            layerGameObject.polygonLayer.polygonChanged.AddListener(UpdateGrid);
            var scatterSettings = layerGameObject.PropertyData as ScatterGenerationSettingsPropertyData;
            scatterSettings.ScatterSettingsChanged.AddListener(UpdateGrid);
        }

        private void UpdateGrid()
        {
            poly = layerGameObject.polygonLayer.Polygon;
            var scatterSettings = layerGameObject.PropertyData as ScatterGenerationSettingsPropertyData;
            var angle = scatterSettings.Angle;
            
            var densityPerSquareUnit = scatterSettings.Density / 10000f; //in de UI is het het bomen per hectare, in de functie is het punten per m2
            cellSize = 1f / Mathf.Sqrt(densityPerSquareUnit);
            grid = CompoundPolygon.GenerateGridPoints(poly.Bounds, cellSize, angle, out gridBounds);
        }

        private void Update()
        {
            if (Keyboard.current.kKey.wasPressedThisFrame)
            {
                print(startIndex);

                // angle = GetAngle();
                grid = CompoundPolygon.GenerateGridPoints(poly.Bounds, cellSize, angle, out var gridBounds); // we need the gridBounds out variable. 
            }
        }

        private float GetAngle()
        {
            var verts = poly.SolidPolygon;
            var p0 = verts[startIndex % verts.Length];
            var p1 = verts[++startIndex % verts.Length];

            var dir = p1 - p0;

            var angle = Vector2.Angle(Vector2.up, dir);
            print(p0 + "\t" + p1 + "\t" + angle);
            return angle;
        }

        private void OnDrawGizmos()
        {
            var width = Mathf.CeilToInt(1f * (gridBounds.size.x + 2 * cellSize)); //add 2*maxRandomOffset to include the max scatter range on both sides
            var height = Mathf.CeilToInt(1f * (gridBounds.size.z + 2 * cellSize));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(gridBounds.center, new Vector3(width, 0, height));

            //draw polygon
            Gizmos.color = Color.cyan;
            for (var i = 0; i < poly.SolidPolygon.Length; i++)
            {
                var vert = poly.SolidPolygon[i];
                var vert3D = new Vector3(vert.x, 0, vert.y);

                var nextVert = poly.SolidPolygon[(i + 1) % poly.SolidPolygon.Length];
                var nextVert3D = new Vector3(nextVert.x, 0, nextVert.y);

                Gizmos.DrawSphere(vert3D, 0.5f);
                Gizmos.DrawLine(vert3D, nextVert3D);
            }

            Gizmos.DrawWireCube(poly.Bounds.center, poly.Bounds.size);

            //draw grid
            Gizmos.color = Color.magenta;
            var grid3D = grid.ToVector3List();
            foreach (var p in grid3D)
            {
                Gizmos.DrawSphere(p, 0.2f);
            }

            Gizmos.DrawWireCube(gridBounds.center, gridBounds.size);

            //draw centerline
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(poly.Bounds.center, 0.2f);

            var center = poly.Bounds.center;
            Vector3 direction = Quaternion.Euler(0, -angle, 0) * Vector3.forward; // Calculate the direction of the line based on angle

            Vector3 startPoint = center - direction * (poly.Bounds.extents.magnitude / 2); // Calculate the starting point of the line
            Vector3 endPoint = center + direction * (poly.Bounds.extents.magnitude / 2); // Calculate the end point of the line

            Gizmos.DrawLine(startPoint, endPoint);
        }
    }
}