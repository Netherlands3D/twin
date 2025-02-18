using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
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
        public LayerData Layer { get; set; }

        private RectTransform rectTransform;
        private LayerUIManager layerUIManager;
        public Transform LayerBaseTransform => layerUIManager.LayerUIContainer;

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
        [SerializeField] private float doubleClickThreshold = 0.5f;

        private LayerUI ParentUI;
        private LayerUI[] ChildrenUI = Array.Empty<LayerUI>();

        private static LayerUI referenceLayerUnderMouse;
        private static LayerUI newParent;
        private int newSiblingIndex;

        private bool draggingLayerShouldBePlacedBeforeOtherLayer;
        private bool waitForFullClickToDeselect;
        private bool isDirty;
        private float lastClickTime = 0f;

        public LayerActiveState State { get; set; }
        public InteractionState InteractionState { get; set; }

        public Sprite VisibilitySprite => visibilitySprites[(int)State];

        public bool hasChildren => childrenPanel.childCount > 0;
        public Sprite LayerTypeSprite => layerTypeImage.sprite;
        public string LayerName => Layer.Name;
        public bool PropertiesOpen => propertyToggle.isOn;

        private int Depth => ParentUI ? ParentUI.Depth + 1 : 0;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            layerUIManager = GetComponentInParent<LayerUIManager>();
            childVerticalLayoutGroup = childrenPanel.GetComponent<VerticalLayoutGroup>();
            spacerStartWidth = spacer.sizeDelta.x;
        }

        private void OnEnable()
        {
            enabledToggle.onValueChanged.AddListener(OnEnabledToggleValueChanged);
            foldoutToggle.onValueChanged.AddListener(OnFoldoutToggleValueChanged);
            layerNameField.onEndEdit.AddListener(OnInputFieldChanged);
        }

        private void OnDisable()
        {
            enabledToggle.onValueChanged.RemoveListener(OnEnabledToggleValueChanged);
            foldoutToggle.onValueChanged.RemoveListener(OnFoldoutToggleValueChanged);
            layerNameField.onEndEdit.RemoveListener(OnInputFieldChanged);
        }

        private void OnEnabledToggleValueChanged(bool isOn)
        {
            Layer.ActiveSelf = isOn;
        }

        private void OnFoldoutToggleValueChanged(bool isOn)
        {
            UpdateFoldout();
            RecalculateVisibleHierarchyRecursive();
        }

        private void OnSelectInputField()
        {
            layerNameField.text = Layer.Name;
            layerNameText.text = Layer.Name;
            layerNameField.gameObject.SetActive(true);
            layerNameText.gameObject.SetActive(false);
            layerNameField.Select();
            layerNameField.ActivateInputField();
            StartCoroutine(WaitForNextFrame(() =>
            {
                layerNameField.caretPosition = layerNameField.text.Length;
                layerNameField.selectionAnchorPosition = 0;
            }));
        }

        private IEnumerator WaitForNextFrame(Action onNextFrame)
        {
            yield return new WaitForEndOfFrame();
            onNextFrame.Invoke();
        }

        private void OnInputFieldChanged(string newName)
        {
            Layer.Name = newName;
            layerNameText.text = newName;
            layerNameField.gameObject.SetActive(false);
            layerNameText.gameObject.SetActive(true);
        }

        private void Start()
        {
            Layer.NameChanged.AddListener(OnNameChanged);
            Layer.LayerActiveInHierarchyChanged.AddListener(UpdateEnabledToggle);
            Layer.ColorChanged.AddListener(UpdateColor);
            Layer.LayerSelected.AddListener(OnLayerSelected);
            Layer.LayerDeselected.AddListener(OnLayerDeselected);
            Layer.ChildrenChanged.AddListener(OnLayerChildrenChanged);
            Layer.ParentOrSiblingIndexChanged.AddListener(OnParentOrSiblingIndexChanged);
            Layer.LayerDestroyed.AddListener(DestroyUI);

            MarkLayerUIAsDirty();

            //Match initial layer states
            SetParent(layerUIManager.GetLayerUI(Layer.ParentLayer), Layer.SiblingIndex); // needed because eventListener is not assigned yet when calling layer.SetParent immediately after creating a layer object
            if (Layer.IsSelected)
                SetHighlight(InteractionState.Selected); // needed because eventListener is not assigned yet when calling layer.SelectLayer when the UI is instantiated
            enabledToggle.SetIsOnWithoutNotify(Layer.ActiveInHierarchy); //initial update of if the toggle should be on or off. This should not be in UpdateLayerUI, because if a parent toggle is off, the child toggle could be on but then the layer would still not be active in the scene
            UpdateColor(Layer.Color);
            RegisterWithPropertiesPanel(Properties.Properties.Instance);
        }

        private void OnNameChanged(string newName)
        {
            gameObject.name = newName;
        }

        private void UpdateEnabledToggle(bool isOn)
        {
            enabledToggle.SetIsOnWithoutNotify(isOn);
            RecalculateCurrentTreeStates();
            SetEnabledToggleInteractiveStateRecursive();
        }

        private void UpdateColor(Color newColor)
        {
            var opaqueColor = newColor;
            opaqueColor.a = 1;

            colorButton.targetGraphic.color = opaqueColor;
        }

        private void OnLayerSelected(LayerData layer)
        {
            SelectUI();
        }

        private void OnLayerDeselected(LayerData layer)
        {
            DeselectUI();
        }

        private void OnLayerChildrenChanged()
        {
            RecalculateCurrentTreeStates();
        }

        private void OnParentOrSiblingIndexChanged(int newSiblingIndex)
        {
            SetParent(layerUIManager.GetLayerUI(Layer.ParentLayer), newSiblingIndex);
        }

        private void DestroyUI()
        {
            // Unparent before deleting to avoid UI being destroyed multiple times (through DestroyUI and as a
            // consequence of Destroying the parent)
            SetParent(null);

            // Make sure to remove the properties when removing the UI
            if (propertyToggle.isOn) propertyToggle.isOn = false;

            gameObject.SetActive(false); //ensure it won't get re-added in LayerManager.RecalculateLayersInInspector
            Destroy(gameObject);
            layerUIManager.RecalculateLayersVisibleInInspector();

            if (ParentUI)
                ParentUI.RecalculateParentAndChildren();

            RecalculateParentStates();
        }

        private void RecalculateCurrentTreeStates()
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

        private void SetEnabledToggleInteractiveStateRecursive()
        {
            enabledToggle.interactable = !ParentUI || (ParentUI && Layer.ParentLayer.ActiveInHierarchy);

            foreach (var child in ChildrenUI)
                child.SetEnabledToggleInteractiveStateRecursive();
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

        public void SetParent(LayerUI newParent, int siblingIndex = -1)
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

        private void RecalculateVisibleHierarchyRecursive()
        {
            layerUIManager.RecalculateLayersVisibleInInspector();
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
            foreach (var child in ChildrenUI)
            {
                child.MarkLayerUIAsDirty();
            }
        }

        private void LateUpdate()
        {
            if (isDirty)
            {
                UpdateLayerUI();
                isDirty = false;
            }
        }

        private void UpdateLayerUI()
        {
            UpdateName();
            RecalculateIndent(Depth);
            SetLayerTypeImage();
            RecalculateNameWidth();
            UpdateFoldout();
        }

        private void SetLayerTypeImage()
        {
            var sprite = layerUIManager.GetLayerTypeSprite(Layer);
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
            layerNameField.GetComponent<RectTransform>().sizeDelta = layerNameFieldRectTransform.sizeDelta;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnLeftButtonDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Right)
                OnRightButtonDown(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!layerUIManager.DragGhost && !Layer.IsSelected)
                SetHighlight(InteractionState.Hover);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!Layer.IsSelected)
                SetHighlight(InteractionState.Default);
        }

        private void OnLeftButtonDown(PointerEventData eventData)
        {
            layerUIManager.DragStartOffset = (Vector2)transform.position - eventData.position;
            referenceLayerUnderMouse = this;

            //if the layer under mouse is already selected, this can be the beginning of a drag, so don't deselect anything yet. wait for the pointer up event that is not a drag
            waitForFullClickToDeselect = false; //reset value to be sure no false positives are processed
            if (Layer.IsSelected)
            {
                //only one extra click on a selected layer should initiate the layer name editing
                float timeSinceLastClick = Time.time - lastClickTime;
                
                if (timeSinceLastClick <= doubleClickThreshold)
                {
                    // Detected double-click
                    JumpCameraToLayer();
                }
                else if (eventData.pointerEnter == layerNameText.gameObject)
                {
                    OnSelectInputField();
                }

                waitForFullClickToDeselect = true;
            }
            else
            {
                ProcessLayerSelection();
            }
            lastClickTime = Time.time;
        }

        private void JumpCameraToLayer()
        {
            if (Layer is ReferencedLayerData referencedLayerData)
            {
                Layer.DoubleClickLayer();
            }
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
            if (SequentialSelectionModifierKeyIsPressed() && Layer.Root.SelectedLayers.Count > 0) //if no layers are selected, there will be no reference layer to add to
            {
                // add all layers between the currently selected layer and the reference layer
                var referenceLayer = Layer.Root.SelectedLayers.Last(); //last element is always the last selected layer
                var myIndex = layerUIManager.LayerUIsVisibleInInspector.IndexOf(this);
                var referenceIndex = layerUIManager.LayerUIsVisibleInInspector.IndexOf(layerUIManager.GetLayerUI(referenceLayer));

                var startIndex = referenceIndex > myIndex ? myIndex + 1 : referenceIndex + 1;
                var endIndex = referenceIndex > myIndex ? referenceIndex - 1 : myIndex - 1;

                var addLayers = !Layer.IsSelected; //add or subtract layers?

                for (int i = startIndex; i <= endIndex; i++)
                {
                    var ui = layerUIManager.LayerUIsVisibleInInspector[i];

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

        private void SelectUI()
        {
            propertyToggle.isOn = true;
            SetHighlight(InteractionState.Selected);
        }

        private void DeselectUI()
        {
            propertyToggle.isOn = false;
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

            layerUIManager.StartDragLayer(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (!layerUIManager.IsDragOnButton()) //don't reparent when dragging on a button, since the button action should be called instead and handle any possible reparents
            {
                layerUIManager.SortSelectedLayersByVisibility();
                Layer.Root.SelectedLayers.Reverse();

                foreach (var selectedLayer in Layer.Root.SelectedLayers.ToList()) //to list makes a copy and avoids a collectionmodified error
                {
                    selectedLayer.SetParent(newParent?.Layer, newSiblingIndex);
                }
            }

            RemoveHoverHighlight(referenceLayerUnderMouse);

            layerUIManager.EndDragLayer();
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

                var spacingOffset = (childVerticalLayoutGroup.spacing / 2) * layerUIManager.DragLine.lossyScale.y;
                spacingOffset -= layerUIManager.DragLine.rect.height / 2 * layerUIManager.DragLine.lossyScale.y;
                float leftOffset = referenceLayerUnderMouse.parentRowRectTransform.GetComponent<HorizontalLayoutGroup>().padding.left +
                                   referenceLayerUnderMouse.layerTypeImage.rectTransform.anchoredPosition.x +
                                   referenceLayerUnderMouse.layerTypeImage.rectTransform.rect.width * referenceLayerUnderMouse.layerTypeImage.rectTransform.pivot.x;

                if (relativeYValue > 0.25f * hoverTransform.rect.height)
                {
                    // print("higher than " + referenceLayerUnderMouse.Layer.name);
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerUIManager.DragLine.gameObject.SetActive(true);
                    layerUIManager.DragLine.position = new Vector2(layerUIManager.DragLine.position.x, referenceLayerUnderMouse.parentRowRectTransform.position.y + correctedSize.y / 2 - spacingOffset);
                    layerUIManager.DragLine.SetLeft(leftOffset);

                    newParent = referenceLayerUnderMouse.ParentUI;
                    newSiblingIndex = referenceLayerUnderMouse.transform.GetSiblingIndex();

                    if (newParent == ParentUI && newSiblingIndex > transform.GetSiblingIndex()) //account for self being included
                        newSiblingIndex--;
                }
                else if (relativeYValue < -0.25f * hoverTransform.rect.height)
                {
                    // print("lower than" + referenceLayerUnderMouse.Layer.name);
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerUIManager.DragLine.gameObject.SetActive(true);
                    layerUIManager.DragLine.position = new Vector2(layerUIManager.DragLine.position.x, referenceLayerUnderMouse.parentRowRectTransform.position.y - correctedSize.y / 2 - spacingOffset);

                    //edge case: if the reorder is between layerUnderMouse, and between layerUnderMouse and child 0 of layerUnderMouse, the new parent should be the layerUnderMouse instead of the layerUnderMouse's parent 
                    if (referenceLayerUnderMouse.ChildrenUI.Length > 0 && referenceLayerUnderMouse.foldoutToggle.isOn)
                    {
                        // print("edge case for: " + referenceLayerUnderMouse.Layer.name);
                        leftOffset += indentWidth;
                        layerUIManager.DragLine.SetLeft(leftOffset);
                        newParent = referenceLayerUnderMouse;
                        newSiblingIndex = 0;
                    }
                    else
                    {
                        layerUIManager.DragLine.SetLeft(leftOffset);
                        newParent = referenceLayerUnderMouse.ParentUI;
                        newSiblingIndex = referenceLayerUnderMouse.transform.GetSiblingIndex() + 1;
                        if (newParent == ParentUI && newSiblingIndex > transform.GetSiblingIndex()) //account for self being included
                            newSiblingIndex--;
                    }

                    if (relativeYValue < -hoverTransform.rect.height / 2 - spacingOffset) // if dragging below last layer, the dragged layer should SetParent to null, and the dragline should indicate that 
                    {
                        //if mouse is fully to the bottom, set parent to null
                        var defaultLeftOffset = leftOffset - referenceLayerUnderMouse.Depth * indentWidth;
                        layerUIManager.DragLine.SetLeft(defaultLeftOffset);
                        newParent = null;
                        newSiblingIndex = LayerBaseTransform.childCount;
                    }
                }
                else
                {
                    // print("reparent to " + referenceLayerUnderMouse.Layer.name);
                    referenceLayerUnderMouse.SetHighlight(InteractionState.DragHover);
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerUIManager.DragLine.gameObject.SetActive(false);

                    newParent = referenceLayerUnderMouse;
                    newSiblingIndex = referenceLayerUnderMouse.childrenPanel.childCount;
                }
            }
        }

        private void RemoveHoverHighlight(LayerUI ui)
        {
            if (!ui) return;

            var state = InteractionState.Default;
            if (ui.Layer.IsSelected)
                state = InteractionState.Selected;
            ui.SetHighlight(state);
        }

        public void SetHighlight(InteractionState state)
        {
            GetComponentInChildren<Image>().sprite = backgroundSprites[(int)state];
        }

        private LayerUI CalculateLayerUnderMouse(out float relativeYValue)
        {
            var ghostRectTransform = layerUIManager.DragGhost.GetComponent<RectTransform>();
            var ghostSize = ghostRectTransform.rect.size * ghostRectTransform.lossyScale;
            var mousePos = (Vector2)layerUIManager.DragGhost.transform.position - ghostSize / 2;

            for (var i = layerUIManager.LayerUIsVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = layerUIManager.LayerUIsVisibleInInspector[i];
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

            var firstLayer = layerUIManager.LayerUIsVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                relativeYValue = (mousePos.y - firstLayer.parentRowRectTransform.position.y) / firstLayer.parentRowRectTransform.lossyScale.y;
                return firstLayer; //above first
            }

            var lastLayer = layerUIManager.LayerUIsVisibleInInspector.Last();
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

        private void OnDestroy()
        {
            Layer.NameChanged.RemoveListener(OnNameChanged);
            Layer.LayerActiveInHierarchyChanged.RemoveListener(UpdateEnabledToggle);
            Layer.ColorChanged.RemoveListener(UpdateColor);
            Layer.LayerSelected.RemoveListener(OnLayerSelected);
            Layer.LayerDeselected.RemoveListener(OnLayerDeselected);
            Layer.ChildrenChanged.RemoveListener(OnLayerChildrenChanged);
            Layer.ParentOrSiblingIndexChanged.RemoveListener(OnParentOrSiblingIndexChanged);
            Layer.LayerDestroyed.RemoveListener(DestroyUI);
        }

        private void RegisterWithPropertiesPanel(Properties.Properties propertiesPanel)
        {
            var layerWithProperties = Properties.Properties.TryFindProperties(Layer);
            var hasProperties = layerWithProperties != null && layerWithProperties.GetPropertySections().Count > 0;
            propertyToggle.gameObject.SetActive(hasProperties);

            if (!hasProperties)
                return;

            propertyToggle.group = propertiesPanel.GetComponent<ToggleGroup>();
            propertyToggle.onValueChanged.AddListener((onOrOff) => ToggleProperties(onOrOff, propertiesPanel));
            ToggleProperties(propertyToggle.isOn, propertiesPanel);
        }

        public void ToggleProperties(bool onOrOff)
        {
            propertyToggle.isOn = onOrOff;
        }

        private void ToggleProperties(bool onOrOff, Properties.Properties properties)
        {
            var layerWithProperties = Properties.Properties.TryFindProperties(Layer);
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
    }
}