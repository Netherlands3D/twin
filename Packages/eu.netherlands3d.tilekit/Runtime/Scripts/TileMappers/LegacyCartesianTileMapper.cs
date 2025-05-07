using System;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileMappers
{
    // TODO: Move this outside of the tilekit package as soon as this becomes stable - this creates an unwanted
    // dependency on the legacy cartesian tiles. This renderer should move to the application space as this is a
    // temporary one
    public class LegacyCartesianTileMapper : MonoBehaviour, ITileMapper
    {
        [SerializeField] private Layer layerPrefab;
        
        private void Start()
        {
            var tileHandler = FindAnyObjectByType<TileHandler>();
            var layer = Instantiate(layerPrefab, tileHandler.transform);

            tileHandler.AddLayer(layer);
        }

        public void FromTileSet(TileSet tileSet)
        {
            // Not needed - ignore
        }

        public void Map()
        {
            // not needed - ignore
        }
    }
}