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
        public ColorSetLayer ColorSetLayer;
        public override bool IsActiveInScene
        {
            get { return ColorSetLayer.Enabled; }
            set
            {
                if (ColorSetLayer != null)
                {
                    ColorSetLayer.Enabled = value;
                    GeometryColorizer.RecalculatePrioritizedColors();
                }
            }
        }
        
        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
        }
    }
}