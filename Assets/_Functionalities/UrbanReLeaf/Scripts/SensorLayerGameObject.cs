using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    [RequireComponent(typeof(SensorProjectionLayer))]
    public class SensorLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData
    {
        private SensorProjectionLayer SensorProjectionLayer { get; set; }
        
        protected override void OnLayerInitialize()
        {
            SensorProjectionLayer = GetComponent<SensorProjectionLayer>();
            base.OnLayerInitialize();
        }

        protected override void OnLayerReady()
        {
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
        }

        //a higher order means rendering over lower indices
        private void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            SensorProjectionLayer.RenderIndex = -order;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            SensorPropertyData propertyData = LayerData.GetProperty<SensorPropertyData>();
            propertyData.OnStartDateChanged.AddListener(RefreshTiles);
            propertyData.OnEndDateChanged.AddListener(RefreshTiles);
            propertyData.OnMinValueChanged.AddListener(RefreshTiles);
            propertyData.OnMaxValueChanged.AddListener(RefreshTiles);
            propertyData.OnMinColorChanged.AddListener(RefreshTiles);
            propertyData.OnMaxColorChanged.AddListener(RefreshTiles);
            propertyData.OnResetValues.AddListener(ResetValues);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            SensorPropertyData propertyData = LayerData.GetProperty<SensorPropertyData>();
            propertyData.OnStartDateChanged.RemoveListener(RefreshTiles);
            propertyData.OnEndDateChanged.RemoveListener(RefreshTiles);
            propertyData.OnMinValueChanged.RemoveListener(RefreshTiles);
            propertyData.OnMaxValueChanged.RemoveListener(RefreshTiles);
            propertyData.OnMinColorChanged.RemoveListener(RefreshTiles);
            propertyData.OnMaxColorChanged.RemoveListener(RefreshTiles);
            propertyData.OnResetValues.RemoveListener(ResetValues);
        }

        private void RefreshTiles<T>(T value)
        {
            SensorProjectionLayer.RefreshTiles();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<SensorPropertyData>(properties, null, 
                SensorProjectionLayer.SensorDataController.Minimum,
                SensorProjectionLayer.SensorDataController.Maximum,
                SensorProjectionLayer.SensorDataController.MinColor,
                SensorProjectionLayer.SensorDataController.MaxColor,
                SensorProjectionLayer.SensorDataController.DefaultStartDate,
                SensorProjectionLayer.SensorDataController.DefaultEndDate);
        }

        public void ResetValues()
        {
            SensorPropertyData propertyData = LayerData.GetProperty<SensorPropertyData>();
            propertyData.MinValue = SensorProjectionLayer.SensorDataController.defaultMinValue;
            propertyData.MaxValue = SensorProjectionLayer.SensorDataController.defaultMaxValue;
            propertyData.MinColor = SensorProjectionLayer.SensorDataController.defaultMinColor;
            propertyData.MaxColor = SensorProjectionLayer.SensorDataController.defaultMaxColor;
            propertyData.StartDate = SensorProjectionLayer.SensorDataController.DefaultStartDate;
            propertyData.EndDate = SensorProjectionLayer.SensorDataController.DefaultEndDate;
        }
    }
}