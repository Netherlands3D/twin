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
        // Selected = when the GameObject is selected in the hierarchy
        [DrawGizmo(GizmoType.Selected | GizmoType.Active, typeof(DataService))]
        private static void DrawForDataService(DataService service, GizmoType gizmoType)
        {
            // TODO: Remove these two lines - these are for debugging purposes only
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(service.transform.position, 20f);

            if (!Application.isPlaying) return;

            // If you already cache this on DataService, you can expose it via a property.
            if (service.DataSet == null || !service.DataSet.IsInitialized) return;

            var tileSet = service.DataSet.TileSet;
            if (tileSet == null) return;

            if (!tileSet.Warm.IsCreated || !tileSet.Hot.IsCreated) return;

            DrawTileGizmo(tileSet, tileSet.Root, new Color(0,0,0));
            DrawGizmosForTiles(tileSet, tileSet.Warm, Color.yellow);
            DrawGizmosForTiles(tileSet, tileSet.Hot, Color.red);
        }

        private static void DrawGizmosForTiles(TileSet tileSet, NativeList<int> tiles, Color color, int height = 0)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                DrawTileGizmo(tileSet, tileSet.GetTile(tiles[i]), color, height + 1, false);
            }
        }

        private static void DrawGizmosForTiles(TileSet tileSet, Tile tile, BufferBlock<int> tiles, Color color, int height)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                DrawTileGizmo(tileSet, tileSet.GetTile(tiles[i]), color, height + 1);
            }
        }

        private static void DrawTileGizmo(TileSet tileSet, Tile tile, Color color, int height = 0, bool recursive = true)
        {
            var bounds = tile.BoundingVolume
                .ToBounds()
                .ToLocalCoordinateSystem(CoordinateSystem.RD);

            Gizmos.color = color;
            Gizmos.DrawWireCube(bounds.center + Vector3.up * 0.1f, bounds.size);

            if (recursive) DrawGizmosForTiles(tileSet, tile, tile.Children(), color, height);
        }
    }
}