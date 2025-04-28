using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Tilekit;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit.Events;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    [RequireComponent(typeof(Timer))]
    [RequireComponent(typeof(FrustrumChecker))]
    public class TileMapper2 : MonoBehaviour
    {
        [field:SerializeField] public string Id { get; } = Guid.NewGuid().ToString();
        
        public TileSelector TileSelector;
        public TilesTransitionPlanner TilesTransitionPlanner;
        [SerializeField] private float intervalInSeconds = 0.2f;

        private TileSet tileSet;
        private Coroutine coroutine;
        private Timer timer;
        public EventChannel EventChannel => EventBus.Channel(Id);
        public EventSource EventSource;

        public List<Tile> TilesInView { get; private set; } = new();

        private void Awake()
        {
            timer = GetComponent<Timer>();
            // Make sure timer doesn't autostart, we need it to start running only once the tileSet has loaded
            timer.AutoStart = false;
        }

        private void Start()
        {
            timer.tick.AddListener(OnTimerTick);
            EventChannel.FrustumChanged += OnFrustumChanged;
            EventChannel.TilesSelected += OnTilesSelected;
            EventChannel.TransitionCreated += OnTransition;
            EventChannel.ChangesPlanned += OnChangesPlanned;
        }

        private void OnDestroy()
        {
            timer.tick.RemoveListener(OnTimerTick);
            EventChannel.FrustumChanged -= OnFrustumChanged;
            EventChannel.TilesSelected -= OnTilesSelected;
            EventChannel.TransitionCreated -= OnTransition;
            EventChannel.ChangesPlanned -= OnChangesPlanned;
        }

        public void Load(TileSet tileSet)
        {
            this.tileSet = tileSet;
            EventSource = new EventSource(tileSet);
            timer.Resume();
        }

        private void OnTimerTick()
        {
            EventChannel.Tick.Invoke(EventSource);
        }

        private void OnFrustumChanged(EventSource eventSource, Plane[] planes)
        {
            var stagedTiles = TileSelector.Select(tileSet, planes);

            EventChannel.TilesSelected.Invoke(eventSource, stagedTiles);
        }

        private void OnTilesSelected(EventSource eventSource, Tiles tiles)
        {
            var transition = TilesTransitionPlanner.CreateTransition(TilesInView, tiles);

            EventChannel.TransitionCreated.Invoke(eventSource, transition);
        }

        private void OnTransition(EventSource eventSource, List<Change> transition)
        {
            // Map -> Create new ChangePlan from Transition + Running Changeplan
            //  -- Can we do this without a complicated service but just by merging the transition into the Changeplan
            //     and getting a new one? Or by having a service without a Changeplan entity?
            EventChannel.ChangesPlanned.Invoke(eventSource, transition);
        }

        private void OnChangesPlanned(EventSource eventSource, List<Change> changes)
        {
            // Map -> Perform changeplan
        }
    }
}