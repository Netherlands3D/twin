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
        private TilekitEventChannel EventChannel => EventBus.All;

        [SerializeField] private bool logTicks = false;
        [SerializeField] private bool logInProduction = false;
        
        protected virtual void Start()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            EventChannel.UpdateTriggered.AddListener(OnUpdateTriggered);
            EventChannel.FrustumChanged.AddListener(OnFrustumChanged);
            EventChannel.TilesSelected.AddListener(OnTilesSelected);
            EventChannel.TransitionCreated.AddListener(OnTransition);
            EventChannel.ChangeScheduleRequested.AddListener(OnChangeScheduleRequested);
            EventChannel.ChangesScheduled.AddListener(OnChangesPlanned);
            EventChannel.ChangeApply.AddListener(OnChangeApply);
        }

        private void OnDestroy()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            EventChannel.UpdateTriggered.RemoveListener(OnUpdateTriggered);
            EventChannel.FrustumChanged.RemoveListener(OnFrustumChanged);
            EventChannel.TilesSelected.RemoveListener(OnTilesSelected);
            EventChannel.TransitionCreated.RemoveListener(OnTransition);
            EventChannel.ChangeScheduleRequested.RemoveListener(OnChangeScheduleRequested);
            EventChannel.ChangesScheduled.RemoveListener(OnChangesPlanned);
            EventChannel.ChangeApply.RemoveListener(OnChangeApply);
        }

        private void OnUpdateTriggered(TilekitEventSource source)
        {
            // Let's not do this always, it clutters the console
            if (logTicks)
            {
                Log(source, $"A tick of the timer has happened for: {source.EventChannelId}");
            }
        }

        private void OnFrustumChanged(TilekitEventSource source, Plane[] frustumPlanes)
        {
            Log(source, $"The camera frustum was changed, let the games begin!");
        }

        private void OnTilesSelected(TilekitEventSource source, Tiles tiles)
        {
            Log(source, $"The following number of tiles should be in view: ({tiles.Count}) after transitioning");
        }

        private void OnTransition(TilekitEventSource source, List<Change> transition)
        {
            Log(source, $"A transition was planned with ({transition.Count}) changes");
        }

        private void OnChangeScheduleRequested(TilekitEventSource source, Change change)
        {
            Log(source, $"Change for ({change.Tile.Id}) is to be scheduled");
        }

        private void OnChangesPlanned(TilekitEventSource source, List<Change> changes)
        {
            Log(source, $"A series ({changes.Count}) of changes was planned");
        }

        private void OnChangeApply(TilekitEventSource source, Change change)
        {
            Log(source, $"Change for ({change.Tile.Id}) is being applied");
        }

        private void Log(TilekitEventSource source, object message)
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;
            
            Debug.Log($"[Tilekit][{source.EventChannelId}] {message}");
        }
    }
}