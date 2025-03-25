using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileSetRenderers
{
    public class ComposableTileSetRenderer : TileSetRenderer
    {
        public Tiles TilesInView { get; } = new();

        public TileSelector TileSelector;
        public ChangeScheduler ChangeScheduler;
        public TileRenderer TileRenderer;

        private Camera mainCamera;
        private Plane[] frustumPlanes = new Plane[6];
        private Plane[] previousFrustumPlanes = new Plane[6];

        private void Start()
        {
            mainCamera = Camera.main;
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);
        }

        public override void Stage(TileSet tileSet)
        {
            // Move this so that you can 'force' invoke a computation because outside forces influence the selection
            // or make this another adapter
            if (FrustumChanged() == false) return;

            var stagedTiles = TileSelector.Select(tileSet, frustumPlanes);
            
            // Crude diff - if a staged tile is not in the active tiles array, add it
            foreach (var tile in stagedTiles)
            {
                if (TilesInView.Contains(tile)) continue;

                this.ChangeScheduler.Schedule(this, Change.Add(tile));
            }

            // Crude diff - if an active tile is not in the staged array, remove it
            foreach (var tile in TilesInView)
            {
                if (stagedTiles.Contains(tile)) continue;

                this.ChangeScheduler.Schedule(this, Change.Remove(tile));
            }
        }

        public override void Render(TileSet tileSet)
        {
            StartCoroutine(ChangeScheduler.Apply());
        }
        
        private bool FrustumChanged()
        {
            // Every time we repopulate the frustumPlanes array - saving allocations
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);

            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                if (frustumPlanes[i].Equals(previousFrustumPlanes[i])) continue;
                
                // Here we use the CalculateFrustumPlanes version but we want new allocation so that the check above
                // actually triggers - otherwise it will always return true
                previousFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

                return true;
            }

            return false;
        }
    }
}