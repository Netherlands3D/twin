using System;
using System.Linq;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public enum InteractionState
    {
        Default,
        Hover,
        Selected
    }

    public class LayerUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public LayerNL3DBase Layer { get; set; }

        private RectTransform rectTransform;
        private VerticalLayoutGroup verticalLayoutGroup;

        private LayerManager layerManager;
        public Transform LayerBaseTransform => layerManager.transform;

        [SerializeField] private RectTransform parentRowRectTransform;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button colorButton;
        [SerializeField] private RectTransform spacer;
        [SerializeField] private float indentWidth = 40f;
        [SerializeField] private Toggle foldoutToggle;
        [SerializeField] private Image layerTypeImage;
        [SerializeField] private TMP_Text layerNameText;
        [SerializeField] private TMP_InputField layerNameField;
        [SerializeField] private RectTransform childrenPanel;

        [SerializeField] private TMP_Text debugIndexText;

        private LayerUI parentUI;
        private LayerUI[] childrenUI = Array.Empty<LayerUI>();

        private static LayerUI layerUnderMouse;
        private LayerUI newParent;
        private int newSiblingIndex;

        private bool draggingLayerShouldBePlacedBeforeOtherLayer;
        private bool waitForFullClickToDeselect;

        public Color Color { get; set; } = Color.blue;
        public Sprite Icon { get; set; }
        public int Depth { get; private set; } = 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            layerManager = GetComponentInParent<LayerManager>();
        }

        private void OnEnable()
        {
            foldoutToggle.onValueChanged.AddListener(OnFoldoutToggleValueChanged);
        }

        private void OnDisable()
        {
            foldoutToggle.onValueChanged.RemoveListener(OnFoldoutToggleValueChanged);
        }

        private bool test;

        private void OnFoldoutToggleValueChanged(bool isOn)
        {
            UpdateFoldout();
            // childrenPanel.gameObject.SetActive(isOn);
            // LayoutRebuilder.ForceRebuildLayoutImmediate(LayerBaseTransform as RectTransform);
            // LayoutRebuilder.ForceRebuildLayoutImmediate(childrenPanel as RectTransform);
            // test = true;
        }

        private void LateUpdate()
        {
            if (test)
            {
                Canvas.ForceUpdateCanvases();
                test = false;
            }
        }

        private void Start()
        {
            UpdateLayerUI();
        }

        public void SetParent(LayerUI newParent, int siblingIndex = -1)
        {
            if (newParent == this)
                return;

            var oldParent = parentUI;
            var childCountBeforeChange = newParent ? newParent.childrenPanel.childCount : LayerBaseTransform.childCount;

            if (newParent == null)
            {
                // print(Layer.name + " new parent null");
                transform.SetParent(LayerBaseTransform);
            }
            else
            {
                transform.SetParent(newParent.childrenPanel);
            }

            var childCountAfterChange = newParent ? newParent.childrenPanel.childCount : LayerBaseTransform.childCount;

            var parentChanged = oldParent ? transform.parent != oldParent.childrenPanel : transform.parent != LayerBaseTransform;
            var reorderWithSameParent = oldParent == newParent;
            if (parentChanged || reorderWithSameParent) //if reparent fails, the new siblingIndex is also invalid
                transform.SetSiblingIndex(siblingIndex);

            if (oldParent)
            {
                oldParent.RecalculateParentAndChildren();
            }

            if (newParent)
            {
                newParent.RecalculateParentAndChildren();
                newParent.foldoutToggle.isOn = true;
            }

            RecalculateParentAndChildren();

            RecalculateDepthValuesRecursively();
            RecalculateVisibleHierarchyRecursive();
        }

        private void RecalculateVisibleHierarchyRecursive()
        {
            LayerManager.LayersVisibleInInspector.Clear();
            foreach (Transform unparentedLayer in LayerBaseTransform)
            {
                var ui = unparentedLayer.GetComponent<LayerUI>();
                // print("recalculating unparented layer: " + ui.Layer.name);
                ui.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
            }
        }

        private void RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers()
        {
            // print("adding " + Layer.name + " to list at index " + LayerManager.LayersVisibleInInspector.Count);
            LayerManager.LayersVisibleInInspector.Add(this);
            if (foldoutToggle.isOn)
            {
                // print("parent " + Layer.name + " is enabled");
                foreach (var child in childrenUI)
                {
                    // print("child: " + child.Layer.name);
                    child.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
                }
            }

            UpdateLayerUI();
        }

        private void RecalculateParentAndChildren()
        {
            parentUI = transform.parent.GetComponentInParent<LayerUI>(); // use transform.parent.GetComponentInParent to avoid getting the LayerUI on this gameObject
            childrenUI = childrenPanel.GetComponentsInChildren<LayerUI>();
            // if (parentUI)
            //     print(Layer.name + " has parent " + parentUI.Layer.name + " and " + childrenUI.Length + " children");
            // else
            //     print(Layer.name + " has no parent and " + childrenUI.Length + " children");
        }


        private void RecalculateDepthValuesRecursively()
        {
            if (transform.parent != LayerBaseTransform)
                Depth = parentUI.Depth + 1;
            else
                Depth = 0;

            foreach (var child in childrenUI)
            {
                child.RecalculateDepthValuesRecursively();
            }
            // UpdateLayerUI();
        }

        public void UpdateLayerUI()
        {
            // print("updating ui of: " + Layer.name);

            UpdateName();
            var maxWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            RecalculateNameWidth(maxWidth);
            RecalculateIndent();
            UpdateFoldout();
            debugIndexText.text = "Vi: " + LayerManager.LayersVisibleInInspector.IndexOf(this) + "\nSi: " + transform.GetSiblingIndex();
            // print("Vi: " + LayerManager.LayersVisibleInInspector.IndexOf(this) + "\nSi: " + transform.GetSiblingIndex());
            Canvas.ForceUpdateCanvases();
            // LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform); //not sure why it is needed to manually force a canvas update
            // LayoutRebuilder.MarkLayoutForRebuild(childrenPanel as RectTransform); //not sure why it is needed to manually force a canvas update
            // LayoutRebuilder.ForceRebuildLayoutImmediate(layerManager.transform as RectTransform); //not sure why it is needed to manually force a canvas update
        }

        private void UpdateFoldout()
        {
            foldoutToggle.gameObject.SetActive(childrenUI.Length > 0);
            childrenPanel.gameObject.SetActive(foldoutToggle.isOn);

            RebuildChildrenPanelRecursive();
        }

        private void RebuildChildrenPanelRecursive()
        {
            // print("rebuilding child panel of: " + Layer.name);
            LayoutRebuilder.ForceRebuildLayoutImmediate(childrenPanel);
            if (parentUI)
            {
                parentUI.RebuildChildrenPanelRecursive();
            }
            else
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(LayerBaseTransform as RectTransform);
            }
        }

        private void RecalculateLayersVisibleInHierarchyAfterReparent(LayerUI changingLayer, LayerUI newParent, int newSiblingIndex)
        {
            LayerManager.LayersVisibleInInspector.Remove(changingLayer);
            var newIndex = 0;
            if (newParent)
                newIndex += LayerManager.LayersVisibleInInspector.IndexOf(newParent) + 1;

            print("new parentIndex" + newIndex);
            print("new sibling index " + newSiblingIndex);
            newIndex += newSiblingIndex;
            print("new total index " + newIndex);

            if (newIndex < LayerManager.LayersVisibleInInspector.Count)
                LayerManager.LayersVisibleInInspector.Insert(newIndex, changingLayer);
            else
                LayerManager.LayersVisibleInInspector.Add(changingLayer);

            string st = changingLayer.Layer.name;
            for (var index = 0; index < LayerManager.LayersVisibleInInspector.Count; index++)
            {
                var l = LayerManager.LayersVisibleInInspector[index];
                st += " - " + index + l.Layer.name;
            }

            print(st);

            return;

            // var parentIndex = 0;
            // if (changingLayer.Layer.Parent)
            //     parentIndex += LayerManager.LayersVisibleInInspector.IndexOf(changingLayer.Layer.Parent.UI);


            var relativeToIndex = LayerManager.LayersVisibleInInspector.IndexOf(layerUnderMouse);

            var newVisibleIndex = newSiblingIndex + relativeToIndex + 1;
            // var newVisibleIndex = draggingLayerShouldBePlacedBeforeOtherLayer ? relativeToIndex : relativeToIndex + 1;
            print(changingLayer.Layer.name + "\t" + layerUnderMouse.Layer.name + "\t" + draggingLayerShouldBePlacedBeforeOtherLayer + "\t" + newVisibleIndex);
            LayerManager.LayersVisibleInInspector.Insert(newVisibleIndex, changingLayer);
            // LayerManager.DraggingLayer.transform.SetSiblingIndex(newSiblingIndex);

            // var newIndex = parentIndex + changingLayer.transform.GetSiblingIndex() + 1; //draggingLayerShouldBePlacedBeforeOtherLayer ? relativeToIndex : relativeToIndex + 1;
            // print("Visible index of dragged layer after reparent\t" + newIndex);
            // LayerManager.LayersVisibleInInspector.Insert(newIndex, changingLayer);
            // LayerManager.DraggingLayer.transform.SetSiblingIndex(newIndex);

            // foreach (Transform child in transform) //recursively recalculate indices of all changed children
            // {
            //     RecalculateLayersVisibleInHierarchyAfterReparent(child.GetComponent<LayerUI>(), child.GetSiblingIndex());
            // }

            string s = changingLayer.Layer.name;
            for (var index = 0; index < LayerManager.LayersVisibleInInspector.Count; index++)
            {
                var l = LayerManager.LayersVisibleInInspector[index];
                s += " - " + index + l.Layer.name;
            }

            print(s);
        }

        private void RecalculateIndent()
        {
            var spacerRectTransform = spacer.transform as RectTransform;
            spacerRectTransform.sizeDelta = new Vector2(Depth * indentWidth, spacerRectTransform.sizeDelta.y);
        }

        private void UpdateName()
        {
            layerNameText.text = Layer.name;
            layerNameField.text = Layer.name;
        }

        private void RecalculateNameWidth(float maxWidth)
        {
            var layerNameFieldRectTransform = layerNameField.GetComponent<RectTransform>();
            var width = maxWidth;
            width -= layerNameFieldRectTransform.anchoredPosition.x;
            width -= verticalLayoutGroup.spacing;
            width -= parentRowRectTransform.GetComponent<HorizontalLayoutGroup>().padding.right;
            layerNameFieldRectTransform.sizeDelta = new Vector2(width, layerNameFieldRectTransform.rect.height);
            layerNameText.GetComponent<RectTransform>().sizeDelta = layerNameFieldRectTransform.sizeDelta;
        }

        private void RecalculateChildPanelHeight()
        {
            var layerHeight = GetComponent<RectTransform>().rect.height;
            childrenPanel.SetHeight(transform.childCount * layerHeight);
        }


        public void OnPointerDown(PointerEventData eventData)
        {
            layerManager.DragStartOffset = (Vector2)transform.position - eventData.position;
            layerUnderMouse = this; //CalculateLayerUnderMouse(out _);

            //if the layer under mouse is already selected, this can be the beginning of a drag, so don't deselect anything yet. wait for the pointer up event that is not a drag
            waitForFullClickToDeselect = false; //reset value to be sure no false positives are processed
            if (LayerManager.SelectedLayers.Contains(this))
            {
                waitForFullClickToDeselect = true;
                // print("maybe start drag");
                return;
            }

            ProcessLayerSelection();
        }

        private void ProcessLayerSelection()
        {
            if (!AddToSelectionModifierKeyIsPressed())
                ResetSelectedLayers();

            if (LayerManager.SelectedLayers.Contains(this))
            {
                LayerManager.SelectedLayers.Remove(this);
                SetHighlight(InteractionState.Default);
            }
            else
            {
                LayerManager.SelectedLayers.Add(this);
                SetHighlight(InteractionState.Selected);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // print("onpointerup");
            // waitForFullClickToDeselect = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // print("processing selection after click: " + waitForFullClickToDeselect);
            if (waitForFullClickToDeselect)
            {
                ProcessLayerSelection();
            }
            
            waitForFullClickToDeselect = false;
        }

        private void ResetSelectedLayers()
        {
            foreach (var selectedLayer in LayerManager.SelectedLayers)
            {
                selectedLayer.SetHighlight(InteractionState.Default);
            }
            LayerManager.SelectedLayers.Clear();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            layerManager.StartDragLayer(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            print("onenddrag");
            foreach (var selectedLayer in LayerManager.SelectedLayers)
            {
                selectedLayer.SetParent(newParent, newSiblingIndex);
            }

            RemoveHoverHighlight(layerUnderMouse);

            layerManager.EndDragLayer();
        }

        public void OnDrag(PointerEventData eventData) //has to be here or OnBeginDrag and OnEndDrag won't work
        {
            RemoveHoverHighlight(layerUnderMouse);
            
            layerUnderMouse = CalculateLayerUnderMouse(out float relativeYValue);
            print(layerUnderMouse.Layer.name);
            // layerUnderMouse.SetHighlight(layerUnderMouse, InteractionState.Hover);
            // print(Layer.name + "\t" + layerUnderMouse.Layer.name);
            if (layerUnderMouse)
            {
                var hoverTransform = layerUnderMouse.rectTransform; // as RectTransform;
                var correctedSize2 = (hoverTransform.rect.size - layerUnderMouse.childrenPanel.rect.size) * hoverTransform.lossyScale;
                var relativeValue = -relativeYValue / correctedSize2.y;
                var yValue01 = Mathf.Clamp01(relativeValue);

                if (yValue01 < 0.25f)
                {
                    print("higher");
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, layerUnderMouse.transform.position.y);
                    // ReorderLayers(LayerManager.OverLayer, false);

                    newParent = layerUnderMouse.parentUI;
                    newSiblingIndex = layerUnderMouse.transform.GetSiblingIndex();
                }
                else if (yValue01 > 0.75f)
                {
                    print("lower");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    var correctedSize = hoverTransform.rect.size * hoverTransform.lossyScale;
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, layerUnderMouse.transform.position.y) - new Vector2(0, correctedSize.y);
                    // ReorderLayers(LayerManager.OverLayer, true);

                    //edge case: if the reorder is between layerUnderMouse, and between layerUnderMouse and child 0 of layerUnderMouse, the new parent should be the layerUnderMouse instead of the layerUnderMouse's parent 
                    if (layerUnderMouse.childrenUI.Length > 0 && layerUnderMouse.foldoutToggle.isOn)
                    {
                        newParent = layerUnderMouse;
                        newSiblingIndex = 0;
                    }
                    else
                    {
                        newParent = layerUnderMouse.parentUI;
                        newSiblingIndex = layerUnderMouse.transform.GetSiblingIndex() + 1;
                    }
                }
                else
                {
                    // print("reparent");
                    layerUnderMouse.SetHighlight(InteractionState.Hover);
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(false);

                    newParent = layerUnderMouse;
                    newSiblingIndex = layerUnderMouse.childrenPanel.childCount;
                }

                //if mouse is fully to the bottom, set parent to null
                if (relativeValue > 1)
                {
                    // print("newparent null, new child index " + LayerBaseTransform.childCount);
                    newParent = null;
                    newSiblingIndex = LayerBaseTransform.childCount;
                }
            }
        }

        private static void RemoveHoverHighlight(LayerUI layer)
        {
            if (layer)
            {
                var state = InteractionState.Default;
                if (LayerManager.SelectedLayers.Contains(layer))
                    state = InteractionState.Selected;
                layer.SetHighlight(state);
            }
        }

        private void SetHighlight(InteractionState state)
        {
            switch (state)
            {
                case InteractionState.Default:
                    GetComponentInChildren<Image>().color = Color.red;
                    break;
                case InteractionState.Hover:
                    GetComponentInChildren<Image>().color = Color.cyan;
                    break;
                case InteractionState.Selected:
                    GetComponentInChildren<Image>().color = Color.blue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private LayerUI CalculateLayerUnderMouse(out float relativeYValue)
        {
            // var mousePos = Pointer.current.position.ReadValue();
            var ghostRectTransform = layerManager.DragGhost.GetComponent<RectTransform>();
            var ghostSize = ghostRectTransform.rect.size * ghostRectTransform.lossyScale;
            var mousePos = (Vector2)layerManager.DragGhost.transform.position - ghostSize / 2;

            for (var i = LayerManager.LayersVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = LayerManager.LayersVisibleInInspector[i];
                var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
                if (mousePos.y < layer.rectTransform.position.y && mousePos.y >= layer.rectTransform.position.y - correctedSize.y)
                {
                    // print("between " + layer.Layer.name);
                    relativeYValue = mousePos.y - layer.rectTransform.position.y;
                    return layer;
                }
            }

            var firstLayer = LayerManager.LayersVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                // print("above first");
                relativeYValue = mousePos.y - firstLayer.rectTransform.position.y;
                return firstLayer; //above first
            }

            // print("below last");
            var lastLayer = LayerManager.LayersVisibleInInspector.Last();
            relativeYValue = mousePos.y - lastLayer.rectTransform.position.y;
            return lastLayer; //below last
        }

        private bool AddToSelectionModifierKeyIsPressed()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                return Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed;
            }

            return Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
        }
    }
}