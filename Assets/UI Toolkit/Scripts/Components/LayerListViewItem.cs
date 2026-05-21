using System.Collections.Generic;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class LayerListViewItem : VisualElement, IVisualizationWithPropertyData
    {
        private VisibilityToggle isActiveToggle;
        private VisualElement colorBar;
        private Icon layerTypeIcon;
        private Label nameLabel;
        private Toggle propertyToggle;

        PropertyPanelBehaviour propertyPanelBehaviour;

        private LayerData layerData => userData as LayerData;

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

            VisualElement el = this;
            while (el != null && !el.ClassListContains("unity-tree-view__item"))
                el = el.parent;
            if (el == null) return;

            var itemRoot = hierarchy.parent;
            if (itemRoot != null)
            {
                Debug.Log(itemRoot.name);
                // Find the index of the unity tree view toggle
                var treeToggle = itemRoot.Q<Toggle>(className: "unity-tree-view__item-toggle");
                if (treeToggle != null)
                {
                    int toggleIndex = itemRoot.hierarchy.IndexOf(treeToggle);
                    itemRoot.hierarchy.Insert(toggleIndex, colorBar);
                    itemRoot.hierarchy.Insert(toggleIndex, isActiveToggle);
                }
            }

            isActiveToggle.RegisterValueChangedCallback(OnIsActiveToggleChanged);
            propertyToggle.RegisterValueChangedCallback(OnPropertyToggleValueChanged);
        }

        private void UncheckPropertyToggle(LayerData layerData)
        {
            if(layerData == this.layerData)
                propertyToggle.SetValueWithoutNotify(false);
        }

        private void OnPropertyToggleValueChanged(ChangeEvent<bool> evt)
        {
            if(evt.newValue)
                propertyPanelBehaviour.SpawnPanel(layerData);
            else
                propertyPanelBehaviour.ClearActivePanel();
        }

        private void OnIsActiveToggleChanged(ChangeEvent<bool> evt)
        {
            layerData.ActiveSelf = evt.newValue;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyToggle.EnableInClassList(UtilityClassConstants.HIDDEN, !HasPropertiesWithPanel(properties));
        }

        // private void UpdateToggleFromChildren()
        // {
        //     if (layerData.ChildrenLayers.Count == 0)
        //         return;
        //
        //     bool anyOn = false;
        //     bool anyOff = false;
        //
        //     foreach (var child in layerData.ChildrenLayers)
        //     {
        //         var childMasking = child.GetProperty<MaskingLayerPropertyData>();
        //         if (childMasking == null) continue;
        //
        //         bool isOn = GetIsMaskingBitSet(childMasking);
        //         if (isOn)
        //             anyOn = true;
        //         else
        //             anyOff = true;
        //     }
        //
        //     if (anyOn && anyOff)
        //         isActiveToggle.SetState(VisibilityState.PartiallyVisible);
        //     else if (anyOn)
        //         isActiveToggle.SetState(VisibilityState.Visible);
        //     else
        //         isActiveToggle.SetState(VisibilityState.Invisible);
        // }

        public void Initialize(LayerData layerData)
        {
            userData = layerData;
            // isActiveToggle.SetState(layerData.);
            UpdateColorBar(layerData.Color);
            layerData.ColorChanged.AddListener(UpdateColorBar);
            layerTypeIcon.Image = GetImage(layerData);
            nameLabel.text = layerData.Name;

            LoadProperties(layerData.LayerProperties);
        }

        private void UpdateColorBar(Color newColor)
        {
            colorBar.style.backgroundColor = newColor;
        }

        private static IconImage GetImage(LayerData layerData)
        {
            return LayerTypeSpriteLibrary.GetIconImage(layerData);
        }

        public bool HasPropertiesWithPanel(List<LayerPropertyData> properties)
        {
            foreach (var property in properties)
            {
                var type = property.GetType();
                foreach(var collection in PropertySectionRegistry.TypeRegistry.Values)
                    if (collection.Collection.ContainsKey(type))
                        return true;

                foreach (var interfaceType in type.GetInterfaces())
                {
                    foreach(var collection in PropertySectionRegistry.TypeRegistry.Values)
                        if (collection.Collection.ContainsKey(interfaceType))
                            return true;
                }
            }

            return false;
        }
    }
}