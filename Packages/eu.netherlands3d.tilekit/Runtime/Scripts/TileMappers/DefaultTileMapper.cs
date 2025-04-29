using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.Events;
using RSG;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit.TileMappers
{
    public class DefaultTileMapper : BaseEventBasedTileMapper
    {
        [SerializeField] private BaseTileSetFactory tileSetFactory;

        [Header("Components")]
        [Tooltip("Determines which tiles are selected for visualisation")]
        [SerializeField] private BaseTileSelector tileSelector;
        [Tooltip("How should a strategy be planned to transition from the previous tiles in view to the new tiles")]
        [SerializeField] private BaseTransitionPlanner transitionPlanner;
        [Tooltip("How will changes be scheduled and executed")]
        [SerializeField] private BaseChangeScheduler changeScheduler;

        private List<Tile> TilesInView { get; } = new();

        protected override IEnumerator Start()
        {
            yield return base.Start();

            if (!tileSetFactory) yield break;

            // Wait a frame to give other mono behaviours a chance to register on the event channel in their
            // start methods before kicking off the chain when a tileSetFactory was provided.
            yield return null;

            Load(tileSetFactory.CreateTileSet());
        }

        protected override void OnFrustumChanged(EventSource eventSource, Plane[] planes)
        {
            var stagedTiles = tileSelector.Select(tileSet.Value, planes);

            EventChannel.RaiseTilesSelected(eventSource, stagedTiles);
        }

        protected override void OnTilesSelected(EventSource eventSource, Tiles tiles)
        {
            var transition = transitionPlanner.CreateTransition(TilesInView, tiles);

            EventChannel.RaiseTransitionCreated(eventSource, transition);
        }

        protected override void OnTransition(EventSource eventSource, List<Change> transition)
        {
            foreach (var change in transition)
            {
                EventChannel.RaiseChangeScheduleRequested(eventSource, change);
            }

            EventChannel.RaiseChangesScheduled(eventSource, transition);
        }

        protected override void OnChangeScheduleRequested(EventSource eventSource, Change change)
        {
            changeScheduler.Schedule(this, change);
        }

        protected override void OnChangesPlanned(EventSource eventSource, List<Change> changes)
        {
            StartCoroutine(changeScheduler.Apply());
        }

        protected override Promise OnChangeApply(EventSource eventSource, Change change)
        {
            Debug.Log(change.Tile + " is " + change.Type);

            return base.OnChangeApply(eventSource, change);
        }
    }
}