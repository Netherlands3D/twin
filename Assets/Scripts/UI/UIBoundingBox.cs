using UnityEngine;
using UnityEngine.UI;


namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class UIBoundingBox : Graphic
    {
        public Vector2[] points;  // Array of points to draw lines between
        public float lineWidth = 2.0f; // Width of the line

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (points == null || points.Length < 2) return;

            // Loop through each pair of points to create line segments
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[i + 1];
                AddLineSegment(vh, start, end);
            }
        }

        void AddLineSegment(VertexHelper vh, Vector2 start, Vector2 end)
        {
            // Calculate the direction and perpendicular vector for line width
            Vector2 direction = end - start;
            direction.Normalize();
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (lineWidth / 2);

            // Define the four corners of the quad
            Vector2 p1 = start + perpendicular; // Top left
            Vector2 p2 = start - perpendicular; // Bottom left
            Vector2 p3 = end - perpendicular;   // Bottom right
            Vector2 p4 = end + perpendicular;   // Top right

            // Add the vertices for the quad
            vh.AddVert(p1, color, new Vector2(0, 0));
            vh.AddVert(p2, color, new Vector2(0, 1));
            vh.AddVert(p3, color, new Vector2(1, 1));
            vh.AddVert(p4, color, new Vector2(1, 0));

            // Calculate the starting vertex index for this quad
            int startIndex = vh.currentVertCount - 4;

            // Add the two triangles that make up the quad
            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2); // First triangle
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3); // Second triangle
        }
    }
}