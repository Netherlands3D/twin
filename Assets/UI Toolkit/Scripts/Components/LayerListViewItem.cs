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
    public partial class LayerListViewItem : VisualElement, IVisualizationWithPropertyData
    {
        private bool layoutReordered = false; //needs to happen the first time, but not after rebinding the element

        private VisibilityToggle isActiveToggle;
        private VisualElement colorBar;
        private Icon layerTypeIcon;
        private EditableNameField nameInputField;
        private Toggle propertyToggle;

        private PropertyPanelBehaviour propertyPanelBehaviour;

        public LayerData layerData => userData as LayerData;
        public UnityEvent RequestTreeRefresh { get; } = new();
        public UnityEvent RequestTreeRebuild { get; } = new();
        
        private IVisualElementScheduledItem clickTimer;
        [UxmlAttribute] public float ClickInterval { get; set; } = 0.5f;
        private bool waitingForClick = false;
        
        public UnityEvent<Vector2, LayerListViewItem> DragStarted { get; } = new();
        public UnityEvent<Vector2, LayerListViewItem> Dragging { get; } = new();
        public UnityEvent<Vector2, LayerListViewItem> DragEnded { get; } = new();
        
        private VisualElement itemRoot;
        public VisualElement ItemRoot => itemRoot;
        public VisibilityState VisibilityState => isActiveToggle.Image;
        public IconImage LayerTypeIcon => layerTypeIcon.Image;
        public float IndentWidth => ItemRoot.Q("unity-tree-view__item-indent").resolvedStyle.width;
        
        public LayerListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            propertyPanelBehaviour = ServiceLocator.GetService<PropertyPanelBehaviour>();
            propertyPanelBehaviour.PropertySectionClosed.AddListener(UncheckPropertyToggle);

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
            layerData.DoubleClickLayer();
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
            layerData.Name = evt.newValue;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (!layoutReordered)
                UpdateLayout();
        }

        private void UpdateLayout()
        {
            itemRoot = GetTreeViewItemRoot();

            if (itemRoot == null) return;
            itemRoot.AddComponentStylesheetByType(GetType());

            // Find the index of the unity tree view toggle
            var treeToggle = itemRoot.Q("unity-tree-view__item-indent");
            if (treeToggle != null)
            {
                int toggleIndex = itemRoot.hierarchy.IndexOf(treeToggle);

                itemRoot.hierarchy.Insert(toggleIndex, colorBar);
                itemRoot.hierarchy.Insert(toggleIndex, isActiveToggle);
            }

            layoutReordered = true;
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
            layerData.ActiveSelf = evt.newValue;
        }

        private void UncheckPropertyToggle(LayerData layerData)
        {
            if (layerData == this.layerData)
                propertyToggle.SetValueWithoutNotify(false);
        }

        private void OnPropertyToggleValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                propertyPanelBehaviour.SpawnPanel(layerData);
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

            var previous = this.layerData;
            userData = layerData;

            //visibility toggle
            previous?.ActiveSelfChanged.RemoveListener(OnActiveSelfChanged);
            UpdateEnabledToggle(layerData.ActiveInHierarchy);
            layerData.ActiveSelfChanged.AddListener(OnActiveSelfChanged);

            previous?.ParentOrSiblingIndexChanged.RemoveListener(OnParentChanged);
            layerData.ParentOrSiblingIndexChanged.AddListener(OnParentChanged);

            previous?.LayerDestroyed.RemoveListener(OnLayerDestroyed);
            layerData.LayerDestroyed.AddListener(OnLayerDestroyed);

            //Color bar
            previous?.ColorChanged.RemoveListener(UpdateColorBar);
            UpdateColorBar(layerData.Color);
            layerData.ColorChanged.AddListener(UpdateColorBar);

            //LayerTypeIcon
            previous?.OnPrefabIdChanged.RemoveListener(UpdateLayerTypeIcon);
            UpdateLayerTypeIcon();
            layerData.OnPrefabIdChanged.AddListener(UpdateLayerTypeIcon);

            //Layer Name
            previous?.NameChanged.RemoveListener(UpdateNameLabels);
            UpdateNameLabels(layerData, layerData.Name);
            layerData.NameChanged.AddListener(UpdateNameLabels);

            //properties
            previous?.PropertySet.RemoveListener(OnPropertiesChanged);
            previous?.PropertyRemoved.RemoveListener(OnPropertiesChanged);
            LoadProperties(layerData.LayerProperties);
            layerData.PropertySet.AddListener(OnPropertiesChanged);
            layerData.PropertyRemoved.AddListener(OnPropertiesChanged);
        }

        private void OnPropertiesChanged(LayerPropertyData propertyData)
        {
            LoadProperties(layerData.LayerProperties);
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
            var parent = layerData.ParentLayer;
            var interactable = parent is RootLayer || (parent != null && parent.ActiveInHierarchy);
            isActiveToggle.SetEnabled(interactable);
        }

        private void RecalculateState()
        {
            var allChildrenActive = true;

            foreach (var child in layerData.ChildrenLayers)
            {
                allChildrenActive &= child.ActiveSelf;
            }

            isActiveToggle.SetStateFromLayerState(layerData.ActiveSelf, layerData.ActiveInHierarchy, allChildrenActive);
        }


        private void UpdateColorBar(Color newColor)
        {
            var opaqueColor = newColor;
            opaqueColor.a = 1;

            colorBar.style.backgroundColor = opaqueColor;
        }

        private void UpdateLayerTypeIcon()
        {
            layerTypeIcon.Image = GetImage(layerData); //todo test if the icon updates when setting prefab (scatter)
        }

        private static IconImage GetImage(LayerData layerData)
        {
            return LayerTypeSpriteLibrary.GetIconImage(layerData);
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

        //todo: root.Selectedlayers
        //todo: cleanup old scripts
        //todo: credential needed state
    }
}