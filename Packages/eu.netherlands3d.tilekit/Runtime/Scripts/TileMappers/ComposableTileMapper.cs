using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit.TileMappers
{
    public class ComposableTileMapper : TileMapper
    {
        public Tiles TilesInView { get; } = new();

        [SerializeField] private TileSetFactory TileSetFactory;
        public TileSelector TileSelector;
        public ChangeScheduler ChangeScheduler;
        public TileRenderer TileRenderer;
        public TilesTransitionPlanner TilesTransitionPlanner;

        private Camera mainCamera;
        private Plane[] frustumPlanes = new Plane[6];
        private Plane[] previousFrustumPlanes = new Plane[6];
        

        private void Start()
        {
            mainCamera = Camera.main;
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);
            
            // If a factory was provided - load the tileset from there. Otherwise, depend on a custom controller
            // that should call the Load() method first.
            if (TileSetFactory)
            {
                Load(TileSetFactory.CreateTileSet());
            }
        }

        public override void Stage()
        {
            if (tileSet == null)
            {
                Debug.LogError("Unable to stage tiles from a tileSet, none was loaded");
                return;
            }

            // Move this so that you can 'force' invoke a computation because outside forces influence the selection
            // or make this another adapter
            // TODO: Should this be part of the TileSelector?
            if (FrustumChanged() == false) return;

            var stagedTiles = TileSelector.Select(tileSet.Value, frustumPlanes);
            var transition = TilesTransitionPlanner.CreateTransition(TilesInView, stagedTiles);
            foreach (var change in transition)
            {
                this.ChangeScheduler.Schedule(this, change);
            }
        }

        public override void Map()
        {
            if (tileSet == null)
            {
                Debug.LogError("Unable to render tileSet, none was loaded");
                return;
            }

            // TODO: Won't this potentially start the change scheduler multiple times?
            StartCoroutine(ChangeScheduler.Apply());
        }
        
        // TODO: Move this to another location - this is reusable and not the responsibility of this class
        // Move this to the TileSelector? Since this may be specific to TilesInView?
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