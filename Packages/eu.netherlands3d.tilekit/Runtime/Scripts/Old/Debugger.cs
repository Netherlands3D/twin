using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public class Debugger : MonoBehaviour
    {
        private TilekitEventSystem EventSystem => TilekitEventSystem.current;

        [SerializeField] private bool logTicks = false;
        [SerializeField] private bool logInProduction = false;
        
        protected virtual void Start()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            if (!TilekitEventSystem.current) return;

            EventSystem.UpdateTriggered.AddListener(OnUpdateTriggered);
            EventSystem.FrustumChanged.AddListener(OnFrustumChanged);
            EventSystem.TilesSelected.AddListener(OnTilesSelected);
            EventSystem.TransitionCreated.AddListener(OnTransition);
            EventSystem.ChangeScheduleRequested.AddListener(OnChangeScheduleRequested);
            EventSystem.ChangesScheduled.AddListener(OnChangesPlanned);
            EventSystem.ChangeApply.AddListener(OnChangeApply);
        }

        private void OnDestroy()
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            if (!TilekitEventSystem.current) return;

            EventSystem.UpdateTriggered.RemoveListener(OnUpdateTriggered);
            EventSystem.FrustumChanged.RemoveListener(OnFrustumChanged);
            EventSystem.TilesSelected.RemoveListener(OnTilesSelected);
            EventSystem.TransitionCreated.RemoveListener(OnTransition);
            EventSystem.ChangeScheduleRequested.RemoveListener(OnChangeScheduleRequested);
            EventSystem.ChangesScheduled.RemoveListener(OnChangesPlanned);
            EventSystem.ChangeApply.RemoveListener(OnChangeApply);
        }

        private void OnUpdateTriggered(ITileMapper tileMapper)
        {
            // Let's not do this always, it clutters the console
            if (logTicks)
            {
                Log(tileMapper, $"A tick of the timer has happened");
            }
        }

        private void OnFrustumChanged(ITileMapper tileMapper, Plane[] frustumPlanes)
        {
            Log(tileMapper, $"The camera frustum was changed, let the games begin!");
        }

        private void OnTilesSelected(ITileMapper tileMapper, Tiles tiles)
        {
            Log(tileMapper, $"The following number of tiles should be in view: ({tiles.Count}) after transitioning");
        }

        private void OnTransition(ITileMapper tileMapper, List<Change> transition)
        {
            Log(tileMapper, $"A transition was planned with ({transition.Count}) changes");
        }

        private void OnChangeScheduleRequested(ITileMapper tileMapper, Change change)
        {
            Log(tileMapper, $"Change for ({change.Tile.Id}) is to be scheduled");
        }

        private void OnChangesPlanned(ITileMapper tileMapper, List<Change> changes)
        {
            Log(tileMapper, $"A series ({changes.Count}) of changes was planned");
        }

        private void OnChangeApply(ITileMapper tileMapper, Change change)
        {
            Log(tileMapper, $"Change for ({change.Tile.Id}) is being applied");
        }

        private void Log(ITileMapper tileMapper, object message)
        {
            // Only log messages in the editor - or when the log in production flag is explicitly enabled
            // This is a debugger after all
            if (!logInProduction && !Application.isEditor) return;

            var tileSetId = tileMapper is ITileSetProvider provider ? provider.TileSetId : "Unknown";
            Debug.Log($"[Tilekit][{tileSetId}] {message}");
        }
    }
}