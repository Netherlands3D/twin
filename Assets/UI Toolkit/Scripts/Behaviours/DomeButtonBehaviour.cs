using System.Runtime.InteropServices;
using Netherlands3D.Events;
using Netherlands3D.Masking;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "DomeButtonBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/DomeButtonBehaviour", order = 1)]
    public class DomeButtonBehaviour : FloatingButtonBehaviour
    {
        //todo give tooltip
        
        [SerializeField] private BoolEvent blockCameraDragListener;
        private Button domeButton;
        private PointerStyleService pointerStyleService;

        public override void Initialize(VisualElement parent)
        {
            base.Initialize(parent);
            ToolService toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.AddListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.AddListener(OnDisableDome);
            
            App.Dome.Spawner.DomeVisualisation.dragging.AddListener(OnDragDome);
            App.Dome.Spawner.DomeVisualisation.onHoveringChange.AddListener(OnHoverDome);
            
            pointerStyleService = ServiceLocator.GetService<PointerStyleService>();
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
                pointerStyleService.ChangeCursor(PointerStyleService.Style.GRAB);
            else
                pointerStyleService.ChangeCursor(PointerStyleService.Style.POINTER);
        }

        private void OnHoverDome(bool hover)
        {
            if (hover)
            {
                if(!dragging)
                    pointerStyleService.ChangeCursor(PointerStyleService.Style.POINTER);
            }
            else
                pointerStyleService.ChangeCursor(PointerStyleService.Style.AUTO);
        }

        public override VisualElement SpawnFloatingButtonContent()
        {
            domeButton = new Button();
            domeButton.Type = Button.ButtonType.Standard;
            domeButton.name = "DomeButton";
            domeButton.ShowIcon = Button.ButtonStyle.IconOnly;
            domeButton.Image = IconImage.SCALE_V_2;

            domeButton.RegisterCallback<PointerDownEvent>(evt =>
            {
                VisualDome dome = App.Dome.Spawner.DomeVisualisation;
                mainCamera = App.Cameras.ActiveCamera;

                pointerStartDragPosition = mainCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
                pointerObjectStartPosition = mainCamera.WorldToViewportPoint(dome.transform.position);
                pointerObjectStartPosition.z = 0; //Remove depth

                startDistance = Vector3.Distance(pointerStartDragPosition, pointerObjectStartPosition);

                startScale = dome.transform.localScale;
                domeButton.AddToClassList("grabbing");
                dragging = true;
            }, TrickleDown.TrickleDown);

            domeButton.RegisterCallback<PointerUpEvent>(evt =>
            {
                dragging = false;
                domeButton.RemoveFromClassList("grabbing");
            });

            domeButton.RegisterCallback<PointerEnterEvent>(evt =>
            {
                pointerStyleService.ChangeCursor(pointerStyleService.StyleOnHover);
                hovering = true;
            });

            domeButton.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                // Always change back cursor to CSS default 'auto'
                pointerStyleService.ChangeCursor(PointerStyleService.Style.AUTO);
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
            VisualDome dome = App.Dome.Spawner.DomeVisualisation;
            var worldPos = App.Dome.Spawner.DomeVisualisation.ScaleAnchor.position;
            var screenPos = App.Cameras.ActiveCamera.WorldToScreenPoint(worldPos);
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
            pointerStyleService.ChangeCursor(PointerStyleService.Style.AUTO);
            ToolService toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.RemoveListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.RemoveListener(OnDisableDome);
        }
    }
}