using System.Collections.Generic;
using Netherlands3D.AddressSearch;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.UI.Behaviours
{
    /// <summary>
    /// Behaviour bridge between <see cref="AddressSearchService"/> and <see cref="LocationSearchPanel"/>.
    /// Subscribes to panel UI events, drives the service, and forwards coordinate/building results
    /// to outer consumers via UnityEvents.
    /// </summary>
    public class LocationSearchBehaviour : MonoBehaviour
    {
        [SerializeField] private AddressSearchService service;

        public UnityEvent<Coordinate> onCoordinateFound = new();

        private LocationSearchPanel panel;
        private CameraService cameraService;

        private void OnEnable()
        {
            if (service == null) return;
            cameraService = ServiceLocator.GetService<CameraService>();

            service.onCoordinateFound.AddListener(OnCoordinateFound);
            service.SuggestionsReady += OnSuggestionsReady;
            service.SuggestionsCleared += OnSuggestionsCleared;
            service.SuggestionAutoSelected += OnSuggestionAutoSelected;
        }

        private void OnDisable()
        {
            if (service == null) return;

            service.onCoordinateFound.RemoveListener(OnCoordinateFound);
            service.SuggestionsReady -= OnSuggestionsReady;
            service.SuggestionsCleared -= OnSuggestionsCleared;
            service.SuggestionAutoSelected -= OnSuggestionAutoSelected;
        }

        /// <summary>
        /// Wire the panel's UI events. Must be called once after the panel has been
        /// registered with the inspector panel system.
        /// </summary>
        public void Initialize(LocationSearchPanel locationSearchPanel)
        {
            panel = locationSearchPanel;

            panel.QueryChanged += OnQueryChanged;
            panel.SubmitRequested += OnSubmitRequested;
            panel.SuggestionSelected += OnSuggestionSelected;
            panel.CoordinateXSubmitted += OnCoordinateXSubmitted;
            panel.CoordinateYSubmitted += OnCoordinateYSubmitted;

            SyncPanelToMainCameraPosition();
        }

        private void OnQueryChanged(string text)
        {
            service?.FetchSuggestions(text);
        }

        private void OnSubmitRequested(string text, SuggestionResult? active)
        {
            if (active.HasValue)
            {
                ConfirmSuggestion(active.Value);
            }
            else if (!string.IsNullOrWhiteSpace(text))
            {
                // No suggestions open yet – fetch and auto-pick the first result
                service?.FetchSuggestionsForced(text);
            }
        }

        private void OnSuggestionSelected(SuggestionResult result)
        {
            ConfirmSuggestion(result);
        }

        private void OnSuggestionAutoSelected(SuggestionResult result)
        {
            ConfirmSuggestion(result);
        }

        private void OnCoordinateXSubmitted(string text)
        {
            if (!int.TryParse(text, out var x))
            {
                panel?.SetCoordinateValidity(false);
                return;
            }

            if (!TryGetMainCameraRd(out var mainCamera, out var currentRd)) return;

            var targetCoordinate = new Coordinate(CoordinateSystem.RDNAP, x, currentRd.northing, currentRd.height);
            MoveMainCamera(mainCamera, targetCoordinate);
        }

        private void OnCoordinateYSubmitted(string text)
        {
            if (!int.TryParse(text, out var y))
            {
                panel?.SetCoordinateValidity(false);
                return;
            }

            if (!TryGetMainCameraRd(out var mainCamera, out var currentRd))
                return;

            var targetCoordinate = new Coordinate(CoordinateSystem.RDNAP, currentRd.easting, y, currentRd.height);
            MoveMainCamera(mainCamera, targetCoordinate);
        }

        private void ConfirmSuggestion(SuggestionResult result)
        {
            panel?.SetQueryText(result.Label);
            panel?.ClearSuggestions();
            service?.SelectSuggestion(result.Id, result.Label);
        }

        private void OnSuggestionsReady(List<SuggestionResult> suggestions)
        {
            panel?.SetSuggestions(suggestions);
        }

        private void OnSuggestionsCleared()
        {
            panel?.ClearSuggestions();
        }

        private void OnCoordinateFound(Coordinate coord)
        {
            SyncPanelToCoordinate(coord);
            onCoordinateFound.Invoke(coord);
        }
        
        private void MoveMainCamera(Camera mainCamera, Coordinate targetCoordinate)
        {
            if (!mainCamera.TryGetComponent<WorldTransform>(out var worldTransform))
                return;

            worldTransform.MoveToCoordinate(targetCoordinate);
            SyncPanelToCoordinate(targetCoordinate);
        }

        private bool TryGetMainCameraRd(out Camera mainCamera, out Coordinate rdCoordinate)
        {
            mainCamera = cameraService.ActiveCamera;
            rdCoordinate = default;

            if (mainCamera == null) return false;

            rdCoordinate = new Coordinate(mainCamera.transform.position).Convert(CoordinateSystem.RDNAP);
            return true;
        }

        private void SyncPanelToMainCameraPosition()
        {
            var mainCamera = cameraService.ActiveCamera;
            if (mainCamera == null) return;
            SyncPanelToCoordinate(new Coordinate(mainCamera.transform.position));
        }

        private void SyncPanelToCoordinate(Coordinate coordinate)
        {
            var rd = coordinate.Convert(CoordinateSystem.RD);
            panel?.SetCoordinates((int)rd.easting, (int)rd.northing);
            panel?.SetCoordinateValidity(rd.IsValid());
        }
    }
}

