using System;
using System.Linq;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    public class LayerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler
    {
        public LayerNL3DBase Layer { get; set; }

        private RectTransform rectTransform;
        private VerticalLayoutGroup verticalLayoutGroup;

        private LayerManager layerManager;
        public Transform LayerBaseTransform => layerManager.LayerUIContainer;

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

        public LayerUI ParentUI { get; private set; }
        public LayerUI[] ChildrenUI { get; private set; } = Array.Empty<LayerUI>();

        private static LayerUI referenceLayerUnderMouse;
        private static LayerUI newParent;
        private int newSiblingIndex;

        private bool draggingLayerShouldBePlacedBeforeOtherLayer;
        private bool waitForFullClickToDeselect;

        public bool IsActiveInHierarchy
        {
            get
            {
                if (ParentUI)
                    return enabledToggle.isOn && ParentUI.IsActiveInHierarchy;
                return enabledToggle.isOn;
            }
        }

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
            enabledToggle.onValueChanged.AddListener(OnEnabledToggleValueChanged);
            foldoutToggle.onValueChanged.AddListener(OnFoldoutToggleValueChanged);
        }


        private void OnDisable()
        {
            enabledToggle.onValueChanged.RemoveListener(OnEnabledToggleValueChanged);
            foldoutToggle.onValueChanged.RemoveListener(OnFoldoutToggleValueChanged);
        }

        private void OnEnabledToggleValueChanged(bool isOn)
        {
            enabledToggle.interactable = !ParentUI || (ParentUI && ParentUI.IsActiveInHierarchy);

            Layer.IsActiveInScene = IsActiveInHierarchy;
            foreach (var child in ChildrenUI)
                child.OnEnabledToggleValueChanged(child.IsActiveInHierarchy);
        }

        private void OnFoldoutToggleValueChanged(bool isOn)
        {
            UpdateFoldout();
        }

        private void Start()
        {
            UpdateLayerUI();
            enabledToggle.SetIsOnWithoutNotify(Layer.IsActiveInScene); //initial update of if the toggle should be on or off. This should not be in UpdateLayerUI, because if a parent toggle is off, the child toggle could be on but then the layer would still not be active in the scene
        }

        public void SetParent(LayerUI newParent, int siblingIndex = -1)
        {
            if (newParent == this)
                return;

            var oldParent = ParentUI;

            if (newParent == null)
                transform.SetParent(LayerBaseTransform);
            else
                transform.SetParent(newParent.childrenPanel);
            
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

            OnEnabledToggleValueChanged(IsActiveInHierarchy);
        }

        private void RecalculateVisibleHierarchyRecursive()
        {
            LayerData.LayersVisibleInInspector.Clear();
            foreach (Transform unparentedLayer in LayerBaseTransform)
            {
                var ui = unparentedLayer.GetComponent<LayerUI>();
                ui.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
            }
        }

        private void RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers()
        {
            LayerData.LayersVisibleInInspector.Add(this);
            if (foldoutToggle.isOn)
            {
                foreach (var child in ChildrenUI)
                {
                    child.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
                }
            }

            UpdateLayerUI();
        }

        private void RecalculateParentAndChildren()
        {
            ParentUI = transform.parent.GetComponentInParent<LayerUI>(); // use transform.parent.GetComponentInParent to avoid getting the LayerUI on this gameObject
            ChildrenUI = childrenPanel.GetComponentsInChildren<LayerUI>();
        }

        private void RecalculateDepthValuesRecursively()
        {
            if (transform.parent != LayerBaseTransform)
                Depth = ParentUI.Depth + 1;
            else
                Depth = 0;

            foreach (var child in ChildrenUI)
            {
                child.RecalculateDepthValuesRecursively();
            }
        }

        public void UpdateLayerUI()
        {
            // UpdateEnabledToggle(); 
            UpdateName();
            RecalculateIndent();
            SetLayerTypeImage();
            var maxWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            RecalculateNameWidth(maxWidth);
            UpdateFoldout();
            Canvas.ForceUpdateCanvases();
        }

        private void SetLayerTypeImage()
        {
            var sprite = layerManager.GetLayerTypeSprite(Layer);
            layerTypeImage.sprite = sprite;
        }

        private void UpdateFoldout()
        {
            foldoutToggle.gameObject.SetActive(ChildrenUI.Length > 0);
            childrenPanel.gameObject.SetActive(foldoutToggle.isOn);

            RebuildChildrenPanelRecursive();
        }

        private void RebuildChildrenPanelRecursive()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(childrenPanel);
            if (ParentUI)
            {
                ParentUI.RebuildChildrenPanelRecursive();
            }
            else
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(LayerBaseTransform as RectTransform);
            }
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
            width -= spacer.rect.width;
            // width -= verticalLayoutGroup.spacing;
            width += parentRowRectTransform.GetComponent<HorizontalLayoutGroup>().padding.left;
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
            if (eventData.button == PointerEventData.InputButton.Left)
                OnLeftButtonDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Right)
                OnRightButtonDown(eventData);
        }

        private void OnLeftButtonDown(PointerEventData eventData)
        {
            layerManager.DragStartOffset = (Vector2)transform.position - eventData.position;
            referenceLayerUnderMouse = this;

            //if the layer under mouse is already selected, this can be the beginning of a drag, so don't deselect anything yet. wait for the pointer up event that is not a drag
            waitForFullClickToDeselect = false; //reset value to be sure no false positives are processed
            if (LayerData.SelectedLayers.Contains(this))
            {
                waitForFullClickToDeselect = true;
                return;
            }

            ProcessLayerSelection();
        }

        private void OnRightButtonDown(PointerEventData eventData)
        {
            referenceLayerUnderMouse = this;
            if (!LayerData.SelectedLayers.Contains(this))
            {
                ProcessLayerSelection();
            }

            layerManager.EnableContextMenu(true, eventData.position);
        }

        private void ProcessLayerSelection()
        {
            if (SequentialSelectionModifierKeyIsPressed() && LayerData.SelectedLayers.Count > 0) //if no layers are selected, there will be no reference layer to add to
            {
                // add all layers between the currently selected layer and the reference layer
                var referenceLayer = LayerData.SelectedLayers.Last(); //last element is always the last selected layer
                var myIndex = LayerData.LayersVisibleInInspector.IndexOf(this);
                var referenceIndex = LayerData.LayersVisibleInInspector.IndexOf(referenceLayer);

                var startIndex = referenceIndex > myIndex ? myIndex + 1 : referenceIndex + 1;
                var endIndex = referenceIndex > myIndex ? referenceIndex - 1 : myIndex - 1;

                var addLayers = !LayerData.SelectedLayers.Contains(this); //add or subtract layers?

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var newLayer = LayerData.LayersVisibleInInspector[i];

                    // print("add? " + addLayers + "\t" + newLayer.Layer.name);
                    if (addLayers && !LayerData.SelectedLayers.Contains(newLayer))
                    {
                        newLayer.Select();
                        // LayerManager.SelectedLayers.Add(newLayer);
                        // newLayer.Layer.OnSelect();
                        // newLayer.SetHighlight(InteractionState.Selected);
                    }
                    else if (!addLayers && LayerData.SelectedLayers.Contains(newLayer))
                    {
                        newLayer.Deselect();
                        // LayerManager.SelectedLayers.Remove(newLayer);
                        // newLayer.Layer.OnDeselect();
                        // newLayer.SetHighlight(InteractionState.Default);
                    }
                }

                if (!addLayers)
                {
                    referenceLayer.Deselect();
                    // LayerManager.SelectedLayers.Remove(referenceLayer);
                    // referenceLayer.Layer.OnDeselect();
                    // referenceLayer.SetHighlight(InteractionState.Default);

                    Deselect();
                }
            }

            if (!AddToSelectionModifierKeyIsPressed() && !SequentialSelectionModifierKeyIsPressed())
                LayerManager.DeselectAllLayers();

            if (LayerData.SelectedLayers.Contains(this))
            {
                Deselect();
            }
            else
            {
                Select();
            }
        }

        public void Deselect()
        {
            LayerData.SelectedLayers.Remove(this);
            Layer.OnDeselect();
            SetHighlight(InteractionState.Default);
        }

        public void Select(bool deselectOthers = false)
        {
            if(deselectOthers)
                LayerManager.DeselectAllLayers();
            
            LayerData.SelectedLayers.Add(this);
            Layer.OnSelect();
            SetHighlight(InteractionState.Selected);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (waitForFullClickToDeselect)
            {
                ProcessLayerSelection();
            }

            waitForFullClickToDeselect = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            layerManager.StartDragLayer(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!layerManager.IsDragOnButton()) //don't reparent when dragging on a button, since the button action should be called instead and handle any possible reparents
            {
                LayerManager.SortSelectedLayersByVisibility();
                LayerData.SelectedLayers.Reverse();

                foreach (var selectedLayer in LayerData.SelectedLayers)
                {
                    selectedLayer.SetParent(newParent, newSiblingIndex);
                }
            }

            RemoveHoverHighlight(referenceLayerUnderMouse);

            layerManager.EndDragLayer();
        }

        public void OnDrop(PointerEventData eventData)
        {
            // if (eventData.button != PointerEventData.InputButton.Left)
            //     return;
            //
            // LayerManager.SortSelectedLayersByVisibility();
            // LayerManager.SelectedLayers.Reverse();
            // foreach (var selectedLayer in LayerManager.SelectedLayers)
            // {
            //     selectedLayer.SetParent(newParent, newSiblingIndex);
            // }

            // RemoveHoverHighlight(layerUnderMouse);
            //
            // layerManager.EndDragLayer();
        }

        public void OnDrag(PointerEventData eventData) //has to be here or OnBeginDrag and OnEndDrag won't work
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            RemoveHoverHighlight(referenceLayerUnderMouse);

            // print("dragging: " + Layer.name);
            referenceLayerUnderMouse = CalculateLayerUnderMouse(out float relativeYValue);
            if (referenceLayerUnderMouse)
            {
                var hoverTransform = referenceLayerUnderMouse.rectTransform; // as RectTransform;
                var correctedSize = (hoverTransform.rect.size - referenceLayerUnderMouse.childrenPanel.rect.size) * hoverTransform.lossyScale;
                var relativeValue = -relativeYValue / correctedSize.y;
                var yValue01 = Mathf.Clamp01(relativeValue);

                if (yValue01 < 0.25f)
                {
                    // print("higher");
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, referenceLayerUnderMouse.transform.position.y);

                    newParent = referenceLayerUnderMouse.ParentUI;
                    newSiblingIndex = referenceLayerUnderMouse.transform.GetSiblingIndex();
                    
                    if (newSiblingIndex > transform.GetSiblingIndex()) //account for self being included
                        newSiblingIndex--;
                }
                else if (yValue01 > 0.75f)
                {
                    // print("lower");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, referenceLayerUnderMouse.transform.position.y) - new Vector2(0, correctedSize.y);

                    //edge case: if the reorder is between layerUnderMouse, and between layerUnderMouse and child 0 of layerUnderMouse, the new parent should be the layerUnderMouse instead of the layerUnderMouse's parent 
                    if (referenceLayerUnderMouse.ChildrenUI.Length > 0 && referenceLayerUnderMouse.foldoutToggle.isOn)
                    {
                        newParent = referenceLayerUnderMouse;
                        newSiblingIndex = 0;
                    }
                    else
                    {
                        newParent = referenceLayerUnderMouse.ParentUI;
                        newSiblingIndex = referenceLayerUnderMouse.transform.GetSiblingIndex() + 1;
                        if (newSiblingIndex > transform.GetSiblingIndex()) //account for self being included
                            newSiblingIndex--;
                    }
                }
                else
                {
                    // print("reparent");
                    referenceLayerUnderMouse.SetHighlight(InteractionState.Hover);
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(false);

                    newParent = referenceLayerUnderMouse;
                    newSiblingIndex = referenceLayerUnderMouse.childrenPanel.childCount;
                }

                //if mouse is fully to the bottom, set parent to null
                if (relativeValue > 1)
                {
                    newParent = null;
                    newSiblingIndex = LayerBaseTransform.childCount;
                }
            }

            // if (referenceLayerUnderMouse)
            //     print("ref: " + referenceLayerUnderMouse.Layer.name);
            // else
            //     print("ref: null");
            //
            // if (newParent)
            //     print("newparent: " + newParent.Layer.name);
            // else
            //     print("newparent: null");
        }

        private static void RemoveHoverHighlight(LayerUI layer)
        {
            if (layer)
            {
                var state = InteractionState.Default;
                if (LayerData.SelectedLayers.Contains(layer))
                    state = InteractionState.Selected;
                layer.SetHighlight(state);
            }
        }

        public void SetHighlight(InteractionState state)
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
            var ghostRectTransform = layerManager.DragGhost.GetComponent<RectTransform>();
            var ghostSize = ghostRectTransform.rect.size * ghostRectTransform.lossyScale;
            var mousePos = (Vector2)layerManager.DragGhost.transform.position - ghostSize / 2;

            for (var i = LayerData.LayersVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = LayerData.LayersVisibleInInspector[i];
                var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
                if (mousePos.y < layer.rectTransform.position.y && mousePos.y >= layer.rectTransform.position.y - correctedSize.y)
                {
                    relativeYValue = mousePos.y - layer.rectTransform.position.y;
                    return layer;
                }
            }

            var firstLayer = LayerData.LayersVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                relativeYValue = mousePos.y - firstLayer.rectTransform.position.y;
                return firstLayer; //above first
            }

            var lastLayer = LayerData.LayersVisibleInInspector.Last();
            relativeYValue = mousePos.y - lastLayer.rectTransform.position.y;
            return lastLayer; //below last
        }

        public static bool AddToSelectionModifierKeyIsPressed()
        {
            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
            {
                return Keyboard.current.leftCommandKey.isPressed || Keyboard.current.rightCommandKey.isPressed;
            }

            return Keyboard.current.ctrlKey.isPressed;
        }

        public static bool SequentialSelectionModifierKeyIsPressed()
        {
            return Keyboard.current.shiftKey.isPressed;
        }
    }
}