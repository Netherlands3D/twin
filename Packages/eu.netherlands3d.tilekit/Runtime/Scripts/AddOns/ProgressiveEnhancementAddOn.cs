using System;
using Netherlands3D.Tilekit.Changes;
using UnityEngine;

namespace Netherlands3D.Tilekit.AddOns
{
    [RequireComponent(typeof(BaseTileMapper))]
    public class ProgressiveEnhancementAddOn : MonoBehaviour
    {
        private BaseTileMapper tileMapper;

        private void Awake()
        {
            tileMapper = GetComponent<BaseTileMapper>();
        }

        private void OnEnable()
        {
            tileMapper.TileSetLoaded.AddListener(OnTileSetLoaded);
        }

        private void OnDisable()
        {
            tileMapper.TileSetLoaded.RemoveListener(OnTileSetLoaded);
        }

        private void OnTileSetLoaded(ITileMapper tileMapper, TileSet tileSet)
        {
            if (tileMapper is not TileMapper mapper) return;

            // Simplest form of progressive enhancement: load a base tile first
            // TODO: Create an example tileset where we load a 3d mesh, but the top level tile is a large WMS tile
            var change = new Change(TypeOfChange.Add, tileSet.Root);
            change.CannotBeCancelled();
            
            mapper.ChangeScheduler.Schedule(change);
        }
    }
}