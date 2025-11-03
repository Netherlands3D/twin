using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.TileSelectors;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using RSG;
using UnityEngine;
using UnityEngine.Pool;

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
        private ObjectPool<TileBehaviour> tilePool;
        private readonly Dictionary<Tile, TileBehaviour> tileBehaviours = new ();

        private void Awake()
        {
            FrustumChecker ??= new FrustumChecker(viewPortCamera ? viewPortCamera : Camera.main);
            TileSelector ??= new TilesInView(projector);
            TransitionPlanner ??= new TransitionPlanner();
            ChangeScheduler ??= new ImmediateChangeScheduler();
            
            tilePool = new ObjectPool<TileBehaviour>(
                CreateTile,
                OnGetTileFromPool, 
                OnReleaseTileToPool,
                OnDestroyTile
            );
            
            var tileBuilder = TileBuilder.Explicit(
                BoundingVolume.RegionBoundingVolume(-5d, -5d, 5d, 5d, 0d, 0d),
                0
            );
            tileSetConfiguration = new TileSet(tileBuilder.Build());
        }

        private TileBehaviour CreateTile()
        {
            return Instantiate(tilePrefab, transform);
        }

        private void OnGetTileFromPool(TileBehaviour tile)
        {
            tile.gameObject.SetActive(true);
        }

        private void OnReleaseTileToPool(TileBehaviour tile)
        {
            tile.gameObject.SetActive(false);
        }

        private void OnDestroyTile(TileBehaviour tile)
        {
            Destroy(tile.gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            
            UpdateTriggered.AddListener(OnUpdateTriggered);
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

            UpdateTriggered.RemoveListener(OnUpdateTriggered);
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
        
        protected virtual void OnUpdateTriggered(ITileMapper tileMapper)
        {
            if (!FrustumChecker.IsFrustumChanged()) return;

            FrustumChanged.Invoke(this, FrustumChecker.Planes);
        }

        protected virtual void OnFrustumChanged(ITileMapper tileMapper, Plane[] planes)
        {
            var stagedTiles = TileSelector.Select(tileSetConfiguration, planes);

            TilesSelected.Invoke(this, stagedTiles);
        }

        protected virtual void OnTilesSelected(ITileMapper tileMapper, Tiles tiles)
        {
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

        private Promise OnChange(Change change)
        {
            // TODO: This logic does not belong here, but the Change system needs to be revised. First I need the general
            // flow to work 
            switch (change.Type)
            {
                case TypeOfChange.Add: 
                    tilesInView.Add(change.Tile);
                    var tileBehaviour = tilePool.Get();
                    if (tileBehaviour)
                    {
                        tileBehaviours[change.Tile] = tileBehaviour;
                        tileBehaviour.Tile = change.Tile;
                    }

                    break;
                case TypeOfChange.Remove: 
                    tilesInView.Remove(change.Tile);
                    if (tileBehaviours.TryGetValue(change.Tile, out tileBehaviour))
                    {
                        tilePool.Release(tileBehaviour);
                        tileBehaviours.Remove(change.Tile);
                    }
                    break;
            }

            ChangeApply.Invoke(this, change);
            
            return Promise.Resolved() as Promise;
        }

        protected virtual void OnChangeScheduleRequested(ITileMapper tileMapper, Change change)
        {
            // Schedule the change
            ChangeScheduler.Schedule(change);

            // Assign an action to perform on this change when it executes
            change.UsingAction(OnChange);
        }

        protected virtual void OnChangesPlanned(ITileMapper tileMapper, List<Change> changes)
        {
            StartCoroutine(ChangeScheduler.Apply());
        }

        protected virtual void OnChangeApply(ITileMapper tileMapper, Change change)
        {
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