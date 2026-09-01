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
                if(!domeButton.Dragging)
                    PointerStyle.ChangeCursor(PointerStyle.Style.POINTER);
            }
            else
                PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
        }

        public override VisualElement SpawnFloatingButtonContent()
        {
            domeButton = new DomeButton();
            domeButton.SetStyleOnHover(styleOnHover);
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
            PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
            toolService.GetTool(ToolType.Dome).onOpen.RemoveListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.RemoveListener(OnDisableDome);
        }
    }
}