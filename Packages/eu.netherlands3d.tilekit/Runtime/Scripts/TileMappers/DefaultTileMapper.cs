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

        private Tiles lastStagedTiles = new Tiles();

        protected override void OnFrustumChanged(TileSetEventStreamContext tileSetEventStreamContext, Plane[] planes)
        {
            var stagedTiles = tileSelector.Select(TileSetProvider.TileSet.Value, planes);

            EventStream.TilesSelected.Invoke(tileSetEventStreamContext, stagedTiles);
        }

        protected override void OnTilesSelected(TileSetEventStreamContext tileSetEventStreamContext, Tiles tiles)
        {
            lastStagedTiles = tiles;
            var transition = transitionPlanner.CreateTransition(TilesInView, tiles);

            EventStream.TransitionCreated.Invoke(tileSetEventStreamContext, transition);
        }

        protected override void OnTransition(TileSetEventStreamContext tileSetEventStreamContext, List<Change> transition)
        {
            foreach (var change in transition)
            {
                EventStream.ChangeScheduleRequested.Invoke(tileSetEventStreamContext, change);
            }

            EventStream.ChangesScheduled.Invoke(tileSetEventStreamContext, transition);
        }

        protected override void OnChangeScheduleRequested(TileSetEventStreamContext tileSetEventStreamContext, Change change)
        {
            changeScheduler.Schedule(this.TileSetProvider, change);
        }

        protected override void OnChangesPlanned(TileSetEventStreamContext tileSetEventStreamContext, List<Change> changes)
        {
            StartCoroutine(changeScheduler.Apply());
        }

        protected override void OnChangeApply(TileSetEventStreamContext tileSetEventStreamContext, Change change)
        {
            Debug.Log(change.Tile + " is " + change.Type);
            // TODO: This logic does not belong here, but the Change system needs to be revised. First I need the general
            // flow to work 
            switch (change.Type)
            {
                case TypeOfChange.Add: TilesInView.Add(change.Tile); break;
                case TypeOfChange.Remove: TilesInView.Remove(change.Tile); break;
            }

            base.OnChangeApply(tileSetEventStreamContext, change);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            
            // See which tiles were picked to be in view, so we can see whether the changes applied as expected
            foreach (var tile in lastStagedTiles)
            {
                DrawTileGizmo(tile, Color.red, Color.yellow, 0.95f);
            }
        }
    }
}