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
        
        [DllImport("__Internal")]
        private static extern string SetCSSCursor(string cursorName = "auto");
        
        private Button domeButton;

        public override void Initialize(VisualElement parent)
        {
            base.Initialize(parent);
            ToolService toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.AddListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.AddListener(OnDisableDome);
            
        }

        private void OnDisableDome()
        {
            domeButton.EnableInClassList(UtilityClassConstants.HIDDEN, true);
        }

        private void OnEnableDome()
        {
            domeButton.EnableInClassList(UtilityClassConstants.HIDDEN, false);
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
                dragging = true;

                domeButton.CapturePointer(evt.pointerId);
            }, TrickleDown.TrickleDown);

            domeButton.RegisterCallback<PointerUpEvent>(evt =>
            {
                dragging = false;

                Debug.Log("dome up!");
            });

            domeButton.RegisterCallback<PointerEnterEvent>(evt =>
            {
                ChangeCursor(StyleOnHover);

                hovering = true;
            });

            domeButton.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                // Always change back cursor to CSS default 'auto'
                ChangeCursor(Style.AUTO);

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
            ChangeCursor(Style.AUTO);
            ToolService toolService = ServiceLocator.GetService<ToolService>();
            toolService.GetTool(ToolType.Dome).onOpen.RemoveListener(OnEnableDome);
            toolService.GetTool(ToolType.Dome).onClose.RemoveListener(OnDisableDome);
        }

        public enum Style
        {
            AUTO,
            DEFAULT,
            NONE,
            CONTEXT_MENU,
            HELP,
            POINTER,
            PROGRESS,
            WAIT,
            CELL,
            CROSSHAIR,
            TEXT,
            VERTICAL_TEXT,
            ALIAS,
            COPY,
            MOVE,
            NO_DROP,
            NOT_ALLOWED,
            GRAB,
            GRABBING,
            ALL_SCROLL,
            COL_RESIZE,
            ROW_RESIZE,
            N_RESIZE,
            NE_RESIZE,
            E_RESIZE,
            SE_RESIZE,
            S_RESIZE,
            SW_RESIZE,
            W_RESIZE,
            NW_RESIZE,
            EW_RESIZE,
            NS_RESIZE,
            NESW_RESIZE,
            NWSE_RESIZE
        }

        [SerializeField]
        private Style styleOnHover = Style.POINTER;

        public static Style cursorType = Style.AUTO;

        public Style StyleOnHover
        {
            get => styleOnHover;
            set => styleOnHover = value;
        }

        public static void ChangeCursor(Style type)
        {
            cursorType = type;

            var cursorString = "";

            switch (cursorType)
            {
                case Style.AUTO:
                    cursorString = "auto";
                    break;
                case Style.DEFAULT:
                    cursorString = "default";
                    break;
                case Style.NONE:
                    cursorString = "none";
                    break;
                case Style.CONTEXT_MENU:
                    cursorString = "context-menu";
                    break;
                case Style.HELP:
                    cursorString = "help";
                    break;
                case Style.POINTER:
                    cursorString = "pointer";
                    break;
                case Style.PROGRESS:
                    cursorString = "progress";
                    break;
                case Style.WAIT:
                    cursorString = "wait";
                    break;
                case Style.CELL:
                    cursorString = "cell";
                    break;
                case Style.CROSSHAIR:
                    cursorString = "crosshair";
                    break;
                case Style.TEXT:
                    cursorString = "text";
                    break;
                case Style.VERTICAL_TEXT:
                    cursorString = "vertical-text";
                    break;
                case Style.ALIAS:
                    cursorString = "alias";
                    break;
                case Style.COPY:
                    cursorString = "copy";
                    break;
                case Style.MOVE:
                    cursorString = "move";
                    break;
                case Style.NO_DROP:
                    cursorString = "no-drop";
                    break;
                case Style.NOT_ALLOWED:
                    cursorString = "not-allowed";
                    break;
                case Style.GRAB:
                    cursorString = "grab";
                    break;
                case Style.GRABBING:
                    cursorString = "grabbing";
                    break;
                case Style.ALL_SCROLL:
                    cursorString = "all-scroll";
                    break;
                case Style.COL_RESIZE:
                    cursorString = "col-resize";
                    break;
                case Style.ROW_RESIZE:
                    cursorString = "row-resize";
                    break;
                case Style.N_RESIZE:
                    cursorString = "n-resize";
                    break;
                case Style.NE_RESIZE:
                    cursorString = "ne-resize";
                    break;
                case Style.E_RESIZE:
                    cursorString = "e-resize";
                    break;
                case Style.SE_RESIZE:
                    cursorString = "se-resize";
                    break;
                case Style.S_RESIZE:
                    cursorString = "s-resize";
                    break;
                case Style.SW_RESIZE:
                    cursorString = "sw-resize";
                    break;
                case Style.W_RESIZE:
                    cursorString = "w-resize";
                    break;
                case Style.NW_RESIZE:
                    cursorString = "nw-resize";
                    break;
                case Style.EW_RESIZE:
                    cursorString = "ew-resize";
                    break;
                case Style.NS_RESIZE:
                    cursorString = "ns-resize";
                    break;
                case Style.NESW_RESIZE:
                    cursorString = "nesw-resize";
                    break;
                case Style.NWSE_RESIZE:
                    cursorString = "nwse-resize";
                    break;
            }

#if !UNITY_EDITOR && UNITY_WEBGL
            SetCSSCursor(cursorString);
#endif
        }
    }
}