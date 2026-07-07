using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class LayerTreeViewItem : VisualElement, IVisualizationWithPropertyData
    {
        private bool layoutReordered = false; //needs to happen the first time, but not after rebinding the element

        private VisibilityToggle isActiveToggle;
        private VisualElement colorBar;
        private Icon layerTypeIcon;
        private EditableNameField nameInputField;
        private Toggle propertyToggle;

        private PropertyPanelBehaviour propertyPanelBehaviour;

        public LayerData LayerData => userData as LayerData;
        internal UnityEvent RequestTreeRefresh { get; } = new();
        internal UnityEvent RequestTreeRebuild { get; } = new();

        private IVisualElementScheduledItem clickTimer;
        [UxmlAttribute] public float ClickInterval { get; set; } = 0.5f;
        private bool waitingForClick = false;

        public UnityEvent<Vector2, LayerTreeViewItem> DragStarted { get; } = new();
        public UnityEvent<Vector2, LayerTreeViewItem> Dragging { get; } = new();
        public UnityEvent<Vector2, LayerTreeViewItem> DragEnded { get; } = new();

        private VisualElement itemRoot;
        public VisualElement ItemRoot => itemRoot;
        public VisibilityState VisibilityState => isActiveToggle.Image;
        public IconImage LayerTypeIcon => layerTypeIcon.Image;

        private VisualElement indent;
        private VisualElement foldout;
        public float IndentWidth => indent.resolvedStyle.width;
        public Rect FoldoutWorldBound => foldout.worldBound;
        
        public UnityEvent<LayerTreeViewItem> SelectLayerItem = new();
        public UnityEvent<LayerTreeViewItem> DeselectLayerItem = new();
        public UnityEvent<int, bool> VisibilityToggleChanged = new UnityEvent<int, bool>();

        public LayerTreeViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            isActiveToggle = this.Q<VisibilityToggle>("IsActiveToggle");
            layerTypeIcon = this.Q<Icon>("TypeIcon");
            colorBar = this.Q<VisualElement>("ColorBar");
            nameInputField = this.Q<EditableNameField>("NameInputField");
            propertyToggle = this.Q<Toggle>("PropertyToggle");

            RegisterCallback<ClickEvent>(OnClick);
            var dragManipulator = new DragManipulator(8);
            dragManipulator.DragStarted.AddListener(OnDragStarted);
            dragManipulator.Dragging.AddListener(OnDragging);
            dragManipulator.DragEnded.AddListener(OnDragEnded);
            this.AddManipulator(dragManipulator);

            isActiveToggle.RegisterValueChangedCallback(OnIsActiveToggleChanged);
            nameInputField.RegisterValueChangedCallback(OnNameChanged);
            propertyToggle.RegisterValueChangedCallback(OnPropertyToggleValueChanged);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel); // we can only update the layout after attaching to the panel
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnClick(ClickEvent evt)
        {
            if (nameInputField.IsEditing)
            {
                waitingForClick = false;
                clickTimer.Pause();
                return;
            }

            if (waitingForClick)
            {
                OnDoubleClick();
                waitingForClick = false;
                clickTimer.Pause();
                return;
            }

            waitingForClick = true;
            clickTimer = schedule.Execute(() => waitingForClick = false);
            clickTimer.ExecuteLater((long)(ClickInterval * 1000));
        }

        private void OnDoubleClick()
        {
            LayerData.DoubleClickLayer();
        }

        private void OnDragStarted(Vector2 startPosition)
        {
            DragStarted.Invoke(startPosition, this);
        }

        private void OnDragging(Vector2 delta)
        {
            Dragging.Invoke(delta, this);
        }

        private void OnDragEnded(Vector2 endPosition)
        {
            DragEnded.Invoke(endPosition, this);
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            LayerData.Name = evt.newValue;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (propertyPanelBehaviour == null)
            {
                propertyPanelBehaviour = ServiceLocator.GetService<PropertyPanelBehaviour>();
                propertyPanelBehaviour.PropertySectionOpened.AddListener(CheckPropertyToggle);
                propertyPanelBehaviour.PropertySectionClosed.AddListener(UncheckPropertyToggle);
            }

            if (!layoutReordered)
                UpdateLayout();
        }

        private void UpdateLayout()
        {
            itemRoot = GetTreeViewItemRoot();
            indent = ItemRoot.Q("unity-tree-view__item-indent");
            foldout = ItemRoot.Q(className: "unity-tree-view__item-toggle");
                
            if (itemRoot == null) return;
            itemRoot.AddComponentStylesheetByType(GetType());

            // Find the index of the unity tree view toggle
            if (indent != null)
            {
                int toggleIndex = itemRoot.hierarchy.IndexOf(indent);

                itemRoot.hierarchy.Insert(toggleIndex, colorBar);
                itemRoot.hierarchy.Insert(toggleIndex, isActiveToggle);
            }

            layoutReordered = true;
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            propertyPanelBehaviour.PropertySectionClosed.RemoveListener(UncheckPropertyToggle);
            propertyPanelBehaviour.PropertySectionOpened.RemoveListener(CheckPropertyToggle);
        }

        private VisualElement GetTreeViewItemRoot()
        {
            VisualElement itemRoot = this;
            while (itemRoot != null && !itemRoot.ClassListContains("unity-tree-view__item"))
            {
                itemRoot = itemRoot.parent;
            }

            return itemRoot;
        }

        private void OnIsActiveToggleChanged(ChangeEvent<bool> evt)
        {
            if(LayerData.IsSelected)
                VisibilityToggleChanged.Invoke(LayerData.RootId, evt.newValue); //invoke an event to allow toggling of multi-selected items
            else
                LayerData.ActiveSelf = evt.newValue;
            
            evt.StopPropagation(); //avoid the layer from deselecting
        }

        private void UncheckPropertyToggle(LayerData layerData)
        {
            if (layerData == LayerData)
                propertyToggle.SetValueWithoutNotify(false);
        }

        private void CheckPropertyToggle(LayerData layerData)
        {
            if (layerData == LayerData)
                propertyToggle.SetValueWithoutNotify(true);
        }

        private void OnPropertyToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                propertyPanelBehaviour.SpawnPanel(LayerData);
            else
                propertyPanelBehaviour.ClearActivePanel();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyToggle.EnableInClassList(UtilityClassConstants.HIDDEN, !HasPropertiesWithPanel(properties));
        }


        public void Initialize(LayerData layerData)
        {
            if (layerData == null) return;

            var previous = this.LayerData;
            if(previous != null)
                RemoveLayerDataListeners(previous);
            
            userData = layerData;

            SetAppearance(layerData);

            layerData.HasValidCredentialsChanged.AddListener(OnCredentialStatusChanged);
            layerData.ActiveSelfChanged.AddListener(OnActiveSelfChanged);
            layerData.ParentOrSiblingIndexChanged.AddListener(OnParentChanged);
            layerData.LayerDestroyed.AddListener(OnLayerDestroyed);
            layerData.ColorChanged.AddListener(UpdateColorBar);
            layerData.OnPrefabIdChanged.AddListener(UpdateLayerTypeIcon);
            layerData.NameChanged.AddListener(UpdateNameLabels);
            layerData.PropertySet.AddListener(OnPropertiesChanged);
            layerData.PropertyRemoved.AddListener(OnPropertiesChanged);
            
            layerData.LayerSelected.AddListener(SelectUI);
            layerData.LayerDeselected.AddListener(DeselectUI);
        }

        public void RemoveLayerDataListeners(LayerData layerData)
        {
            layerData.HasValidCredentialsChanged.RemoveListener(OnCredentialStatusChanged);
            layerData.ActiveSelfChanged.RemoveListener(OnActiveSelfChanged);
            layerData.ParentOrSiblingIndexChanged.RemoveListener(OnParentChanged);
            layerData.LayerDestroyed.RemoveListener(OnLayerDestroyed);
            layerData.ColorChanged.RemoveListener(UpdateColorBar);
            layerData.OnPrefabIdChanged.RemoveListener(UpdateLayerTypeIcon);
            layerData.NameChanged.RemoveListener(UpdateNameLabels);
            layerData.PropertySet.RemoveListener(OnPropertiesChanged);
            layerData.PropertyRemoved.RemoveListener(OnPropertiesChanged);
            layerData.LayerSelected.RemoveListener(SelectUI); 
            layerData.LayerDeselected.RemoveListener(DeselectUI);
        }

        private void DeselectUI(LayerData layerData)
        {
            DeselectLayerItem.Invoke(this);
        }
        
        private void SelectUI(LayerData layerData)
        {
            SelectLayerItem.Invoke(this);
        }

        private void OnCredentialStatusChanged(bool valid)
        {
            SetAppearance(LayerData);
        }

        private void SetAppearance(LayerData layerData)
        {
            var validCredentials = layerData.HasValidCredentials;
            isActiveToggle.SetEnabled(validCredentials);
            ItemRoot.EnableInClassList("credentials-needed", !validCredentials);

            UpdateNameLabels(layerData, layerData.Name);
            UpdateEnabledToggle(layerData.ActiveInHierarchy);
            UpdateColorBar(validCredentials ? layerData.Color : null); //clear the colorbar style to ensure the warning color is not overridden when the credentials are invalid
            UpdateLayerTypeIcon();
            LoadProperties(layerData.LayerProperties);
        }

        private void OnPropertiesChanged(LayerPropertyData propertyData)
        {
            LoadProperties(LayerData.LayerProperties);
        }

        private void OnActiveSelfChanged(bool activeSelf)
        {
            RequestTreeRefresh.Invoke();
        }

        private void OnParentChanged(int newIndex)
        {
            RequestTreeRebuild.Invoke();
        }

        private void OnLayerDestroyed()
        {
            propertyToggle.value = false; //this closes the property panel if it was open
            RequestTreeRebuild.Invoke();
        }

        private void UpdateEnabledToggle(bool activeInHierarchy)
        {
            isActiveToggle.SetValueWithoutNotify(activeInHierarchy);
            RecalculateState();
            SetEnabledToggleInteractiveState();
        }

        private void SetEnabledToggleInteractiveState()
        {
            var parent = LayerData.ParentLayer;
            var interactable = parent is RootLayer || (parent != null && parent.ActiveInHierarchy);
            isActiveToggle.SetEnabled(interactable);
        }

        private void RecalculateState()
        {
            var allChildrenActive = true;

            foreach (var child in LayerData.ChildrenLayers)
            {
                allChildrenActive &= child.ActiveSelf;
            }

            isActiveToggle.SetStateFromLayerState(LayerData.ActiveSelf, LayerData.ActiveInHierarchy, allChildrenActive);
        }
        
        private void UpdateColorBar(Color newColor)
        {
            var opaqueColor = newColor;
            opaqueColor.a = 1;

            colorBar.style.backgroundColor = opaqueColor;
        }
        
        private void UpdateColorBar(Color? newColor)
        {
            if (!newColor.HasValue)
            {
                colorBar.style.backgroundColor = StyleKeyword.Null;
                return;
            }

            UpdateColorBar(newColor.Value);
        }

        private void UpdateLayerTypeIcon()
        {
            layerTypeIcon.Image = LayerTypeSpriteLibrary.GetIconImage(LayerData); //todo test if the icon updates when setting prefab (scatter)
        }

        private void UpdateNameLabels(LayerData layerData, string newName)
        {
            nameInputField.SetValueWithoutNotify(newName);
        }

        public bool HasPropertiesWithPanel(List<LayerPropertyData> properties)
        {
            foreach (var property in properties)
            {
                var type = property.GetType();
                foreach (var collection in PropertySectionRegistry.TypeRegistry.Values)
                    if (collection.Collection.ContainsKey(type))
                        return true;

                foreach (var interfaceType in type.GetInterfaces())
                {
                    foreach (var collection in PropertySectionRegistry.TypeRegistry.Values)
                        if (collection.Collection.ContainsKey(interfaceType))
                            return true;
                }
            }

            return false;
        }
    }
}