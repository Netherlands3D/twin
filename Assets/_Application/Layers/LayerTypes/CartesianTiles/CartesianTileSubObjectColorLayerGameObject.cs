using System.Collections.Generic;
using System.Linq;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles
{
    public class CartesianTileSubObjectColorLayerGameObject : LayerGameObject, IVisualizationWithPropertyData
    {
        private static CartesianTileSubObjectColorLayerGameObject activeColorLayer;
        private CartesianTileSubObjectColorPropertyData propertyData;

        public override BoundingBox Bounds => StandardBoundingBoxes.RDBounds;
        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
        }

        public UnityEvent<float> progressEvent = new();

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.OfType<CartesianTileSubObjectColorPropertyData>().FirstOrDefault();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            if (propertyData != null)
                propertyData.ColorsChanged += OnColorsChanged;
        }

        protected override void UnregisterEventListeners()
        {
            if (propertyData != null)
                propertyData.ColorsChanged -= OnColorsChanged;

            ClearColors();
            base.UnregisterEventListeners();
        }

        protected override void OnVisualizationReady()
        {
            if (propertyData?.Colors != null)
                progressEvent.Invoke(1f);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            UpdateColors(isActive);
        }

        private void OnColorsChanged()
        {
            UpdateColors(LayerData.ActiveInHierarchy);
            if (propertyData.Colors != null)
                progressEvent.Invoke(1f);
        }

        private void UpdateColors(bool isActive)
        {
            if (isActive && propertyData?.Colors is { Count: > 0 } colors)
            {
                activeColorLayer = this;
                Interaction.SetImportedBuildingColors(colors);
            }
            else
            {
                ClearColors();
            }
        }

        private void ClearColors()
        {
            if (!ReferenceEquals(activeColorLayer, this))
                return;

            activeColorLayer = null;
            Interaction.SetImportedBuildingColors(null);
        }

        private void OnDestroy()
        {
            if (propertyData != null)
                propertyData.ColorsChanged -= OnColorsChanged;

            ClearColors();
        }
    }
}
