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
        private Label nameLabel;
        private Toggle propertyToggle;

        PropertyPanelBehaviour propertyPanelBehaviour;

        private LayerData layerData => userData as LayerData;
        public UnityEvent RequestTreeRefresh { get; } = new();
        public UnityEvent RequestTreeRebuild { get; } = new();

        public LayerListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            propertyPanelBehaviour = ServiceLocator.GetService<PropertyPanelBehaviour>();
            propertyPanelBehaviour.PropertySectionClosed.AddListener(UncheckPropertyToggle);

            isActiveToggle = this.Q<VisibilityToggle>("IsActiveToggle");
            layerTypeIcon = this.Q<Icon>("TypeIcon");
            colorBar = this.Q<VisualElement>("ColorBar");
            nameLabel = this.Q<Label>("NameLabel");
            propertyToggle = this.Q<Toggle>("PropertyToggle");

            isActiveToggle.RegisterValueChangedCallback(OnIsActiveToggleChanged);
            propertyToggle.RegisterValueChangedCallback(OnPropertyToggleValueChanged);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel); // we can only update the layout after attaching to the panel
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if(!layoutReordered)
                UpdateLayout();
        }
        
        private void UpdateLayout()
        {
            VisualElement itemRoot = this;
            while (itemRoot != null && !itemRoot.ClassListContains("unity-tree-view__item"))
            {
                itemRoot = itemRoot.parent;
            }

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
            if(layerData == null) return;
            
            var previous = this.layerData;
            userData = layerData;
            
            //visibility toggle
            previous?.ActiveSelfChanged.RemoveListener(OnActiveSelfChanged);
            UpdateEnabledToggle(layerData.ActiveInHierarchy);
            layerData.ActiveSelfChanged.AddListener(OnActiveSelfChanged);

            layerData.ParentOrSiblingIndexChanged.RemoveListener(OnParentChanged);
            layerData.ParentOrSiblingIndexChanged.AddListener(OnParentChanged);
            
            //Color bar
            previous?.ColorChanged.RemoveListener(UpdateColorBar);
            UpdateColorBar(layerData.Color);
            layerData.ColorChanged.AddListener(UpdateColorBar);
            
            //LayerTypeIcon
            previous?.OnPrefabIdChanged.RemoveListener(UpdateLayerTypeIcon);
            UpdateLayerTypeIcon();
            layerData.OnPrefabIdChanged.AddListener(UpdateLayerTypeIcon);
            
            //Layer Name
            previous?.NameChanged.RemoveListener(UpdateNameLabel);
            UpdateNameLabel(layerData, layerData.Name);
            layerData.NameChanged.AddListener(UpdateNameLabel);

            //properties
            LoadProperties(layerData.LayerProperties);
        }

        private void OnActiveSelfChanged(bool activeSelf)
        {
            RequestTreeRefresh.Invoke();
        }
        
        private void OnParentChanged(int newIndex)
        {
            RequestTreeRebuild.Invoke(); //todo: test if this updates the visibility toggle correctly
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
        
            if (!layerData.ActiveSelf)
            {
                isActiveToggle.SetState(VisibilityState.Invisible);
            }
            else if (layerData.ActiveSelf && !layerData.ActiveInHierarchy)
            {
                isActiveToggle.SetState(VisibilityState.VisibleInInvisible);
            }
            else if (allChildrenActive)
            {
                isActiveToggle.SetState(VisibilityState.Visible);
            }
            else
            {
                isActiveToggle.SetState(VisibilityState.PartiallyVisible);
            }
        }
        
        
        private void UpdateColorBar(Color newColor)
        {
            var opaqueColor = newColor;
            opaqueColor.a = 1;
            
            colorBar.style.backgroundColor = opaqueColor;
        }

        private void UpdateLayerTypeIcon()
        {
            layerTypeIcon.Image = GetImage(layerData);
        }

        private static IconImage GetImage(LayerData layerData)
        {
            return LayerTypeSpriteLibrary.GetIconImage(layerData);
        }
        
        private void UpdateNameLabel(LayerData layerData, string newName)
        {
            nameLabel.text = newName;
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