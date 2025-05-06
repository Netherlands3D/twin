using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using KindMen.Uxios;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.ExtensionMethods;
using Netherlands3D.Tilekit.TileSelectors;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Tilekit.TilesTransitionPlanners;

namespace Netherlands3D.Tilekit.V2
{
    // Interface defining all tile-related events
    public interface ITilekitEvents
    {
        UnityEvent<ITileMapper> UpdateTriggered { get; }
        UnityEvent<ITileMapper, TileSet> TileSetLoaded { get; }
        UnityEvent<ITileMapper> FrustumChanged { get; }
        UnityEvent<ITileMapper, Tiles> TilesSelected { get; }
        UnityEvent<ITileMapper, List<Change>> TransitionCreated { get; }
        UnityEvent<ITileMapper, Change> ChangeScheduleRequested { get; }
        UnityEvent<ITileMapper, List<Change>> ChangesScheduled { get; }
        UnityEvent<ITileMapper, Change> ChangeApply { get; }
        UnityEvent<ITileMapper, TileBehaviour> TileSpawned { get; }
    }

    public interface ITileMapper
    {
        void FromTileSet(TileSet tileSet);
        void Invoke();
    }

    // Abstract base mapper providing factory methods, event management and pipeline invocation
    public abstract class BaseTileMapper : MonoBehaviour, ITilekitEvents, ITileMapper
    {
        // ITilekitEvents implementation
        public UnityEvent<ITileMapper> UpdateTriggered { get; } = new();
        public UnityEvent<ITileMapper, TileSet> TileSetLoaded { get; } = new();
        public UnityEvent<ITileMapper> FrustumChanged { get; } = new();
        public UnityEvent<ITileMapper, Tiles> TilesSelected { get; } = new();
        public UnityEvent<ITileMapper, List<Change>> TransitionCreated { get; } = new();
        public UnityEvent<ITileMapper, Change> ChangeScheduleRequested { get; } = new();
        public UnityEvent<ITileMapper, List<Change>> ChangesScheduled { get; } = new();
        public UnityEvent<ITileMapper, Change> ChangeApply { get; } = new();
        public UnityEvent<ITileMapper, TileBehaviour> TileSpawned { get; } = new();

        // Creation methods
        public TileBuilder QuadTree(BoundingVolume volume, TemplatedUri uri) => TileBuilder.QuadTree(volume, uri);
        public TileBuilder Octree(BoundingVolume volume, TemplatedUri uri) => TileBuilder.Octree(volume, uri);
        public TileBuilder Grid(BoundingVolume volume, TemplatedUri uri) => TileBuilder.Grid(volume, uri);
        public TileBuilder Explicit(BoundingVolume volume) => TileBuilder.Explicit(volume);

        public void FromTileSet(TileSet tileSet)
        {
            // TODO: Initialize builder from existing TileSet
            throw new NotImplementedException();
        }

        private void OnEnable()
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;
            
            AddListeners(eventSystem);
        }

        private void OnDisable()
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;

