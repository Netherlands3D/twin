using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using Netherlands3D.Twin.Tilekit.Events;
using Netherlands3D.Twin.Tilekit.TileMappers;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class Debugger : MonoBehaviour
    {
        private TileSetEventStream EventStream => EventBus.All;

        [SerializeField] private bool logTicks = false;
        [SerializeField] private bool logInProduction = false;
        
        protected virtual void Start()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            EventStream.UpdateTriggered.AddListener(OnUpdateTriggered);
            EventStream.FrustumChanged.AddListener(OnFrustumChanged);
            EventStream.TilesSelected.AddListener(OnTilesSelected);
            EventStream.TransitionCreated.AddListener(OnTransition);
            EventStream.ChangeScheduleRequested.AddListener(OnChangeScheduleRequested);
            EventStream.ChangesScheduled.AddListener(OnChangesPlanned);
            EventStream.ChangeApply.AddListener(OnChangeApply);
        }

        private void OnDestroy()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            EventStream.UpdateTriggered.RemoveListener(OnUpdateTriggered);
            EventStream.FrustumChanged.RemoveListener(OnFrustumChanged);
            EventStream.TilesSelected.RemoveListener(OnTilesSelected);
            EventStream.TransitionCreated.RemoveListener(OnTransition);
            EventStream.ChangeScheduleRequested.RemoveListener(OnChangeScheduleRequested);
            EventStream.ChangesScheduled.RemoveListener(OnChangesPlanned);
            EventStream.ChangeApply.RemoveListener(OnChangeApply);
        }

        private void OnUpdateTriggered(TileSetEventStreamContext streamContext)
        {
            // Let's not do this always, it clutters the console
            if (logTicks)
            {
                Log(streamContext, $"A tick of the timer has happened for: {streamContext.EventStreamId}");
            }
        }

        private void OnFrustumChanged(TileSetEventStreamContext streamContext, Plane[] frustumPlanes)
        {
            Log(streamContext, $"The camera frustum was changed, let the games begin!");
        }

        private void OnTilesSelected(TileSetEventStreamContext streamContext, Tiles tiles)
        {
            Log(streamContext, $"The following number of tiles should be in view: ({tiles.Count}) after transitioning");
        }

        private void OnTransition(TileSetEventStreamContext streamContext, List<Change> transition)
        {
            Log(streamContext, $"A transition was planned with ({transition.Count}) changes");
        }

        private void OnChangeScheduleRequested(TileSetEventStreamContext streamContext, Change change)
        {
            Log(streamContext, $"Change for ({change.Tile.Id}) is to be scheduled");
        }

        private void OnChangesPlanned(TileSetEventStreamContext streamContext, List<Change> changes)
        {
            Log(streamContext, $"A series ({changes.Count}) of changes was planned");
        }

        private void OnChangeApply(TileSetEventStreamContext streamContext, Change change)
        {
            Log(streamContext, $"Change for ({change.Tile.Id}) is being applied");
        }

        private void Log(TileSetEventStreamContext streamContext, object message)
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;
            
            Debug.Log($"[Tilekit][{streamContext.EventStreamId}] {message}");
        }
    }
}