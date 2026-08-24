using Netherlands3D.Masking;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "DomeButtonBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/DomeButtonBehaviour", order = 1)]
    public class DomeButtonBehaviour : FloatingButtonBehaviour
    {
        private DomeButton domeButton;
        private ToolService toolService;
        
        [SerializeField] private PointerStyle.Style styleOnHover = PointerStyle.Style.GRABBING;

        public override void Initialize(VisualElement parent)
        {
            base.Initialize(parent);
            toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.AddListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.AddListener(OnDisableDome);
            
            App.Dome.Spawner.DomeVisualisation.dragging.AddListener(OnDragDome);
            App.Dome.Spawner.DomeVisualisation.onHoveringChange.AddListener(OnHoverDome);
            
            OnDisableDome();
            
        }

        private void OnDisableDome()
        {
            domeButton.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        private void OnEnableDome()
        {
            domeButton.EnableInClassList(UtilityClassConstants.HIDDEN, false);
        }

        private void OnDragDome(bool drag)
        {
            if (drag)
                PointerStyle.ChangeCursor(PointerStyle.Style.GRAB);
            else
                PointerStyle.ChangeCursor(PointerStyle.Style.POINTER);
        }

        private void OnHoverDome(bool hover)
        {
            if (hover)
            {
                if(!dragging)
                    PointerStyle.ChangeCursor(PointerStyle.Style.POINTER);
            }
            else
                PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
        }

        public override VisualElement SpawnFloatingButtonContent()
        {
            domeButton = new DomeButton();
            Button button = domeButton.Q<Button>();
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                VisualDome dome = App.Dome.Spawner.DomeVisualisation;
                mainCamera = App.Cameras.ActiveCamera;

                pointerStartDragPosition = mainCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
                pointerObjectStartPosition = mainCamera.WorldToViewportPoint(dome.transform.position);
                pointerObjectStartPosition.z = 0; //Remove depth

                startDistance = Vector3.Distance(pointerStartDragPosition, pointerObjectStartPosition);

                startScale = dome.transform.localScale;
                button.AddToClassList("grabbing");
                dragging = true;
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                dragging = false;
                button.RemoveFromClassList("grabbing");
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerEnterEvent>(evt =>
            {
                PointerStyle.ChangeCursor(styleOnHover);
                hovering = true;
            });

            button.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                // Always change back cursor to CSS default 'auto'
                PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
                hovering = false;
            });

            return domeButton;
        }

        private Camera mainCamera;

        [SerializeField] private float scaleMultiplier = 2.0f;

        private Vector3 startScale = Vector3.one;
        private Vector3 pointerStartDragPosition;
        private Vector3 pointerObjectStartPosition;

        private float startDistance;
        private bool hovering = false;
        private bool dragging = false;

        public override void UpdateBehaviour()
        {
            mainCamera = App.Cameras.ActiveCamera;
            VisualDome dome = App.Dome.Spawner.DomeVisualisation;
            var worldPos = App.Dome.Spawner.DomeVisualisation.ScaleAnchor.position;
            var screenPos = mainCamera.WorldToScreenPoint(worldPos);
            Vector2 panelPos = App.UIRoot.GetUIPositionFromScreenPosition(screenPos);
            var contentPos = content.worldBound.position;
            var localPos = panelPos - contentPos;
            floatingButton.SetPosition(localPos);

            if (dragging)
            {
                var pointerViewportPoint = mainCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
                float dist = Vector3.Distance(pointerViewportPoint, pointerObjectStartPosition);
                var distancePointerMoved = dist / startDistance;
                dome.SetTargetScale(startScale * distancePointerMoved);
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
            toolService.GetTool(ToolType.Dome).onOpen.RemoveListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.RemoveListener(OnDisableDome);
        }
    }
}