            RemoveListeners(eventSystem);
        }

        // Event subscription
        public virtual void AddListeners(ITilekitEvents events)
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;
            
            UpdateTriggered.AddListener(events.UpdateTriggered.Invoke);
            TileSetLoaded.AddListener(events.TileSetLoaded.Invoke);
            FrustumChanged.AddListener(events.FrustumChanged.Invoke);
            TilesSelected.AddListener(events.TilesSelected.Invoke);
            TransitionCreated.AddListener(events.TransitionCreated.Invoke);
            ChangeScheduleRequested.AddListener(events.ChangeScheduleRequested.Invoke);
            ChangesScheduled.AddListener(events.ChangesScheduled.Invoke);
            ChangeApply.AddListener(events.ChangeApply.Invoke);
            TileSpawned.AddListener(events.TileSpawned.Invoke);
        }

        public virtual void RemoveListeners(ITilekitEvents events)
        {
            var eventSystem = TilekitEventSystem.current;
            if (!eventSystem) return;

            UpdateTriggered.RemoveListener(events.UpdateTriggered.Invoke);
            TileSetLoaded.RemoveListener(events.TileSetLoaded.Invoke);
            FrustumChanged.RemoveListener(events.FrustumChanged.Invoke);
            TilesSelected.RemoveListener(events.TilesSelected.Invoke);
            TransitionCreated.RemoveListener(events.TransitionCreated.Invoke);
            ChangeScheduleRequested.RemoveListener(events.ChangeScheduleRequested.Invoke);
            ChangesScheduled.RemoveListener(events.ChangesScheduled.Invoke);
            ChangeApply.RemoveListener(events.ChangeApply.Invoke);
            TileSpawned.RemoveListener(events.TileSpawned.Invoke);
        }

        public abstract void Invoke();
    }

    // Concrete mapper that uses selector, planner and scheduler services
    public class TileMapper : BaseTileMapper
    {
        private TileSet tileSetConfiguration;
        public Tile[] TilesInView => tilesInView.ToArray();

        // Services (can inject via property or DI container)
        public ITileSelector TileSelector { get; set; } = new TilesInView();
        public ITransitionPlanner TransitionPlanner { get; set; } = new TransitionPlanner();
        public IChangeScheduler ChangeScheduler { get; set; }

        // Serialized prefab for spawning tiles
        [SerializeField] private TileBehaviour tilePrefab;
        private Tiles tilesInView = new();
        private Tiles lastStagedTiles = new Tiles();

        public override void Invoke()
        {
            
        }

        protected void OnFrustumChanged(Plane[] planes)
        {
            var stagedTiles = TileSelector.Select(tileSetConfiguration, planes);

            TilesSelected.Invoke(this, stagedTiles);
        }

        protected void OnTilesSelected(Tiles tiles)
        {
            lastStagedTiles = tiles;
            var transition = TransitionPlanner.CreateTransition(tilesInView, tiles);

            TransitionCreated.Invoke(this, transition);
        }

        protected void OnTransition(List<Change> transition)
        {
            foreach (var change in transition)
            {
                ChangeScheduleRequested.Invoke(this, change);
            }

            ChangesScheduled.Invoke(this, transition);
        }

        protected void OnChangeScheduleRequested(Change change)
        {
            ChangeScheduler.Schedule(this.TileSetProvider, change);
        }

        protected void OnChangesPlanned(List<Change> changes)
        {
            StartCoroutine(ChangeScheduler.Apply());
        }

        protected void OnChangeApply(Change change)
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
        
        protected void OnDrawGizmosSelected()
        {
            foreach (var tile in TilesInView)
            {
                DrawTileGizmo(tile, Color.blue, Color.green);
            }
        }

        protected void DrawTileGizmo(Tile tile, Color tileColor, Color tileContentColor, float sizeFactor = 1f)
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

    // Registry of content-type to prefab mappings
    [CreateAssetMenu(menuName = "Tilekit/TileContentRegistry")]
    public class TileContentRegistry : ScriptableObject
    {
        public Dictionary<ContentType, TileContentPrefab> TileContentPrefabs =
            new Dictionary<ContentType, TileContentPrefab>();
    }

    // Fluent builder for constructing Tile hierarchies
    public class TileBuilder
    {
        private readonly List<TileBuilder> Children = new List<TileBuilder>();
        private string identifier;
        private TileBuilder parent;

        // Static factory methods (root builders)
        public static TileBuilder QuadTree(BoundingVolume volume, TemplatedUri uri)
        {
            // TODO: init quadtree
            return new TileBuilder();
        }

        public static TileBuilder Octree(BoundingVolume volume, TemplatedUri uri)
        {
            // TODO: init octree
            return new TileBuilder();
        }

        public static TileBuilder Grid(BoundingVolume volume, TemplatedUri uri)
        {
            // TODO: init grid
            return new TileBuilder();
        }

        public static TileBuilder Explicit(BoundingVolume volume)
        {
            // TODO: init explicit structure
            return new TileBuilder();
        }

        // Builder chaining
        public TileBuilder Identifier(string identifier)
        {
            this.identifier = identifier;
            return this;
        }

        public TileBuilder Parent(TileBuilder tile)
        {
            this.parent = tile;
            return this;
        }

        public TileBuilder AddChild()
        {
            var child = new TileBuilder { parent = this };
            Children.Add(child);
            return child;
        }

        public TileContent AddContent()
        {
            // TODO: configure custom content
            throw new NotImplementedException();
        }

        public TileContent AddContent(TileSet tileSet)
        {
            // TODO: link external TileSet
            throw new NotImplementedException();
        }

        public TileContent AddContent(ContentType contentType, Uri uri)
        {
            // TODO: configure remote content
            throw new NotImplementedException();
        }

        public Tile Build()
        {
            // TODO: create Tile and attach children
            throw new NotImplementedException();
        }
    }

    // Singleton event dispatcher implementing ITilekitEvents
    public class TilekitEventSystem : MonoBehaviour, ITilekitEvents
    {
        [CanBeNull]
        public static TilekitEventSystem current { get; set; }

        // Events
        public UnityEvent<ITileMapper> UpdateTriggered { get; private set; } = new ();
        public UnityEvent<ITileMapper, TileSet> TileSetLoaded { get; private set; } = new ();
        public UnityEvent<ITileMapper> FrustumChanged { get; private set; } = new ();
        public UnityEvent<ITileMapper, Tiles> TilesSelected { get; private set; } = new ();
        public UnityEvent<ITileMapper, List<Change>> TransitionCreated { get; private set; } = new ();
        public UnityEvent<ITileMapper, Change> ChangeScheduleRequested { get; private set; } = new ();
        public UnityEvent<ITileMapper, List<Change>> ChangesScheduled { get; private set; } = new ();
        public UnityEvent<ITileMapper, Change> ChangeApply { get; private set; } = new ();
        public UnityEvent<ITileMapper, TileBehaviour> TileSpawned { get; private set; } = new ();

        private void Awake()
        {
            if (current == null)
            {
                current = this;
            }
            else
            {
                Destroy(this);
            }
        }
    }

    // Component that links Tile data to a GameObject
    public class TileBehaviour : MonoBehaviour
    {
        public Tile Tile { get; set; }
        public TileContentRegistry TileContentRegistry { get; set; }
    }

    // Placeholder types (to be replaced with real implementations)
    public class TileContent
    {
    }

    public class ContentType
    {
    }

    public class TileContentPrefab
    {
    }
}