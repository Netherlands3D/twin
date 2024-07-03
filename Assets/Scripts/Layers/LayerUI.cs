using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public enum InteractionState
    {
        Default,
        Hover,
        DragHover,
        Selected
    }

    public enum LayerActiveState
    {
        Enabled = 0,
        Disabled = 1,
        Mixed = 2,
        EnabledInDisabled = 3
    }

    public class LayerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public LayerNL3DBase Layer { get; set; }

        private RectTransform rectTransform;
        private LayerManager layerManager;
        public Transform LayerBaseTransform => layerManager.LayerUIContainer;

        private VerticalLayoutGroup childVerticalLayoutGroup;
        [SerializeField] private RectTransform parentRowRectTransform;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button colorButton;
        [SerializeField] private RectTransform spacer;
        private float spacerStartWidth;
        [SerializeField] private float indentWidth = 40f;
        [SerializeField] private Toggle foldoutToggle;
        [SerializeField] private Image layerTypeImage;
        [SerializeField] private TMP_Text layerNameText;
        [SerializeField] private TMP_InputField layerNameField;
        [SerializeField] private RectTransform childrenPanel;

        [SerializeField] private TMP_Text debugIndexText;
        [SerializeField] private Sprite[] visibilitySprites;
        [SerializeField] private Sprite[] foldoutSprites;
        [SerializeField] private Sprite[] backgroundSprites;
        [SerializeField] private Toggle propertyToggle;

        private LayerUI ParentUI;
        private LayerUI[] ChildrenUI = Array.Empty<LayerUI>();

        private static LayerUI referenceLayerUnderMouse;
        private static LayerUI newParent;
        private int newSiblingIndex;

        private bool draggingLayerShouldBePlacedBeforeOtherLayer;
        private bool waitForFullClickToDeselect;
        private bool isDirty;

        public LayerActiveState State { get; set; }
        public InteractionState InteractionState { get; set; }

        public Sprite VisibilitySprite => visibilitySprites[(int)State];

        public bool hasChildren => childrenPanel.childCount > 0;
        public Sprite LayerTypeSprite => layerTypeImage.sprite;
        public string LayerName => Layer.Name;
        public bool PropertiesOpen => propertyToggle.isOn;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            layerManager = GetComponentInParent<LayerManager>();
            childVerticalLayoutGroup = childrenPanel.GetComponent<VerticalLayoutGroup>();
            spacerStartWidth = spacer.sizeDelta.x;
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

            Layer.LayerActiveInHierarchyChanged.RemoveListener(UpdateEnabledToggle);
            Layer.ColorChanged.RemoveListener(SetColor);
            Layer.LayerDestroyed.RemoveListener(DestroyUI);
            Layer.LayerSelected.RemoveListener(OnLayerSelected);
            Layer.LayerDeselected.RemoveListener(OnLayerDeselected);
        }

        public void RecalculateCurrentTreeStates()
        {
            RecalculateState();
            RecalculateChildrenStates();
            RecalculateParentStates();
        }

        private void RecalculateParentStates()
        {
            if (ParentUI)
            {
                ParentUI.RecalculateState();
                ParentUI.RecalculateParentStates();
            }
        }

        private void RecalculateChildrenStates()
        {
            foreach (var child in ChildrenUI)
            {
                child.RecalculateState();
                child.RecalculateChildrenStates();
            }
        }

        private void OnEnabledToggleValueChanged(bool isOn)
        {
            Layer.ActiveSelf = isOn;
            // RecalculateCurrentTreeStates();
            // SetEnabledToggleInteractiveStateRecursive();
        }

        public void SetEnabledToggleInteractiveStateRecursive()
        {
            enabledToggle.interactable = !ParentUI || (ParentUI && Layer.ParentLayer.ActiveInHierarchy);

            foreach (var child in ChildrenUI)
                child.SetEnabledToggleInteractiveStateRecursive();
        }

        public void UpdateEnabledToggle(bool isOn)
        {
            enabledToggle.SetIsOnWithoutNotify(isOn);
            RecalculateCurrentTreeStates();
            SetEnabledToggleInteractiveStateRecursive();
        }

        private void SetVisibilitySprite()
        {
            debugIndexText.text = State.ToString();
            enabledToggle.targetGraphic.GetComponent<Image>().sprite = VisibilitySprite;
        }

        private void RecalculateState()
        {
            var allChildrenActive = true;
            foreach (var child in ChildrenUI)
            {
                allChildrenActive &= child.Layer.ActiveSelf;
            }

            if (!Layer.ActiveSelf)
            {
                State = LayerActiveState.Disabled;
            }
            else if (Layer.ActiveSelf && !Layer.ActiveInHierarchy)
            {
                State = LayerActiveState.EnabledInDisabled;
            }
            else if (allChildrenActive)
            {
                State = LayerActiveState.Enabled;
            }
            else
            {
                State = LayerActiveState.Mixed;
            }

            SetVisibilitySprite();
        }

        private void OnFoldoutToggleValueChanged(bool isOn)
        {
            UpdateFoldout();
            RecalculateVisibleHierarchyRecursive();
        }

        private void Start()
        {
            Layer.LayerActiveInHierarchyChanged.AddListener(UpdateEnabledToggle);
            Layer.ColorChanged.AddListener(SetColor);
            Layer.LayerDestroyed.AddListener(DestroyUI);
            Layer.LayerSelected.AddListener(OnLayerSelected);
            Layer.LayerDeselected.AddListener(OnLayerDeselected);

            MarkLayerUIAsDirty();
            enabledToggle.SetIsOnWithoutNotify(Layer.ActiveInHierarchy); //initial update of if the toggle should be on or off. This should not be in UpdateLayerUI, because if a parent toggle is off, the child toggle could be on but then the layer would still not be active in the scene
            SetColor(Layer.Color);
        }

        public void SetParent(LayerUI newParent, int siblingIndex = -1) //todo: make this only change the UI parent, move all data logic to LayerNL3DBase
        {
            if (newParent == this)
                return;

            var oldParent = ParentUI;

            if (newParent == null)
                transform.SetParent(LayerBaseTransform);
            else
                transform.SetParent(newParent.childrenPanel);

            transform.SetSiblingIndex(siblingIndex);

            if (oldParent)
            {
                oldParent.RecalculateParentAndChildren();
                oldParent.RecalculateCurrentTreeStates();
            }

            if (newParent)
            {
                newParent.RecalculateParentAndChildren();
                newParent.foldoutToggle.isOn = true;
            }

            RecalculateParentAndChildren();
            RecalculateVisibleHierarchyRecursive();
            RecalculateCurrentTreeStates();
        }

        public void RecalculateVisibleHierarchyRecursive()
        {
            layerManager.LayerUIsVisibleInInspector.Clear();
            layerManager.LayerUIsVisibleInInspector = LayerBaseTransform.GetComponentsInChildren<LayerUI>(false).ToList();

            if (Layer) // When the layer is deleted, this UI should not update
                MarkLayerUIAsDirty();
        }

        private void RecalculateParentAndChildren()
        {
            ParentUI = transform.parent.GetComponentInParent<LayerUI>(true); // use transform.parent.GetComponentInParent to avoid getting the LayerUI on this gameObject

            var list = new List<LayerUI>();
            foreach (Transform t in childrenPanel) //loop over the transforms explicitly because using GetComponentsInChildren is recursive.
            {
                var ui = t.GetComponent<LayerUI>();
                list.Add(ui);
            }

            ChildrenUI = list.ToArray();
            UpdateFoldout(); //update the foldout because the children are recalculated
        }

        public void MarkLayerUIAsDirty()
        {
            isDirty = true;
        }

        private void LateUpdate()
        {
            if (isDirty)
            {
                UpdateLayerUI();
                isDirty = false;
            }
        }

        void UpdateLayerUI()
        {
            UpdateName();
            RecalculateIndent(Layer.Depth);
            SetLayerTypeImage();
            RecalculateNameWidth();
            UpdateFoldout();

            UpdatePropertiesToggle();
        }

        private void UpdatePropertiesToggle()
        {
            // only show properties button if the layer has any property sections to show
            var layerWithProperties = TryFindProperties();
            propertyToggle.gameObject.SetActive(
                layerWithProperties != null && layerWithProperties.GetPropertySections().Count > 0
            );
        }

        private ILayerWithProperties TryFindProperties()
        {
            var layerProxy = Layer as ReferencedProxyLayer;

            return (layerProxy == null) ? Layer as ILayerWithProperties : layerProxy.Reference as ILayerWithProperties;
        }

        private void SetLayerTypeImage()
        {
            var sprite = layerManager.GetLayerTypeSprite(Layer);
            layerTypeImage.sprite = sprite;
        }

        private void UpdateFoldout()
        {
            foldoutToggle.gameObject.SetActive(ChildrenUI.Length > 0);
            childrenPanel.gameObject.SetActive(foldoutToggle.isOn && (ChildrenUI.Length > 0));
            int index = foldoutToggle.isOn ? 1 : 0;
            foldoutToggle.targetGraphic.GetComponent<Image>().sprite = foldoutSprites[index];
        }

        private void RecalculateIndent(int childDepth)
        {
            var spacerRectTransform = spacer.transform as RectTransform;
            spacerRectTransform.sizeDelta = new Vector2((childDepth * indentWidth) + spacerStartWidth, spacerRectTransform.sizeDelta.y);
        }

        private void UpdateName()
        {
            layerNameText.text = Layer.Name;
            layerNameField.text = Layer.Name;
        }

        private void RecalculateNameWidth()
        {
            var maxWidth = 0f;
            if (ParentUI)
                maxWidth = ParentUI.rectTransform.rect.width;
            else
                maxWidth = LayerBaseTransform.GetComponent<RectTransform>().rect.width;

            var layerNameFieldRectTransform = layerNameField.GetComponent<RectTransform>();
            var width = maxWidth;
            width -= layerNameFieldRectTransform.anchoredPosition.x;
            width -= spacer.rect.width;
            width += parentRowRectTransform.GetComponent<HorizontalLayoutGroup>().padding.left;
            layerNameFieldRectTransform.sizeDelta = new Vector2(width, layerNameFieldRectTransform.rect.height);
            layerNameText.GetComponent<RectTransform>().sizeDelta = layerNameFieldRectTransform.sizeDelta;
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
            if (Layer.IsSelected)
            {
                waitForFullClickToDeselect = true;
                return;
            }

            ProcessLayerSelection();
        }

        private void OnRightButtonDown(PointerEventData eventData)
        {
            referenceLayerUnderMouse = this;
            if (!Layer.IsSelected)
            {
                ProcessLayerSelection();
            }

            // layerManager.EnableContextMenu(true, eventData.position);  //disabled context menu until UI is ready
        }

        private void ProcessLayerSelection()
        {
            if (SequentialSelectionModifierKeyIsPressed() && ProjectData.RootLayer.SelectedLayers.Count > 0) //if no layers are selected, there will be no reference layer to add to
            {
                // add all layers between the currently selected layer and the reference layer
                var referenceLayer = ProjectData.RootLayer.SelectedLayers.Last(); //last element is always the last selected layer
                var myIndex = layerManager.LayerUIsVisibleInInspector.IndexOf(this);
                var referenceIndex = layerManager.LayerUIsVisibleInInspector.IndexOf(referenceLayer.UI);

                var startIndex = referenceIndex > myIndex ? myIndex + 1 : referenceIndex + 1;
                var endIndex = referenceIndex > myIndex ? referenceIndex - 1 : myIndex - 1;

                var addLayers = !Layer.IsSelected; //add or subtract layers?

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var ui = layerManager.LayerUIsVisibleInInspector[i];

                    if (addLayers && !ui.Layer.IsSelected)
                    {
                        ui.Layer.SelectLayer();
                    }
                    else if (!addLayers && ui.Layer.IsSelected)
                    {
                        ui.Layer.DeselectLayer();
                    }
                }

                if (!addLayers)
                {
                    // referenceLayer.DeselectUI();
                    // DeselectUI();
                    referenceLayer.DeselectLayer();
                    Layer.DeselectLayer();
                }
            }

            if (!AddToSelectionModifierKeyIsPressed() && !SequentialSelectionModifierKeyIsPressed())
                Layer.Root.DeselectAllLayers();

            if (Layer.IsSelected)
            {
                Layer.DeselectLayer();
            }
            else
            {
                Layer.SelectLayer();
            }
        }

        private void OnLayerSelected(LayerNL3DBase layer)
        {
            SelectUI();
        }
        
        private void OnLayerDeselected(LayerNL3DBase layer)
        {
            DeselectUI();
        }
        
        private void SelectUI()
        {
            SetHighlight(InteractionState.Selected);
        }
        
        private void DeselectUI()
        {
            if (propertyToggle.isOn) propertyToggle.isOn = false;
            SetHighlight(InteractionState.Default);
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
                layerManager.SortSelectedLayersByVisibility();
                ProjectData.RootLayer.SelectedLayers.Reverse();

                foreach (var selectedLayer in ProjectData.RootLayer.SelectedLayers)
                {
                    selectedLayer.SetParent(newParent?.Layer, newSiblingIndex);
                }
            }

            RemoveHoverHighlight(referenceLayerUnderMouse);

            layerManager.EndDragLayer();
        }

        public void OnDrag(PointerEventData eventData) //has to be here or OnBeginDrag and OnEndDrag won't work
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            RemoveHoverHighlight(referenceLayerUnderMouse);

            referenceLayerUnderMouse = CalculateLayerUnderMouse(out float relativeYValue);
            if (referenceLayerUnderMouse)
            {
                var hoverTransform = referenceLayerUnderMouse.parentRowRectTransform; // as RectTransform;
                var correctedSize = (hoverTransform.rect.size + new Vector2(0, childVerticalLayoutGroup.padding.top)) * hoverTransform.lossyScale;

                var spacingOffset = (childVerticalLayoutGroup.spacing / 2) * layerManager.DragLine.lossyScale.y;
                spacingOffset -= layerManager.DragLine.rect.height / 2 * layerManager.DragLine.lossyScale.y;
                float leftOffset = referenceLayerUnderMouse.parentRowRectTransform.GetComponent<HorizontalLayoutGroup>().padding.left +
                                   referenceLayerUnderMouse.layerTypeImage.rectTransform.anchoredPosition.x +
                                   referenceLayerUnderMouse.layerTypeImage.rectTransform.rect.width * referenceLayerUnderMouse.layerTypeImage.rectTransform.pivot.x;

                if (relativeYValue > 0.25f * hoverTransform.rect.height)
                {
                    // print("higher than " + referenceLayerUnderMouse.Layer.name);
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, referenceLayerUnderMouse.parentRowRectTransform.position.y + correctedSize.y / 2 - spacingOffset);
                    layerManager.DragLine.SetLeft(leftOffset);

                    newParent = referenceLayerUnderMouse.ParentUI;
                    newSiblingIndex = referenceLayerUnderMouse.transform.GetSiblingIndex();

                    if (newParent == ParentUI && newSiblingIndex > transform.GetSiblingIndex()) //account for self being included
                        newSiblingIndex--;
                }
                else if (relativeYValue < -0.25f * hoverTransform.rect.height)
                {
                    // print("lower than" + referenceLayerUnderMouse.Layer.name);
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, referenceLayerUnderMouse.parentRowRectTransform.position.y - correctedSize.y / 2 - spacingOffset);

                    //edge case: if the reorder is between layerUnderMouse, and between layerUnderMouse and child 0 of layerUnderMouse, the new parent should be the layerUnderMouse instead of the layerUnderMouse's parent 
                    if (referenceLayerUnderMouse.ChildrenUI.Length > 0 && referenceLayerUnderMouse.foldoutToggle.isOn)
                    {
                        // print("edge case for: " + referenceLayerUnderMouse.Layer.name);
                        leftOffset += indentWidth;
                        layerManager.DragLine.SetLeft(leftOffset);
                        newParent = referenceLayerUnderMouse;
                        newSiblingIndex = 0;
                    }
                    else
                    {
                        layerManager.DragLine.SetLeft(leftOffset);
                        newParent = referenceLayerUnderMouse.ParentUI;
                        newSiblingIndex = referenceLayerUnderMouse.transform.GetSiblingIndex() + 1;
                        if (newParent == ParentUI && newSiblingIndex > transform.GetSiblingIndex()) //account for self being included
                            newSiblingIndex--;
                    }

                    if (relativeYValue < -hoverTransform.rect.height / 2 - spacingOffset) // if dragging below last layer, the dragged layer should SetParent to null, and the dragline should indicate that 
                    {
                        //if mouse is fully to the bottom, set parent to null
                        var defaultLeftOffset = leftOffset - referenceLayerUnderMouse.Layer.Depth * indentWidth;
                        layerManager.DragLine.SetLeft(defaultLeftOffset);
                        newParent = null;
                        newSiblingIndex = LayerBaseTransform.childCount;
                    }
                }
                else
                {
                    // print("reparent to " + referenceLayerUnderMouse.Layer.name);
                    referenceLayerUnderMouse.SetHighlight(InteractionState.DragHover);
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(false);

                    newParent = referenceLayerUnderMouse;
                    newSiblingIndex = referenceLayerUnderMouse.childrenPanel.childCount;
                }
            }
        }

        private void RemoveHoverHighlight(LayerUI ui)
        {
            if (ui)
            {
                var state = InteractionState.Default;
                if (ui.Layer.IsSelected)
                    state = InteractionState.Selected;
                ui.SetHighlight(state);
            }
        }

        public void SetHighlight(InteractionState state)
        {
            GetComponentInChildren<Image>().sprite = backgroundSprites[(int)state];
        }

        private LayerUI CalculateLayerUnderMouse(out float relativeYValue)
        {
            var ghostRectTransform = layerManager.DragGhost.GetComponent<RectTransform>();
            var ghostSize = ghostRectTransform.rect.size * ghostRectTransform.lossyScale;
            var mousePos = (Vector2)layerManager.DragGhost.transform.position - ghostSize / 2;

            for (var i = layerManager.LayerUIsVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = layerManager.LayerUIsVisibleInInspector[i];
                var correctedSize = layer.parentRowRectTransform.rect.size * layer.parentRowRectTransform.lossyScale;
                correctedSize.y += childVerticalLayoutGroup.spacing * layer.parentRowRectTransform.lossyScale.y;
                var layerPosTop = (Vector2)layer.parentRowRectTransform.position + correctedSize / 2;
                var layerPosBottom = (Vector2)layer.parentRowRectTransform.position - correctedSize / 2;

                if (mousePos.y < layerPosTop.y && mousePos.y >= layerPosBottom.y)
                {
                    relativeYValue = (mousePos.y - layer.parentRowRectTransform.position.y) / layer.parentRowRectTransform.lossyScale.y;
                    return layer;
                }
            }

            var firstLayer = layerManager.LayerUIsVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                relativeYValue = (mousePos.y - firstLayer.parentRowRectTransform.position.y) / firstLayer.parentRowRectTransform.lossyScale.y;
                return firstLayer; //above first
            }

            var lastLayer = layerManager.LayerUIsVisibleInInspector.Last();
            relativeYValue = (mousePos.y - lastLayer.parentRowRectTransform.position.y) / firstLayer.parentRowRectTransform.lossyScale.y;
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

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!layerManager.DragGhost && !Layer.IsSelected)
                SetHighlight(InteractionState.Hover);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!Layer.IsSelected)
                SetHighlight(InteractionState.Default);
        }

        private void OnDestroy()
        {
            // if(Layer) //todo in case the layer still exists, because for example this ui was a child of a UI that was destroyed
            //     Destroy(Layer.gameObject); //this will also delete the ui when closing the layers panel, because that destroys the UI as well

            layerManager.RemoveUI(this);
            if (ParentUI)
                ParentUI.RecalculateParentAndChildren();

            RecalculateParentStates();
        }

        public void DestroyUI()
        {
            // Unparent before deleting to avoid UI being destroyed multiple times (through DestroyUI and as a
            // consequence of Destroying the parent)
            SetParent(null);

            // Make sure to remove the properties when removing the UI
            if (propertyToggle.isOn) propertyToggle.isOn = false;

            Destroy(gameObject);
        }

        public void RegisterWithPropertiesPanel(Properties propertiesPanel)
        {
            propertyToggle.group = propertiesPanel.GetComponent<ToggleGroup>();
            propertyToggle.onValueChanged.AddListener((onOrOff) => ToggleProperties(onOrOff, propertiesPanel));
        }

        private void ToggleProperties(bool onOrOff, Properties properties)
        {
            var layerWithProperties = TryFindProperties();
            if (layerWithProperties == null) return; // no properties, no action

            if (!onOrOff)
            {
                properties.Hide();
                return;
            }

            properties.Show(layerWithProperties);

            if (!Layer.IsSelected)
            {
                // To prevent confusion with the user, also immediately select this layer.
                Layer.SelectLayer(true);
            }
        }

        public void ToggleProperties(bool isOn)
        {
            propertyToggle.isOn = isOn;
        }

        public void SetColor(Color c)
        {
            colorButton.targetGraphic.color = c;
        }
    }
}