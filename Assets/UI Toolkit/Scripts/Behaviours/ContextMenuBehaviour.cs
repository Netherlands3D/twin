using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    public class ContextMenuBehaviour : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private FloatingPanelBehaviour[] panelBehaviours;

        [SerializeField] private Texture2D cornerSprite;
        [SerializeField] private Texture2D sideSprite;

        private VisualElement root;
        private InputAction rightClickAction;
        private InputAction leftClickAction;
        private InputAction longPressAction;
        private InputAction touchAction;
        private FloatingPanel floatingPanel;
        private VisualElement content;
        private FloatingPanelBehaviour selectedBehaviour;

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            floatingPanel = new FloatingPanel();
            root.Add(floatingPanel);
            floatingPanel.OnClose.AddListener(ClearActivePanel);
            floatingPanel.SetEnabled(false);
            var map = inputActionAsset.FindActionMap("Camera", true);
            rightClickAction = map.FindAction("RightClick", true);
            leftClickAction = map.FindAction("LeftClick", true);
            longPressAction = map.FindAction("LongPress", true);
            touchAction = map.FindAction("Touch", true);
            
            rightClickAction.performed += OnRightClick;
            leftClickAction.performed += OnLeftClick;
            longPressAction.performed += OnRightClick;
            touchAction.performed += OnLeftClick;
        }

        void OnDisable()
        {
            rightClickAction.performed -= OnRightClick;
            leftClickAction.performed -= OnLeftClick;
            longPressAction.performed -= OnRightClick;
            touchAction.performed -= OnLeftClick;
            ClearActivePanel();
            floatingPanel = null;
        }

        public void ClearActivePanel()
        {
            if (content == null)
                return;

            selectedBehaviour?.Dispose();
            floatingPanel.Remove(content);
            content = null;
            floatingPanel.SetEnabled(false);
        }

        private void OnRightClick(InputAction.CallbackContext ctx)
        {
            Vector2 panelPos = App.UIRoot.GetPanelClickPosition();
            App.UIRoot.ClickedUI(panelPos);
            
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
            if(App.UIRoot.ClickedUI(panelPos))
                return;
            
            CheckAndSpawnPanel(panelPos);
        }
        
        private void OnLeftClick(InputAction.CallbackContext ctx)
        {
            Vector2 panelPos = App.UIRoot.GetPanelClickPosition();
            if(IsActivePanelClicked(panelPos))
                return;
            
            ClearActivePanel();
        }

       

        private bool IsActivePanelClicked(Vector2 screenPos)
        {
            if(floatingPanel == null) return false;
            
            var picked = floatingPanel.panel.Pick(screenPos);
            return picked != null && floatingPanel.Contains(picked);
        }
        
        private void CheckAndSpawnPanel(Vector2 screenPos)
        {
            foreach (var panelBehaviour in panelBehaviours)
            {
                if(!panelBehaviour.ShouldBeActive()) continue;

                selectedBehaviour = panelBehaviour;
                var data = panelBehaviour.GetData();
                content = panelBehaviour.SpawnFloatingPanelContent(floatingPanel, data);
                floatingPanel.SetEnabled(true);
                floatingPanel.Add(content);
                floatingPanel.SetPosition(screenPos);
                break;
            }
        }

        private const float edgeWidth = 80;

        void Awake()
        {
            var anchor = App.UIRoot.Root.parent;

            CreateEdge(anchor, sideSprite, SpriteRotation.Deg0,   left: 0,         top: edgeWidth,  bottom: edgeWidth, width: edgeWidth);
            CreateEdge(anchor, sideSprite, SpriteRotation.Deg180, right: 0,        top: edgeWidth,  bottom: edgeWidth, width: edgeWidth);
            CreateEdge(anchor, sideSprite, SpriteRotation.Deg270, left: edgeWidth, right: edgeWidth, top: 0,           height: edgeWidth);
            CreateEdge(anchor, sideSprite, SpriteRotation.Deg90,  left: edgeWidth, right: edgeWidth, bottom: 0,        height: edgeWidth);

            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg270, left: 0,  top: 0,    width: edgeWidth, height: edgeWidth);
            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg180, right: 0, top: 0,    width: edgeWidth, height: edgeWidth);
            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg0,   left: 0,  bottom: 0, width: edgeWidth, height: edgeWidth);
            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg90,  right: 0, bottom: 0, width: edgeWidth, height: edgeWidth);
        }

        void CreateEdge(
            VisualElement anchor,
            Texture2D sprite,
            SpriteRotation rotation,
            float? left = null, float? right = null, float? top = null, float? bottom = null,
            float? width = null, float? height = null)
        {
            var e = new VisualElement();
            e.pickingMode = PickingMode.Ignore;
            e.AddToClassList("vignette-edge");
            e.style.backgroundImage = new StyleBackground(Rotate(sprite, rotation));

            if (left.HasValue)   e.style.left   = left.Value;
            if (right.HasValue)  e.style.right  = right.Value;
            if (top.HasValue)    e.style.top    = top.Value;
            if (bottom.HasValue) e.style.bottom = bottom.Value;
            if (width.HasValue)  e.style.width  = width.Value;
            if (height.HasValue) e.style.height = height.Value;

            anchor.Insert(0, e);
        }

        public enum SpriteRotation
        {
            Deg0,
            Deg90,
            Deg180,
            Deg270
        }

        public static Texture2D Rotate(Texture2D source, SpriteRotation rotation)
        {
            switch (rotation)
            {
                case SpriteRotation.Deg90:
                    return Rotate90(source);
                case SpriteRotation.Deg180:
                    return Rotate90(Rotate90(source));
                case SpriteRotation.Deg270:
                    return Rotate90(Rotate90(Rotate90(source)));
            }
            return source;
        }

        private static Texture2D Rotate90(Texture2D source)
        {
            int w = source.width;
            int h = source.height;
            Texture2D result = new Texture2D(h, w, source.format, false);
            Color32[] src = source.GetPixels32();
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int srcIndex = x + y * w;
                    int newX = h - 1 - y;
                    int newY = x;
                    int dstIndex = newX + newY * h;
                    dst[dstIndex] = src[srcIndex];
                }
            }
            result.SetPixels32(dst);
            result.Apply();
            return result;
        }
    }
}