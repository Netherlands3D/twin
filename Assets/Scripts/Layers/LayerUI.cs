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

    public class LayerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
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

        public bool IsActiveInHierarchy
        {
            get
            {
                if (parentUI)
                    return enabledToggle.isOn && parentUI.IsActiveInHierarchy;
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
            enabledToggle.interactable = !parentUI || (parentUI && parentUI.IsActiveInHierarchy);

            Layer.IsActiveInScene = IsActiveInHierarchy;
            foreach (var child in childrenUI)
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

            var oldParent = parentUI;

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
            LayerManager.LayersVisibleInInspector.Clear();
            foreach (Transform unparentedLayer in LayerBaseTransform)
            {
                var ui = unparentedLayer.GetComponent<LayerUI>();
                ui.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
            }
        }

        private void RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers()
        {
            LayerManager.LayersVisibleInInspector.Add(this);
            if (foldoutToggle.isOn)
            {
                foreach (var child in childrenUI)
                {
                    child.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
                }
            }

            UpdateLayerUI();
        }

        private void RecalculateParentAndChildren()
        {
            parentUI = transform.parent.GetComponentInParent<LayerUI>(); // use transform.parent.GetComponentInParent to avoid getting the LayerUI on this gameObject
            childrenUI = childrenPanel.GetComponentsInChildren<LayerUI>();
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
        }

        public void UpdateLayerUI()
        {
            // UpdateEnabledToggle(); 
            UpdateName();
            RecalculateIndent();
            var maxWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            RecalculateNameWidth(maxWidth);
            UpdateFoldout();
            Canvas.ForceUpdateCanvases();
        }

        private void UpdateFoldout()
        {
            foldoutToggle.gameObject.SetActive(childrenUI.Length > 0);
            childrenPanel.gameObject.SetActive(foldoutToggle.isOn);

            RebuildChildrenPanelRecursive();
        }

        private void RebuildChildrenPanelRecursive()
        {
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
            layerManager.DragStartOffset = (Vector2)transform.position - eventData.position;
            layerUnderMouse = this;

            //if the layer under mouse is already selected, this can be the beginning of a drag, so don't deselect anything yet. wait for the pointer up event that is not a drag
            waitForFullClickToDeselect = false; //reset value to be sure no false positives are processed
            if (LayerManager.SelectedLayers.Contains(this))
            {
                waitForFullClickToDeselect = true;
                return;
            }

            ProcessLayerSelection();
        }

        private void ProcessLayerSelection()
        {
            if (SequentialSelectionModifierKeyIsPressed() && LayerManager.SelectedLayers.Count > 0) //if no layers are selected, there will be no reference layer to add to
            {
                // add all layers between the currently selected layer and the reference layer
                var referenceLayer = LayerManager.SelectedLayers.Last(); //last element is always the last selected layer
                var myIndex = LayerManager.LayersVisibleInInspector.IndexOf(this);
                var referenceIndex = LayerManager.LayersVisibleInInspector.IndexOf(referenceLayer);

                var startIndex = referenceIndex > myIndex ? myIndex + 1 : referenceIndex + 1;
                var endIndex = referenceIndex > myIndex ? referenceIndex - 1 : myIndex - 1;

                var addLayers = !LayerManager.SelectedLayers.Contains(this); //add or subtract layers?

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var newLayer = LayerManager.LayersVisibleInInspector[i];

                    print("add? " + addLayers + "\t" + newLayer.Layer.name);
                    if (addLayers && !LayerManager.SelectedLayers.Contains(newLayer))
                    {
                        LayerManager.SelectedLayers.Add(newLayer);
                        newLayer.SetHighlight(InteractionState.Selected);
                    }
                    else if (!addLayers && LayerManager.SelectedLayers.Contains(newLayer))
                    {
                        LayerManager.SelectedLayers.Remove(newLayer);
                        newLayer.SetHighlight(InteractionState.Default);
                    }
                }

                if (!addLayers)
                {
                    LayerManager.SelectedLayers.Remove(referenceLayer);
                    referenceLayer.SetHighlight(InteractionState.Default);

                    LayerManager.SelectedLayers.Remove(this);
                    SetHighlight(InteractionState.Default);
                }
            }

            if (!AddToSelectionModifierKeyIsPressed() && !SequentialSelectionModifierKeyIsPressed())
                LayerManager.DeselectAllLayers();

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (waitForFullClickToDeselect)
            {
                ProcessLayerSelection();
            }

            waitForFullClickToDeselect = false;
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
                    // print("higher");
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, layerUnderMouse.transform.position.y);

                    newParent = layerUnderMouse.parentUI;
                    newSiblingIndex = layerUnderMouse.transform.GetSiblingIndex();
                }
                else if (yValue01 > 0.75f)
                {
                    // print("lower");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    var correctedSize = hoverTransform.rect.size * hoverTransform.lossyScale;
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, layerUnderMouse.transform.position.y) - new Vector2(0, correctedSize.y);

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

            for (var i = LayerManager.LayersVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = LayerManager.LayersVisibleInInspector[i];
                var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
                if (mousePos.y < layer.rectTransform.position.y && mousePos.y >= layer.rectTransform.position.y - correctedSize.y)
                {
                    relativeYValue = mousePos.y - layer.rectTransform.position.y;
                    return layer;
                }
            }

            var firstLayer = LayerManager.LayersVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                relativeYValue = mousePos.y - firstLayer.rectTransform.position.y;
                return firstLayer; //above first
            }

            var lastLayer = LayerManager.LayersVisibleInInspector.Last();
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