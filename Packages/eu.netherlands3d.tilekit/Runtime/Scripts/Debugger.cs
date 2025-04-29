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
        private EventChannel EventChannel => EventBus.All;

        [SerializeField] private bool logTicks = false;
        [SerializeField] private bool logInProduction = false;
        
        protected virtual void Start()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            EventChannel.UpdateTriggered += OnUpdateTriggered;
            EventChannel.FrustumChanged += OnFrustumChanged;
            EventChannel.TilesSelected += OnTilesSelected;
            EventChannel.TransitionCreated += OnTransition;
            EventChannel.ChangeScheduleRequested += OnChangeScheduleRequested;
            EventChannel.ChangesScheduled += OnChangesPlanned;
            EventChannel.ChangeApply += OnChangeApply;
        }

        private void OnDestroy()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            EventChannel.UpdateTriggered -= OnUpdateTriggered;
            EventChannel.FrustumChanged -= OnFrustumChanged;
            EventChannel.TilesSelected -= OnTilesSelected;
            EventChannel.TransitionCreated -= OnTransition;
            EventChannel.ChangeScheduleRequested -= OnChangeScheduleRequested;
            EventChannel.ChangesScheduled -= OnChangesPlanned;
            EventChannel.ChangeApply -= OnChangeApply;
        }

        private void OnUpdateTriggered(EventSource source)
        {
            // Let's not do this always, it clutters the console
            if (logTicks)
            {
                Log(source, $"A tick of the timer has happened for: {source.EventChannelId}");
            }
        }

        private void OnFrustumChanged(EventSource source, Plane[] frustumPlanes)
        {
            Log(source, $"The camera frustum was changed, let the games begin!");
        }

        private void OnTilesSelected(EventSource source, Tiles tiles)
        {
            Log(source, $"The following number of tiles should be in view: ({tiles.Count}) after transitioning");
        }

        private void OnTransition(EventSource source, List<Change> transition)
        {
            Log(source, $"A transition was planned with ({transition.Count}) changes");
        }

        private void OnChangeScheduleRequested(EventSource source, Change change)
        {
            Log(source, $"Change for ({change.Tile.Id}) is to be scheduled");
        }

        private void OnChangesPlanned(EventSource source, List<Change> changes)
        {
            Log(source, $"A series ({changes.Count}) of changes was planned");
        }

        private Promise OnChangeApply(EventSource source, Change change)
        {
            Log(source, $"Change for ({change.Tile.Id}) is being applied");
            
            return Promise.Resolved() as Promise;
        }

        private void Log(EventSource source, object message)
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;
            
            Debug.Log($"[Tilekit][{source.EventChannelId}] {message}");
        }
    }
}