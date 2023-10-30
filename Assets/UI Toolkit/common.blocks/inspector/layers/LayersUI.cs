using System;
using System.Collections.Generic;
using System.Linq;
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

        private VisualElement layerListContainer;
        private VisualElement ghostElementContainer;

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
            layerListContainer = layerTool.InspectorInstance.Q<VisualElement>("LayerList");
            ghostElementContainer = layerTool.InspectorInstance.Q<VisualElement>("GhostElements");
            foreach (var layer in Layers)
            {
                AddLayerUI(layer);
                layer.LinkListeners();
            }
        }

        public LayerUI2 AddLayerUI(LayerNL3DBase layer)
        {
            var layerRowTemplate = layerAsset.Instantiate();
            layerListContainer.Add(layerRowTemplate);
            var ui = new LayerUI2(layer, layerRowTemplate);
            layer.UI = ui;

            ui.DragStarted.AddListener(OnLayerUIStartDrag);

            return ui;
        }


        private void OnLayerUIStartDrag(LayerUI2 layer)
        {
            draggingLayer = layer;
            InitializeDragContainer();
            DraggingLayerUIElementStyle();
        }

        private void InitializeDragContainer()
        {
            dragLayerElement = layerAsset.Instantiate();
            layerListContainer.Add(dragLayerElement);
        }

        private void DraggingLayerUIElementStyle()
        {
            // draggingLayer.LayerUIElement
        }

        private void Update()
        {
            if (draggingLayer != null)
            {
                CalculateDragElementPosition();
                var potentialNewParent = GetNewParentBasedOnDragPosition(draggingLayer.LayerUIElement.resolvedStyle.height / 2);
                
                ShowReorderLine(potentialNewParent == null, CalculateSiblingIndexFromDragElementPosition());

                if (!Pointer.current.press.IsPressed())
                {
                    // draggingLayer.Layer.SetParent(potentialNewParent.layout);
                    if (potentialNewParent != null)
                    {
                        print("reparenting");
                        var parentLayer = Layers.First(layer => layer.UI.LayerUIElement == potentialNewParent);
                        draggingLayer.Layer.SetParent(parentLayer);
                        potentialNewParent.style.backgroundColor = new StyleColor(Color.clear);
                        draggingLayer.UpdateLayerUI();
                    }
                    else
                    {
                        ReorderLayers();
                    }

                    ShowReorderLine(false);
                    OnDragEnded();
                }
            }
        }

        private void ShowReorderLine(bool show, int behindIndex = 0)
        {
            var reorderLine = ghostElementContainer.Q<VisualElement>("ReorderLine");
            reorderLine.transform.position = new Vector3(0, behindIndex * draggingLayer.LayerUIElement.resolvedStyle.height, 0);
            reorderLine.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ReorderLayers()
        {
            // var oldSiblingIndex = GetSiblingIndex(draggingLayer.LayerUIElement);
            var newSiblingIndex = CalculateSiblingIndexFromDragElementPosition();

            draggingLayer.LayerUIElement.PlaceBehind(draggingLayer.LayerUIElement.parent[newSiblingIndex]);
            // if (newSiblingIndex < oldSiblingIndex)
            //     draggingLayer.LayerUIElement.PlaceBehind(draggingLayer.LayerUIElement.parent[newSiblingIndex]);
            // else if (newSiblingIndex > oldSiblingIndex)
            //     draggingLayer.LayerUIElement.PlaceInFront(draggingLayer.LayerUIElement.parent[newSiblingIndex]);

            Layers = Layers.OrderBy(layer => GetSiblingIndex(layer.UI.LayerUIElement)).ToList();
        }

        private Vector2 CalculateDragElementPosition()
        {
            var mousePosition = Pointer.current.position.ReadValue();
            mousePosition.y = Screen.height - mousePosition.y;
            var mousePositionCorrected = RuntimePanelUtils.ScreenToPanel(dragLayerElement.parent.panel, mousePosition);
            var panelOffset = GetAbsolutePosition(draggingLayer.LayerUIElement);
            var invertedSiblingIndex = draggingLayer.LayerUIElement.parent.childCount - 1 - GetSiblingIndex(draggingLayer.LayerUIElement); // the parent already contains the drag container as a child, but this should not be included in the calculation
            var heightOffset = invertedSiblingIndex * draggingLayer.LayerUIElement.resolvedStyle.height;
            panelOffset.y += heightOffset;
            panelOffset += draggingLayer.dragStartOffset;
            var dragElementPosition = mousePositionCorrected - new Vector2(panelOffset.x, panelOffset.y);
            // dragLayerElement.transform.position = mousePositionCorrected - new Vector2(panelOffset.x, panelOffset.y);
            dragLayerElement.style.top = dragElementPosition.y;
            return dragElementPosition;
        }

        public VisualElement GetNewParentBasedOnDragPosition(float threshold)
        {
            var referencePosition = GetAbsolutePosition(dragLayerElement);
            referencePosition.y -= 0.75f * draggingLayer.LayerUIElement.resolvedStyle.height; //no idea why this should be 0.75
            referencePosition.y += draggingLayer.dragStartOffset.y;

            VisualElement newParent = null;
            for (int i = 0; i < draggingLayer.LayerUIElement.parent.childCount - 1; i++)
            {
                var sibling = draggingLayer.LayerUIElement.parent[i];
                sibling.style.backgroundColor = new StyleColor(Color.clear);
                var siblingPos = GetAbsolutePosition(sibling);
                if (referencePosition.y < siblingPos.y && Mathf.Abs(siblingPos.y - referencePosition.y) < threshold)
                {
                    sibling.style.backgroundColor = new StyleColor(Color.cyan);
                    newParent = sibling;
                    break;
                }
            }

            return newParent;
        }

        public int CalculateSiblingIndexFromDragElementPosition()
        {
            var referencePosition = GetAbsolutePosition(dragLayerElement);
            referencePosition.y -= 0.75f * draggingLayer.LayerUIElement.resolvedStyle.height; //no idea why this should be 0.75
            referencePosition.y += draggingLayer.dragStartOffset.y;
            
            // referencePosition.y -= draggingLayer.LayerUIElement.resolvedStyle.height;
            for (int i = 0; i < draggingLayer.LayerUIElement.parent.childCount; i++)
            {
                var siblingPos = GetAbsolutePosition(draggingLayer.LayerUIElement.parent[i]);
                // print(i + "\t" + siblingPos);
                if (referencePosition.y < siblingPos.y)
                    return i;
            }

            return draggingLayer.LayerUIElement.parent.childCount - 1; // minus 1 for the dragging element, and minus 1 because index is 0 based.
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

        private void OnDragEnded()
        {
            draggingLayer.StopDrag();
            draggingLayer = null;
            layerListContainer.Remove(dragLayerElement);
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
        public VisualElement IndentSpacer { get; }

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
            FoldoutToggle = newTemplateInstance.Q<Toggle>("FoldoutToggle");
            ColorButton = newTemplateInstance.Q<Button>("ColorButton");
            TextLabel = newTemplateInstance.Q<Label>("LayerName");
            childrenContainer = newTemplateInstance.Q<VisualElement>("Content");
            IndentSpacer = newTemplateInstance.Q<VisualElement>("IndentSpacer");

            FoldoutToggle.RegisterValueChangedCallback(OnFoldoutToggleValueChanged);

            LayerUIElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
            LayerUIElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            // LayerUIElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
            UpdateLayerUI();
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

            if (Layer.Parent)
            {
                Debug.Log(Layer.Parent);
                Layer.Parent.UI.childrenContainer.Add(LayerUIElement);
                IndentSpacer.style.width = Layer.Depth * FoldoutToggle.resolvedStyle.width;
            }
        }
    }
}