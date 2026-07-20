using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    public class FooterBehaviour : MonoBehaviour
    {
        [SerializeField] private StringEvent ShowAttributionEvent;
        [SerializeField] private Vector3Event OnCameraPositionChangedEvent;
        [SerializeField] private UnityEvent<bool> OnToggleOrthographicView = new();

        private Footer footer;
        private ToolbarNavigation navigationToolbar;

        private void Awake()
        {
            footer = App.UIRoot.Root.Q<Footer>();
            navigationToolbar = App.UIRoot.Root.Q<ToolbarNavigation>();
            // Ensure attribution is not visible upon start
            OnShowAttribution("");
            
            // Cannot disable because events do not support replaying, meaning you could lose attribution information
            // if attribution changed while object is disabled
            ShowAttributionEvent.AddListenerStarted(OnShowAttribution);
            OnCameraPositionChangedEvent.AddListenerStarted(OnActiveCameraPositionChanged);
            navigationToolbar.ToggleOrthographicView += OnToggleOrthographicView.Invoke;
        }
        
        public void OnShowAttribution(string attribution) => footer.Attribution = attribution;

        public void OnActiveCameraPositionChanged(Vector3 position)
        {
            Coordinate coordinate = new Coordinate(position);
            var rd = coordinate.Convert(CoordinateSystem.RDNAP);

            footer.X = (float)rd.easting;
            footer.Y = (float)rd.northing;
            footer.Z = (float)rd.height;
        }
    }
}
