using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class DatasetLayer : LayerNL3DBase
    {
        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            ColorSetLayer.Enabled = activeInHierarchy;
            GeometryColorizer.RecalculatePrioritizedColors();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveCustomColorSet();
        }

        public void RemoveCustomColorSet()
        {
            if (ColorSetLayer != null)
                GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
        }

        public void SetColorSetLayer(ColorSetLayer newColorSetLayer, bool updateColors = true)
        {
            // bool active = newColorSetLayer.Enabled;
            ColorSetLayer = newColorSetLayer;
            if (updateColors)
                GeometryColorizer.RecalculatePrioritizedColors();
        }

        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            var hierarchyList = LayerData.Instance.transform.GetComponentsInChildren<LayerNL3DBase>();
            for (var i = 0; i < hierarchyList.Length; i++)
            {
                var layer = hierarchyList[i];
                if (layer is DatasetLayer)
                {
                    var datasetLayer = (DatasetLayer)layer;
                    datasetLayer.ColorSetLayer.PriorityIndex = i;
                }
            }

            GeometryColorizer.RecalculatePrioritizedColors();
        }
    }
}