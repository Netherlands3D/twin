using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.TileSelectors;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class TileMapper : BaseTileMapper
    {
        private TileSet tileSetConfiguration;
        public Tile[] TilesInView => tilesInView.ToArray();

        public FrustumChecker FrustumChecker { get; set; }
        public ITileSelector TileSelector { get; set; }
        public ITransitionPlanner TransitionPlanner { get; set; }
        public IChangeScheduler ChangeScheduler { get; set; }

        [SerializeField] private Camera viewPortCamera;
        [SerializeField] private BaseProjector projector;
        [SerializeField] private TileBehaviour tilePrefab;

        private Tiles tilesInView = new();
        private Tiles lastStagedTiles = new Tiles();

        private void Awake()
        {
            FrustumChecker ??= new FrustumChecker(viewPortCamera ? viewPortCamera : Camera.main);
            TileSelector ??= new TilesInView(projector);
            TransitionPlanner ??= new TransitionPlanner();
            ChangeScheduler ??= new ImmediateChangeScheduler();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            FrustumChanged.AddListener(OnFrustumChanged);
            TilesSelected.AddListener(OnTilesSelected);
            TransitionCreated.AddListener(OnTransition);
            ChangeScheduleRequested.AddListener(OnChangeScheduleRequested);
            ChangesScheduled.AddListener(OnChangesPlanned);
            ChangeApply.AddListener(OnChangeApply);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            FrustumChanged.RemoveListener(OnFrustumChanged);
            TilesSelected.RemoveListener(OnTilesSelected);
            TransitionCreated.RemoveListener(OnTransition);
            ChangeScheduleRequested.RemoveListener(OnChangeScheduleRequested);
            ChangesScheduled.RemoveListener(OnChangesPlanned);
            ChangeApply.RemoveListener(OnChangeApply);
        }

        public override void Map()
        {
            this.UpdateTriggered.Invoke(this);
        }

        protected virtual void OnFrustumChanged(ITileMapper tileMapper, Plane[] planes)
        {
            var stagedTiles = TileSelector.Select(tileSetConfiguration, planes);

            TilesSelected.Invoke(this, stagedTiles);
        }

        protected virtual void OnTilesSelected(ITileMapper tileMapper, Tiles tiles)
        {
            lastStagedTiles = tiles;
            var transition = TransitionPlanner.CreateTransition(tilesInView, tiles);

            TransitionCreated.Invoke(this, transition);
        }

        protected virtual void OnTransition(ITileMapper tileMapper, List<Change> transition)
        {
            foreach (var change in transition)
            {
                ChangeScheduleRequested.Invoke(this, change);
            }

            ChangesScheduled.Invoke(this, transition);
        }

        protected virtual void OnChangeScheduleRequested(ITileMapper tileMapper, Change change)
        {
            ChangeScheduler.Schedule(this, change);
        }

        protected virtual void OnChangesPlanned(ITileMapper tileMapper, List<Change> changes)
        {
            StartCoroutine(ChangeScheduler.Apply());
        }

        protected virtual void OnChangeApply(ITileMapper tileMapper, Change change)
        {
            // TODO: This logic does not belong here, but the Change system needs to be revised. First I need the general
            // flow to work 
            switch (change.Type)
            {
                case TypeOfChange.Add: tilesInView.Add(change.Tile); break;
                case TypeOfChange.Remove: tilesInView.Remove(change.Tile); break;
            }
            
            ChangeApply.Invoke(this, change);
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            foreach (var tile in TilesInView)
            {
                DrawTileGizmo(tile, Color.blue, Color.green);
            }
        }

        protected virtual void DrawTileGizmo(Tile tile, Color tileColor, Color tileContentColor, float sizeFactor = 1f)
        {
            Gizmos.color = tileColor;
            Gizmos.DrawWireCube(tile.BoundingVolume.Center.ToVector3(), tile.BoundingVolume.Size.ToVector3() * sizeFactor);
            Gizmos.color = tileContentColor;
            foreach (var tileContent in tile.TileContents)
            {
                // Draw content boxes at 99% the size to see them inside the main tile gizmo
                Gizmos.DrawWireCube(
                    tileContent.BoundingVolume.Center.ToVector3(), 
                    tileContent.BoundingVolume.Size.ToVector3() * 0.99f * sizeFactor
                );
            }
        }
    }
}