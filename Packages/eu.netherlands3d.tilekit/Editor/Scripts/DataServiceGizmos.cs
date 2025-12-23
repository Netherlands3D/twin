using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.MemoryManagement;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using CoordinateSystem = Netherlands3D.Coordinates.CoordinateSystem;
using Vector3 = UnityEngine.Vector3;

namespace Netherlands3D.Tilekit.Editor
{
    internal static class DataServiceGizmos
    {
        private const float LabelYOffset = 0.15f;
        private const float MinLabelPixelSize = 50f; // tweak to taste

        [DrawGizmo(GizmoType.Selected | GizmoType.Active, typeof(DataService))]
        private static void DrawForDataService(DataService service, GizmoType gizmoType)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(service.transform.position, 20f);

            if (!Application.isPlaying) return;

            if (service.DataSet == null || !service.DataSet.IsInitialized) return;

            var tileSet = service.DataSet.TileSet;
            if (tileSet == null) return;

            if (!tileSet.Warm.IsCreated || !tileSet.Hot.IsCreated) return;

            // Cold / all tiles (labels culled)
            DrawTileGizmo(tileSet, tileSet.Root, new Color(0, 0, 0), height: 0, recursive: true, alwaysLabel: false);

            // Warm/Hot (labels NOT culled)
            DrawGizmosForTiles(tileSet, tileSet.Warm, Color.yellow, alwaysLabel: true);
            DrawGizmosForTiles(tileSet, tileSet.Hot, Color.red, alwaysLabel: true);
        }

        private static void DrawGizmosForTiles(TileSet tileSet, NativeList<int> tiles, Color color, int height = 0, bool alwaysLabel = false)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                DrawTileGizmo(tileSet, tileSet.GetTile(tiles[i]), color, height + 1, recursive: false, alwaysLabel: alwaysLabel);
            }
        }

        private static void DrawGizmosForTiles(TileSet tileSet, Tile tile, BufferBlock<int> tiles, Color color, int height, bool alwaysLabel)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                DrawTileGizmo(tileSet, tileSet.GetTile(tiles[i]), color, height + 1, recursive: true, alwaysLabel: alwaysLabel);
            }
        }

        private static void DrawTileGizmo(TileSet tileSet, Tile tile, Color color, int height = 0, bool recursive = true, bool alwaysLabel = false)
        {
            var bounds = tile.BoundingVolume
                .ToBounds()
                .ToLocalCoordinateSystem(CoordinateSystem.RD);

            var pos = bounds.center + Vector3.up * LabelYOffset;

            Gizmos.color = color;

            // culling ONLY for non-warm/non-hot tiles
            if (alwaysLabel || IsBigEnoughOnScreen(bounds, MinLabelPixelSize))
            {
                Gizmos.DrawWireCube(pos, bounds.size);
                var style = CreateLabelStyle(color, FontSizeFromHeight(height));
                Handles.Label(pos, tile.Index.ToString(), style);
            }

            if (recursive)
            {
                DrawGizmosForTiles(tileSet, tile, tile.Children(), color, height, alwaysLabel);
            }
        }

        private static bool IsBigEnoughOnScreen(Bounds bounds, float minPixels)
        {
            // Project a few corners and estimate screen-space width/height (in GUI pixels)
            var ext = bounds.extents;
            var c = bounds.center;

            // Use X/Z footprint + a little Y so the label plane isn't exactly on the surface
            var p0 = new Vector3(c.x - ext.x, c.y, c.z - ext.z);
            var p1 = new Vector3(c.x + ext.x, c.y, c.z - ext.z);
            var p2 = new Vector3(c.x - ext.x, c.y, c.z + ext.z);
            var p3 = new Vector3(c.x + ext.x, c.y, c.z + ext.z);

            var g0 = HandleUtility.WorldToGUIPoint(p0);
            var g1 = HandleUtility.WorldToGUIPoint(p1);
            var g2 = HandleUtility.WorldToGUIPoint(p2);
            var g3 = HandleUtility.WorldToGUIPoint(p3);

            float minX = Mathf.Min(g0.x, g1.x, g2.x, g3.x);
            float maxX = Mathf.Max(g0.x, g1.x, g2.x, g3.x);
            float minY = Mathf.Min(g0.y, g1.y, g2.y, g3.y);
            float maxY = Mathf.Max(g0.y, g1.y, g2.y, g3.y);

            float w = maxX - minX;
            float h = maxY - minY;

            return w >= minPixels && h >= minPixels;
        }

        private static int FontSizeFromHeight(int height)
        {
            return Mathf.Clamp(18 - height * 2, 8, 18);
        }

        private static GUIStyle CreateLabelStyle(Color color, int fontSize)
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }
            };
        }
    }
}
