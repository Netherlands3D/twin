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
        [Header("Components")]
        [Tooltip("Determines which tiles are selected for visualisation")]
        [SerializeField] private BaseTileSelector tileSelector;
        [Tooltip("How should a strategy be planned to transition from the previous tiles in view to the new tiles")]
        [SerializeField] private BaseTransitionPlanner transitionPlanner;
        [Tooltip("How will changes be scheduled and executed")]
        [SerializeField] private BaseChangeScheduler changeScheduler;

        private List<Tile> TilesInView { get; } = new();

        protected override void OnFrustumChanged(TilekitEventSource tilekitEventSource, Plane[] planes)
        {
            var stagedTiles = tileSelector.Select(tileSet.Value, planes);

            EventChannel.TilesSelected.Invoke(tilekitEventSource, stagedTiles);
        }

        protected override void OnTilesSelected(TilekitEventSource tilekitEventSource, Tiles tiles)
        {
            var transition = transitionPlanner.CreateTransition(TilesInView, tiles);

            EventChannel.TransitionCreated.Invoke(tilekitEventSource, transition);
        }

        protected override void OnTransition(TilekitEventSource tilekitEventSource, List<Change> transition)
        {
            foreach (var change in transition)
            {
                EventChannel.ChangeScheduleRequested.Invoke(tilekitEventSource, change);
            }

            EventChannel.ChangesScheduled.Invoke(tilekitEventSource, transition);
        }

        protected override void OnChangeScheduleRequested(TilekitEventSource tilekitEventSource, Change change)
        {
            changeScheduler.Schedule(this, change);
        }

        protected override void OnChangesPlanned(TilekitEventSource tilekitEventSource, List<Change> changes)
        {
            StartCoroutine(changeScheduler.Apply());
        }

        protected override void OnChangeApply(TilekitEventSource tilekitEventSource, Change change)
        {
            Debug.Log(change.Tile + " is " + change.Type);

            base.OnChangeApply(tilekitEventSource, change);
        }
    }
}