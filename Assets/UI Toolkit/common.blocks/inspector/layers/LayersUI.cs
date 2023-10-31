using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.Inpector.Layers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI.Inpector
{
    public class LayersUI : MonoBehaviour
    {
        // public static LayersUI Instance;
        public static List<LayerNL3DBase> AllLayers = new List<LayerNL3DBase>();

        public static List<LayerUI2> LayersVisibleInHierarchy = new List<LayerUI2>();
        public static List<LayerUI2> SelectedLayers = new();

        [SerializeField] private Tool layerTool;
        public Tool LayerTool => layerTool;
        [SerializeField] private string accordionId = "unity-content";
        [SerializeField] private VisualTreeAsset layerAsset;
        [SerializeField] private TileSystem.TileHandler tileHandler;

        private VisualElement layerListContainer;
        private VisualElement ghostElementContainer;

        private LayerUI2 draggingLayer;
        private VisualElement dragLayerElement;


        // private void Awake()
        // {
        //     Instance = this;
        // }

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
            foreach (var layer in AllLayers)
            {
                layer.UnLinkListeners();
            }
        }

        private void OnLayerToolActivated()
        {
            layerListContainer = layerTool.InspectorInstance.Q<VisualElement>("LayerList");
            ghostElementContainer = layerTool.InspectorInstance.Q<VisualElement>("GhostElements");
            foreach (var layer in AllLayers)
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
            LayersVisibleInHierarchy.Add(ui);
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

        private VisualElement a;
        private void Update()
        {
            if (a == null)
            {
                a = new();
                GetComponent<UIDocument>().rootVisualElement.Add(a);
            }
            // var mp = CalculateReferencePosition();
            a.name = "test";
            a.style.position = Position.Absolute;
            a.transform.position = Pointer.current.position.ReadValue();
            a.style.width = 10;
            a.style.height = 10;
            a.style.color = Color.green;
            
            if (draggingLayer != null)
            {
                CalculateDragElementPosition();
                var referencePosition = CalculateReferencePosition();
                var potentialNewParent = GetNewParentBasedOnDragPosition(referencePosition,draggingLayer.LayerUIElement.resolvedStyle.height / 2);

                ShowReorderLine(potentialNewParent == null, CalculateSiblingIndexFromDragElementPosition(referencePosition));

                if (!Pointer.current.press.IsPressed())
                {
                    // draggingLayer.Layer.SetParent(potentialNewParent.layout);
                    if (potentialNewParent != null)
                    {
                        var parentLayer = AllLayers.First(layer => layer.UI.LayerUIElement == potentialNewParent);
                        if (parentLayer != draggingLayer.Layer)
                        {
                            // print("reparenting");
                            // draggingLayer.Layer.SetParent(parentLayer);
                            // potentialNewParent.style.backgroundColor = StyleKeyword.Null;
                            // draggingLayer.UpdateLayerUI();
                        }
                    }
                    else
                    {
                        ReorderLayers(referencePosition);
                    }

                    ShowReorderLine(false);
                    OnDragEnded();
                }
            }

            //debug area
            // foreach (var l in AllLayers)
            // {
            //     print("--- " + l.name);
            //     print("parent: " + l.Parent);
            //     string c = "";
            //     foreach (var child in l.Children)
            //     {
            //         c += child.Name + "\t";
            //     }
            //
            //     print("children: " + c);
            //     print("---");
            // }

            // foreach (var layer in LayersVisibleInHierarchy)
            // {
            //     print(layer.Layer.name);
            // }
            //
            // print(LayersVisibleInHierarchy.Count);
        }

        private void ShowReorderLine(bool show, int behindIndex = 0)
        {
            var reorderLine = ghostElementContainer.Q<VisualElement>("ReorderLine");
            reorderLine.transform.position = new Vector3(0, behindIndex * draggingLayer.LayerUIElement.resolvedStyle.height, 0);
            reorderLine.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ReorderLayers(Vector2 referencePosition)
        {
            print("reordering layers");
            var newSiblingIndex = CalculateSiblingIndexFromDragElementPosition(referencePosition);
            // if(newSiblingIndex)
            draggingLayer.LayerUIElement.PlaceBehind(draggingLayer.LayerUIElement.parent[newSiblingIndex]);

            // AllLayers.Remove(draggingLayer.Layer);
            // var afterLayer = AllLayers.IndexOf(LayersVisibleInHierarchy[newSiblingIndex].Layer);
            // AllLayers.Insert(afterLayer, draggingLayer.Layer);
            AllLayers = AllLayers.OrderBy(layer => GetSiblingIndex(layer.UI.LayerUIElement)).ToList();

            //temp debug naming
            for (var index = 0; index < AllLayers.Count; index++)
            {
                var layer = AllLayers[index];
                if (int.TryParse(layer.name[0].ToString(), out var num))
                {
                    layer.Name = index + " " + layer.name.Substring(2, layer.name.Length - 2);
                }
                else
                {
                    layer.Name = index + " " + layer.Name;
                }

                layer.UI.UpdateLayerUI();
            }
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
            dragLayerElement.style.top = dragElementPosition.y;
            return dragElementPosition;
        }

        public VisualElement GetNewParentBasedOnDragPosition(Vector2 referencePosition, float threshold)
        {
            // var referencePosition = CalculateReferencePosition();

            VisualElement newParent = null;
            for (int i = 0; i < draggingLayer.LayerUIElement.parent.childCount - 1; i++)
            {
                var sibling = draggingLayer.LayerUIElement.parent[i];
                sibling.style.backgroundColor = StyleKeyword.Null;
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

        private Vector2 CalculateReferencePosition()
        {
            var referencePosition = GetAbsolutePosition(dragLayerElement);
            referencePosition.y -= 0.75f * draggingLayer.LayerUIElement.resolvedStyle.height; //no idea why this should be 0.75
            referencePosition.y += draggingLayer.dragStartOffset.y;
            return referencePosition;
            // referencePosition.y -= draggingLayer.LayerUIElement.resolvedStyle.height;
        }

        public int CalculateSiblingIndexFromDragElementPosition(Vector2 referencePosition)
        {
            // var referencePosition = CalculateReferencePosition();

            for (int i = 0; i < LayersVisibleInHierarchy.Count; i++)
            {
                var siblingPos = GetAbsolutePosition(LayersVisibleInHierarchy[i].LayerUIElement);
                if (referencePosition.y < siblingPos.y)
                {
                    print(i);
                    return i;
                }
            }

            print(LayersVisibleInHierarchy.Count -1);
            return LayersVisibleInHierarchy.Count -1;
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

        public static void SelectOrDeselectLayer(LayerUI2 layer, bool addToSelection)
        {
            if (!addToSelection)
            {
                foreach (var selectedLayer in SelectedLayers)
                {
                    selectedLayer.LayerUIElement.Q<VisualElement>("ParentRow").RemoveFromClassList("Selected");
                }

                SelectedLayers.Clear();
            }

            if (SelectedLayers.Contains(layer))
            {
                SelectedLayers.Remove(layer);
                layer.LayerUIElement.Q<VisualElement>("ParentRow").RemoveFromClassList("Selected");
            }
            else
            {
                SelectedLayers.Add(layer);
                layer.LayerUIElement.Q<VisualElement>("ParentRow").AddToClassList("Selected");
            }
        }

        public static void UpdateCurrentHierarchy(LayerNL3DBase changedLayer, bool childrenVisible)
        {
            foreach (var child in changedLayer.Children)
            {
                if (childrenVisible && !child.UI.IsVisible)
                {
                    var childIndex = LayersVisibleInHierarchy.IndexOf(changedLayer.UI);
                    LayersVisibleInHierarchy.Insert(childIndex, child.UI);
                }
                else if (!childrenVisible && child.UI.IsVisible)
                {
                    LayersVisibleInHierarchy.Remove(child.UI);
                }

                UpdateCurrentHierarchy(child, child.UI.FoldoutToggle.value);
            }
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

        public bool IsSelected => LayersUI.SelectedLayers.Contains(this);
        public bool IsVisible => LayersUI.LayersVisibleInHierarchy.Contains(this);

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
            newTemplateInstance.Q<VisualElement>("ParentRow").RegisterCallback<PointerEnterEvent>(OnPointerEnter);

            FoldoutToggle.RegisterValueChangedCallback(OnFoldoutToggleValueChanged);

            LayerUIElement.RegisterCallback<PointerDownEvent>(OnPointerDown);
            LayerUIElement.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            // LayerUIElement.RegisterCallback<PointerUpEvent>(OnPointerUp);

            UpdateLayerUI();
        }

        private void OnPointerEnter(PointerEnterEvent evt)
        {
            Debug.Log("pointer enteredn: " + Layer.name);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            Debug.Log("pointer down " + Layer.name);
            pointerDown = true;
            pointerDownPosition = evt.position;
            LayersUI.SelectOrDeselectLayer(this, GetAddToSelectionKeyIsPressed());
            PointerDown.Invoke(this);
        }

        private static bool GetAddToSelectionKeyIsPressed()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                return Keyboard.current.leftCommandKey.IsPressed() || Keyboard.current.rightCommandKey.IsPressed();
            }

            return Keyboard.current.leftCtrlKey.IsPressed() || Keyboard.current.rightCtrlKey.IsPressed();
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
            // childrenContainer.visible = visible;
            childrenContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            LayersUI.UpdateCurrentHierarchy(Layer, visible);
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