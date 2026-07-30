using Netherlands3D.Coordinates;
using Netherlands3D.JavascriptConnection;
using Netherlands3D.Minimap;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MapViewport : VisualElement
    {
        private Vector2 pointerDownPosition;
        private const float dragDeadzone = 4f;
        private bool isDragging;
        private bool unexpandOnMouseUp;

        private Icon locationPin;
        private bool showPin = true;

        private WMTSPanel wmtsPanel;
        private Camera activeCamera;
        private CustomQuad cameraFrustumQuad;

        private float zoomScale = 0.0f;
        private float minZoomScale = 0.0f;
        private float maxZoomScale = 10.0f;
        private float scrollTimeOut = 0.05f;
        private float lastScrollTime;
        private UnityEvent<int> OnZoomChanged = new();
        private Button zoomInButton;
        private Button zoomOutButton;

        [UxmlAttribute("show-pin")]
        public bool ShowPin
        {
            get => showPin;
            set
            {
                showPin = value;
                EnableInClassList("show-pin", value);
                locationPin.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        [UxmlAttribute("bottom-left")] public Vector2Int BottomLeft { get; set; } //vector2Int is parsable in uxml

        [UxmlAttribute("top-right")] public Vector2Int TopRight { get; set; } //vector2Int is parsable in uxml

        [UxmlAttribute("layer-start-index")] public int LayerStartIndex { get; set; } = 6;
        [UxmlAttribute("resize-on-hover")] public bool ResizeOnHover { get; set; } = false;
        private const string EXPANDED_USS_CLASS = "expanded";

        public MapViewport()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            locationPin = this.Q<Icon>("LocationPin");
            locationPin.style.transformOrigin = new TransformOrigin(Length.Percent(50), Length.Percent(100));

            wmtsPanel = this.Q<WMTSPanel>();

            zoomInButton = this.Q<Button>("ZoomIn");
            zoomOutButton = this.Q<Button>("ZoomOut");
            zoomInButton.RegisterCallback<ClickEvent>(OnZoomInClicked);
            zoomOutButton.RegisterCallback<ClickEvent>(OnZoomOutClicked);

            var dragManipulator = new DragManipulator(dragDeadzone);
            dragManipulator.DragStarted.AddListener(OnDragStarted);
            dragManipulator.Dragging.AddListener(OnDragging);
            dragManipulator.DragEnded.AddListener(OnDragEnded);
            this.AddManipulator(dragManipulator);
            
            cameraFrustumQuad = this.Q<CustomQuad>("CameraFrustum");

            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<WheelEvent>(OnScroll);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(OnPointerUp);
            RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            wmtsPanel.TilesChanged.AddListener(UpdateLocationPin); //make sure the pin stays in front of the tiles

            OnZoomChanged.AddListener(wmtsPanel.Zoom);
        }

        /// Provides the WMTS config and RD bounds that can't be authored via UXML.
        /// Must be called once before the map is usable.
        /// </summary>
        public void Initialize(MinimapConfig config)
        {
            //todo: can this function be removed somehow?
            wmtsPanel.Initialize(config, new Vector2RD(BottomLeft.x, BottomLeft.y), new Vector2RD(TopRight.x, TopRight.y), LayerStartIndex);
            var cameraService = ServiceLocator.GetService<CameraService>();
            activeCamera = cameraService.ActiveCamera;
                OnCameraPositionChanged(activeCamera.transform.position);
            cameraService.OnPositionChanged.AddListener(OnCameraPositionChanged);
            cameraService.OnSwitchCamera.AddListener(OnCameraSwitch);
        }

        private void OnZoomInClicked(ClickEvent evt)
        {
            ZoomIn(null);
        }

        private void OnZoomOutClicked(ClickEvent evt)
        {
            ZoomOut(null);
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            if(ResizeOnHover)
                EnableInClassList(EXPANDED_USS_CLASS, true);
            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.POINTER);
        }

        private void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (isDragging)
            {
                unexpandOnMouseUp = true;
                return;
            }

            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.AUTO);
            if(ResizeOnHover)
                EnableInClassList(EXPANDED_USS_CLASS, false);
        }

        private void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateLocationPin();
            UpdateFrustum();
        }
        
        private void OnCameraPositionChanged(Vector3 newWorldPosition)
        {
            UpdateLocationPin();
            UpdateFrustum();
        }
        
        private void OnCameraSwitch(Camera newCamera)
        {
            activeCamera = newCamera;
            OnCameraPositionChanged(activeCamera.transform.position);
        }
        
        private void UpdateLocationPin()
        {
            var activeCamera = ServiceLocator.GetService<CameraService>().ActiveCamera;
            var cameraPosition = new Coordinate(activeCamera.transform.position).Convert(CoordinateSystem.RDNAP);
            Vector2 mapPosition = wmtsPanel.DeterminePositionOnMap(cameraPosition);

            var iconSize = new Vector2(locationPin.resolvedStyle.width, locationPin.resolvedStyle.height);
            var pinPosition = mapPosition - new Vector2(iconSize.x * 0.5f, iconSize.y);

            locationPin.style.translate = new Translate(pinPosition.x, pinPosition.y);
            locationPin.style.scale = Vector3.one / wmtsPanel.resolvedStyle.scale.value.x;
        }
        
        private void UpdateFrustum()
        {
            CameraExtents.GetRDExtent(activeCamera);
            var cameraCorners = CameraExtents.GetWorldSpaceCorners(activeCamera);
            if (cameraCorners != null)
            {
                //Align quad with camera extent points
                var mapCoord0= wmtsPanel.DeterminePositionOnMap(new Coordinate(cameraCorners[3]).Convert(CoordinateSystem.RDNAP));
                var mapCoord1 = wmtsPanel.DeterminePositionOnMap(new Coordinate(cameraCorners[2]).Convert(CoordinateSystem.RDNAP));
                var mapCoord2 = wmtsPanel.DeterminePositionOnMap(new Coordinate(cameraCorners[1]).Convert(CoordinateSystem.RDNAP));
                var mapCoord3 = wmtsPanel.DeterminePositionOnMap(new Coordinate(cameraCorners[0]).Convert(CoordinateSystem.RDNAP));

                cameraFrustumQuad.QuadVertices[0] = wmtsPanel.LocalToViewport(mapCoord0);
                cameraFrustumQuad.QuadVertices[1] = wmtsPanel.LocalToViewport(mapCoord1);
                cameraFrustumQuad.QuadVertices[2] = wmtsPanel.LocalToViewport(mapCoord2);
                cameraFrustumQuad.QuadVertices[3] = wmtsPanel.LocalToViewport(mapCoord3);
                
                //Make sure our graphic width/height is set to the max distance of our verts, so culling works properly
                cameraFrustumQuad.Redraw();
            }
        }


        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Ensure pin visibility matches attribute
            locationPin.style.display = showPin ? DisplayStyle.Flex : DisplayStyle.None;
            zoomScale = minZoomScale;
            lastScrollTime = -scrollTimeOut;
        }

        /// <summary>
        /// Zoom in on the minimap
        /// </summary>
        /// <param name="useMousePosition"></param>
        public void ZoomIn(Vector2? panelPosition)
        {
            if (zoomScale >= maxZoomScale) return;

            zoomScale++;
            ZoomTowardsLocation(panelPosition);
            OnZoomChanged.Invoke((int)zoomScale);
        }

        /// <summary>
        /// Zoom out on the minimap
        /// </summary>
        /// <param name="useMousePosition"></param>
        public void ZoomOut(Vector2? panelPosition)
        {
            if (zoomScale <= minZoomScale) return;

            zoomScale--;
            ZoomTowardsLocation(panelPosition);
            OnZoomChanged.Invoke((int)zoomScale);
        }

        /// <summary>
        /// Set the zoom of the minimap
        /// </summary>
        /// <param name="newZoomScale">Zoom amount</param>
        public void SetZoom(float newZoomScale)
        {
            newZoomScale = Mathf.Clamp(newZoomScale, minZoomScale, maxZoomScale);

            if (zoomScale != newZoomScale)
            {
                zoomScale = newZoomScale;
                ZoomTowardsLocation(null);
                OnZoomChanged.Invoke((int)zoomScale);
            }
        }
        
        /// <summary>
        /// Zoom on a given location on the minimap
        /// </summary>
        /// <param name="useMouse"></param>
        private void ZoomTowardsLocation(Vector2? panelPosition)
        {
            Vector2 mapLocalTarget;
            if (panelPosition.HasValue)
            {
                mapLocalTarget = wmtsPanel.parent.WorldToLocal(panelPosition.Value);
            }
            else
            {
                // Center of the viewport in its own local space already
                mapLocalTarget = new Vector2(wmtsPanel.parent.resolvedStyle.width, wmtsPanel.parent.resolvedStyle.height) * 0.5f;
            }

            wmtsPanel.ScaleMapOverOrigin(mapLocalTarget, Vector3.one * Mathf.Pow(2.0f, zoomScale));
        }

        #region Inputs

        private void OnDragStarted(Vector2 startPosition)
        {
            isDragging = true;
            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.GRABBING);
        }

        private void OnDragging(Vector2 delta)
        {
            wmtsPanel.Pan(delta);
        }

        private void OnDragEnded(Vector2 endPosition)
        {
            isDragging = false;
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, Pointer.current.position.ReadValue());

            if (worldBound.Contains(panelPos))
                ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.POINTER); //pointer is still in the panel
            else
                ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.AUTO);
        }

        public void OnScroll(WheelEvent evt)
        {
            if (Time.time < lastScrollTime + scrollTimeOut)
                return;

            if (evt.delta.y < 0)
            {
                ZoomIn(evt.mousePosition);
                lastScrollTime = Time.time;
            }
            else if (evt.delta.y > 0)
            {
                ZoomOut(evt.mousePosition);
                lastScrollTime = Time.time;
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            pointerDownPosition = evt.position;
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (unexpandOnMouseUp && ResizeOnHover)
                EnableInClassList(EXPANDED_USS_CLASS, false);

            unexpandOnMouseUp = false;

            if (Vector2.Distance(pointerDownPosition, evt.position) > dragDeadzone) return; //we cannot use the manipulator event functions to set isDragging to true or false, because this causes a race-condition.

            wmtsPanel.ClickedMap(evt.position);
        }

        #endregion Interfaces
    }
}