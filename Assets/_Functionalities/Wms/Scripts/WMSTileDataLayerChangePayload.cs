using Netherlands3D.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Functionalities.Wms
{
    /// <summary>
    /// Payload when requesting a WMS tile to track which tile needs to be altered when the
    /// request comes back.
    /// </summary>
    /// <param name="TileChange">Tile change for which the request is done</param>
    /// <param name="Url">Url for which the request is done, could be pulled from the request but passed to be sure</param>
    public record WMSTileDataLayerChangePayload(TileChange TileChange, string Url)
    {
        public TileChange TileChange { get; } = TileChange;
        public string Url  { get; } = Url;
        public Vector2Int TileKey => new(TileChange.X, TileChange.Y);
    }
}