using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.TileSystem;
using Netherlands3D.Twin.UI.Inpector.Layers;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI.Inpector
{
    public class LayersUI : MonoBehaviour
    {
        public static LayersUI Instance;
        public static List<LayerNL3DBase> Layers = new List<LayerNL3DBase>();

        [SerializeField] private Tool layerTool;
        public Tool LayerTool => layerTool;
        [SerializeField] private string accordionId = "unity-content";
        [SerializeField] private VisualTreeAsset layerAsset;
        [SerializeField] private TileSystem.TileHandler tileHandler;

        private VisualElement layerInspector;

        private LayerUI2 draggingLayer;
        private VisualElement dragLayerElement;

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            layerTool.onActivate.AddListener(OnLayerToolActivated);
            layerTool.onDeactivate.AddListener(OnLayerToolDeactivated);
        }

        private void OnDisable()
        {
            layerTool.onActivate.RemoveListener(OnLayerToolActivated);
            layerTool.onDeactivate.RemoveListener(OnLayerToolDeactivated);
        }

        private void OnLayerToolDeactivated()
        {
            foreach (var layer in Layers)
            {
                layer.UnLinkListeners();
            }
        }

        private void OnLayerToolActivated()
        {
            layerInspector = layerTool.InspectorInstance;
            foreach (var layer in Layers)
            {
                AddLayerUI(layer);
                layer.LinkListeners();
            }
        }

        public LayerUI2 AddLayerUI(LayerNL3DBase layer)
        {
            var layerRowTemplate = layerAsset.Instantiate();
            layerInspector.Add(layerRowTemplate);
            var ui = new LayerUI2(layer, layerRowTemplate);
            layer.UI = ui;

            ui.DragStarted.AddListener(OnLayerUIStartDrag);

            return ui;
        }

        private void OnLayerUIStartDrag(LayerUI2 layer)
        {
            draggingLayer = layer;
            dragLayerElement = layerAsset.Instantiate();
            layerInspector.Add(dragLayerElement);
        }

        private void Update()
        {
            if (draggingLayer != null)
            {
                var mousePosition = Pointer.current.position.ReadValue();
                mousePosition.y = Screen.height - mousePosition.y;
                // var viewportPosition = Camera.main.ScreenToViewportPoint(mousePosition);
                // var mousePositionInGameWindow = viewportPosition * new Vector2(Screen.currentResolution.width, Screen.currentResolution.height);
                // print(mousePosition + "\t" + mousePositionInGameWindow);
                // Vector2 mousePositionInGameWindow = mousePosition - new Vector2(Screen.currentResolution.width - Screen.width, Screen.currentResolution.height - Screen.height);
                // print(mousePositionInGameWindow);

                // Vector2 windowOffset = new Vector2(Screen.currentResolution.width - Screen.width, Screen.currentResolution.height - Screen.height); // var mousePositionCorrected = GetComponent<UIDocument>().rootVisualElement.WorldToLocal(mousePosition);
                // var mousePositionCorrected = RuntimePanelUtils.ScreenToPanel(GetComponent<UIDocument>().rootVisualElement.panel, mousePositionInGameWindow);
                var mousePositionCorrected = RuntimePanelUtils.ScreenToPanel(dragLayerElement.parent.panel, mousePosition);
                var panelOffset = GetAbsolutePosition(draggingLayer.LayerUIElement);
                print("test" + panelOffset);
                var siblingIndex = draggingLayer.LayerUIElement.parent.childCount - 1 - GetSiblingIndex(draggingLayer.LayerUIElement);
                var heightOffset = siblingIndex * draggingLayer.LayerUIElement.resolvedStyle.height;
                print( siblingIndex +"\theightoffset " + heightOffset);
                panelOffset.y += heightOffset;
                panelOffset += draggingLayer.dragStartOffset;
                // var mousePositionCorrected = dragLayerElement.parent.WorldToLocal(mousePosition);
                // var mousePositionCorrected = RuntimePanelUtils.ScreenToPanel(dragLayerElement.parent.panel, mousePosition - windowOffset);
                // mousePositionCorrected.y -= dragLayerElement.parent.resolvedStyle.top;
                // Vector2 gameWindowOffset = new Vector2((Screen.currentResolution.width - Screen.width) / 2, (Screen.currentResolution.height - Screen.height) / 2);
                // Vector2 mousePositionCorrected = new Vector2(mousePosition.x -gameWindowOffset.x, Screen.height - mousePosition.y - gameWindowOffset.y);
                // dragLayerElement.style.top = mousePositionCorrected.y;
                // dragLayerElement.style.position = Position.Absolute;
                // dragLayerElement.style.width = draggingLayer.LayerUIElement.style.width;
                dragLayerElement.transform.position = mousePositionCorrected - new Vector2(panelOffset.x, panelOffset.y);
                // dragLayerElement.style.left = mousePositionCorrected.x;
                // print(mousePosition + "\t" + mousePositionCorrected + "\t" + dragLayerElement.parent.resolvedStyle.top);

                if (!Pointer.current.press.IsPressed())
                {
                    draggingLayer.StopDrag();
                    draggingLayer = null;
                    layerInspector.Remove(dragLayerElement);
                }
            }
        }
        
        public static Vector2 GetAbsolutePosition(VisualElement element)
        {
            Vector2 localPosition = element.transform.position;
            Vector2 absolutePosition = element.LocalToWorld(localPosition);

            return absolutePosition;
        }
        
        public static int GetSiblingIndex(VisualElement element)
        {
            VisualElement parent = element.parent;
            if (parent != null)
            {
                int childCount = parent.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    if (parent[i] == element)
                    {
                        return i;
                    }
                }
            }
            return -1; // Element is not a child of the provided parent.
        }
    }

    public class LayerUI2
    {
        public LayerNL3DBase Layer { get; }
        public VisualElement LayerUIElement { get; }
        public Toggle EnabledToggle { get; }
        public Toggle FoldoutToggle { get; }
        public Button ColorButton { get; }
        public Label TextLabel { get; }
        public VisualElement childrenContainer { get; }

        private Vector3 pointerDownPosition;
        private bool pointerDown;
        private int dragThreshold = 50;
        public bool IsDragging { get; private set; }
        public UnityEvent<LayerUI2> PointerDown = new();
        public UnityEvent<LayerUI2> DragStarted = new();
        public UnityEvent<LayerUI2> DragEnded = new();

        public Vector2 dragStartOffset { get; private set; }

        public LayerUI2(LayerNL3DBase layer, TemplateContainer newTemplateInstance)
        {
            Layer = layer;
            LayerUIElement = newTemplateInstance;
            EnabledToggle = newTemplateInstance.Q<Toggle>("EnabledToggle");
            ColorButton = newTemplateInstance.Q<Button>("ColorButton");
            TextLabel = newTemplateInstance.Q<Label>("LayerName");
            childrenContainer = newTemplateInstance.Q<VisualElement>("Content");

            FoldoutToggle.RegisterValueChangedCallback(OnFoldoutToggleValueChanged);

            UpdateLayerUI();

            LayerUIElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
            LayerUIElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            // LayerUIElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            Debug.Log("pointer down" + Layer.name);
            pointerDown = true;
            pointerDownPosition = evt.position;
            PointerDown.Invoke(this);
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!pointerDown)
            {
                return;
            }

            if (pointerDown && !Pointer.current.press.IsPressed())
            {
                Debug.Log("pointer up");
                pointerDown = false;
                return;
            }

            if ((evt.position - pointerDownPosition).sqrMagnitude < dragThreshold)
            {
                //thresold not exceeded to register as a drag, but pointerDown should keep its value
                Debug.Log("pointer down, but not dragging yet");
                return;
            }

            if (!IsDragging)
            {
                Debug.Log("start dragging");
                DragStarted.Invoke(this);
                IsDragging = true;
                dragStartOffset = evt.localPosition;
            }

            // Debug.Log("ectpos:" + evt.position);
        }

        public void StopDrag() //called in LayersUI because it should also stop dragging if the pointer is released while not on the current layer UI element
        {
            if (IsDragging)
            {
                Debug.Log("stop dragging");
                DragEnded.Invoke(this);
                IsDragging = false;
                pointerDown = false;
            }
        }


        private void OnFoldoutToggleValueChanged(ChangeEvent<bool> evt)
        {
            ToggleChildrenVisible(evt.newValue);
        }

        public void ToggleChildrenVisible(bool visible)
        {
            childrenContainer.visible = visible;
        }

        public void UpdateLayerUI()
        {
            EnabledToggle.value = Layer.LayerEnabled;
            ColorButton.style.backgroundColor = new StyleColor(Layer.Color);
            TextLabel.text = Layer.Name;
        }
    }
}