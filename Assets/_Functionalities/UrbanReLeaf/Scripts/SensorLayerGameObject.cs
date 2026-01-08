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
        
        protected override void OnVisualizationInitialize()
        {
            SensorProjectionLayer = GetComponent<SensorProjectionLayer>();
            base.OnVisualizationInitialize();
        }

        protected override void OnVisualizationReady()
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
            
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
        }

        private void RefreshTiles<T>(T value)
        {
            SensorProjectionLayer.RefreshTiles();
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<SensorPropertyData>(properties, null, 
                SensorProjectionLayer.SensorDataController.defaultMinValue,
                SensorProjectionLayer.SensorDataController.defaultMaxValue,
                SensorProjectionLayer.SensorDataController.defaultMinColor,
                SensorProjectionLayer.SensorDataController.defaultMaxColor,
                SensorProjectionLayer.SensorDataController.DefaultStartDate,
                SensorProjectionLayer.SensorDataController.DefaultEndDate);
            
            SensorProjectionLayer.SensorDataController.LoadProperties(properties);
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