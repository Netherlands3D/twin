using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Behaviours
{
    [RequireComponent(typeof(UIDocument))]
    public class FooterBehaviour : MonoBehaviour
    {
        [SerializeField] private StringEvent ShowAttributionEvent;
        [SerializeField] private Vector3Event OnCameraPositionChangedEvent;
        [SerializeField] private UnityEvent OnOrientToNorth = new();
        [SerializeField] private UnityEvent<bool> OnToggleOrthographicView = new();
        
        private UIDocument appDocument;

#region UI Elements
        private VisualElement root;
        private VisualElement Root => root ??= appDocument?.rootVisualElement;

        private Footer footer;
        private Footer Footer => footer ??= Root?.Q<Footer>();

        private ToolbarNavigation navigationToolbar;
        private ToolbarNavigation NavigationToolbar => navigationToolbar ??= Root?.Q<ToolbarNavigation>();
#endregion

        private void Awake()
        {
            appDocument = GetComponent<UIDocument>();
            
            // Ensure attribution is not visible upon start
            OnShowAttribution("");
            
            // Cannot disable because events do not support replaying, meaning you could lose attribution information
            // if attribution changed while object is disabled
            ShowAttributionEvent.AddListenerStarted(OnShowAttribution);
            OnCameraPositionChangedEvent.AddListenerStarted(OnActiveCameraPositionChanged);
            NavigationToolbar.OrientToNorth += OnOrientToNorth.Invoke; 
            NavigationToolbar.ToggleOrthographicView += OnToggleOrthographicView.Invoke; 
        }

        public void OnShowAttribution(string attribution)
        {
            Footer.Attribution = attribution;
        }

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
