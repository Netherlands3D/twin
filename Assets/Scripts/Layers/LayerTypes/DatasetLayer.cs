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

        // public override bool IsActiveInScene
        // {
        //     get { return ColorSetLayer.Enabled; }
        //     set
        //     {
        //         ColorSetLayer.Enabled = value;
        //         GeometryColorizer.RecalculatePrioritizedColors();
        //     }
        // }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            ColorSetLayer.Enabled = activeInHierarchy;
            GeometryColorizer.RecalculatePrioritizedColors();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
        }

        public void SetColorSetLayer(ColorSetLayer newColorSetLayer)
        {
            // bool active = newColorSetLayer.Enabled;
            ColorSetLayer = newColorSetLayer;
            GeometryColorizer.RecalculatePrioritizedColors();
        }

        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
        }
    }
}