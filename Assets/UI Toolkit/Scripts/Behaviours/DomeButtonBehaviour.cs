using Netherlands3D.Masking;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "DomeButtonBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/DomeButtonBehaviour", order = 1)]
    public class DomeButtonBehaviour : FloatingButtonBehaviour
    {
        private DomeButton domeButton;
        private ToolService toolService;

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
        
        private bool isDragging;

        private void OnDragDome(bool drag)
        {
            if (drag)
            {
                isDragging = true;
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRABBING);
            }
            else
            {
                isDragging = false;
                if (App.Dome.Spawner.DomeVisualisation.Hovering)
                    PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB); //pointer is still in the panel
                else
                    PointerStyle.CancelCursorChange(this);
            }
        }

        private void OnHoverDome(bool hover)
        {
            if (hover)
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB);
            else if(!isDragging)
                PointerStyle.CancelCursorChange(this);
        }

        public override VisualElement SpawnFloatingButtonContent()
        {
            domeButton = new DomeButton();
            return domeButton;
        }

        [SerializeField] private float scaleMultiplier = 2.0f;

        public override void UpdateBehaviour()
        {
            VisualDome dome = App.Dome.Spawner.DomeVisualisation;
            var worldPos = App.Dome.Spawner.DomeVisualisation.ScaleAnchor.position;
            var screenPos =  App.Cameras.ActiveCamera.WorldToScreenPoint(worldPos);
            Vector2 panelPos = App.UIRoot.GetUIPositionFromScreenPosition(screenPos);
            var contentPos = content.worldBound.position;
            var localPos = panelPos - contentPos;
            floatingButton.SetPosition(localPos);

            if (domeButton.Dragging)
            {
                dome.SetTargetScale(domeButton.GetDistanceScale());
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
            PointerStyle.CancelCursorChange(this);
            toolService.GetTool(ToolType.Dome).onOpen.RemoveListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.RemoveListener(OnDisableDome);
        }
    }
}