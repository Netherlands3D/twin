using Mono.Cecil.Cil;
using Netherlands3D.Twin;
using Netherlands3D.UI.ExtensionMethods;
using PlasticPipe.Server;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

        private const float edgeWidth = 160;

        void Awake()
        {
            var root = App.UIRoot.Root;
            CreateEdge(root, Left());
            CreateEdge(root, Right());
            CreateEdge(root, Top());
            CreateEdge(root, Bottom());

            CreateEdge(root, TopLeft());
            CreateEdge(root, TopRight());
            CreateEdge(root, BottomLeft());
            CreateEdge(root, BottomRight());
        }

        void CreateEdge(VisualElement root, VisualElement edge)
        {
            edge.pickingMode = PickingMode.Ignore;
            edge.style.position = Position.Absolute;
            edge.AddToClassList("tint-blue-900");
            root.parent.Insert(0, edge);
        }

        VisualElement Left()
        {
            var e = new VisualElement();
            e.style.left = 0;
            e.style.top = edgeWidth;
            e.style.bottom = edgeWidth;
            e.style.width = edgeWidth;
            e.style.backgroundImage = new StyleBackground(Rotate(sideSprite, SpriteRotation.Deg0));
            return e;
        }

        VisualElement Right()
        {
            var e = new VisualElement();
            e.style.right = 0;
            e.style.top = edgeWidth;
            e.style.bottom = edgeWidth;
            e.style.width = edgeWidth;
            e.style.backgroundImage = new StyleBackground(Rotate(sideSprite, SpriteRotation.Deg180));
            return e;
        }

        VisualElement Top()
        {
            var e = new VisualElement();
            e.style.left = edgeWidth;
            e.style.right = edgeWidth;
            e.style.top = 0;
            e.style.height = edgeWidth;
            e.style.backgroundImage = new StyleBackground(Rotate(sideSprite, SpriteRotation.Deg270));
            return e;
        }

        VisualElement Bottom()
        {
            var e = new VisualElement();

            e.style.left = edgeWidth;
            e.style.right = edgeWidth;
            e.style.bottom = 0;
            e.style.height = edgeWidth;
            e.style.backgroundImage = new StyleBackground(Rotate(sideSprite, SpriteRotation.Deg90));   
            return e;
        }

        VisualElement TopLeft()
        {
            var e = new VisualElement();
            e.style.left = 0;
            e.style.top = 0;
            e.style.width = edgeWidth;
            e.style.height = edgeWidth;
            e.style.backgroundImage = new StyleBackground(Rotate(cornerSprite, SpriteRotation.Deg270));
            return e;
        }

        VisualElement TopRight()
        {
            var e = new VisualElement();
            e.style.right = 0;
            e.style.top = 0;
            e.style.width = edgeWidth;
            e.style.height = edgeWidth; 
            e.style.backgroundImage = new StyleBackground(Rotate(cornerSprite, SpriteRotation.Deg180));
            return e;
        }

        VisualElement BottomLeft()
        {
            var e = new VisualElement();
            e.style.left = 0;
            e.style.bottom = 0;
            e.style.width = edgeWidth;
            e.style.height = edgeWidth; 
            e.style.backgroundImage = new StyleBackground(Rotate(cornerSprite, SpriteRotation.Deg0));
            return e;
        }

        VisualElement BottomRight()
        {
            var e = new VisualElement();
            e.style.right = 0;
            e.style.bottom = 0;
            e.style.width = edgeWidth;
            e.style.height = edgeWidth;
            e.style.backgroundImage = new StyleBackground(Rotate(cornerSprite, SpriteRotation.Deg90));
            return e;
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