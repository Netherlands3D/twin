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

        // private static Transform parentTransform;
        //
        // public static Transform ParentTransform
        // {
        //     get
        //     {
        //         if (!parentTransform)
        //         {
        //             var go = new GameObject("Datasets");
        //             parentTransform = go.transform;
        //         }
        //
        //         return parentTransform;
        //     }
        // }

        public override bool IsActiveInScene
        {
            get { return ColorSetLayer.Enabled; }
            set
            {
                ColorSetLayer.Enabled = value;
                GeometryColorizer.RecalculatePrioritizedColors();
            }
        }

        // void Test()
        // {
        //     foreach (var v in GeometryColorizer.PrioritizedColors)
        //     {
        //         print(v.Key + "\t" + v.Value);
        //     }
        // }
        // void Test2()
        // {
        //     print("---"+gameObject.name);
        //     print(ColorSetLayer.PriorityIndex);
        //     foreach (var v in ColorSetLayer.ColorSet)
        //     {
        //         print(v.Key + "\t" + v.Value);
        //     }
        //     print("---");
        // }
        //
        // private void Update()
        // {
        //     Test2();
        // }
        // public Dictionary<string, Color> ColorDataset { get; set; }

        public int PriorityIndex
        {
            get { return transform.GetSiblingIndex(); }
            set { transform.SetSiblingIndex(value); }
        }

        public void OnLayerParentOrOrderChanged()
        {
        }

        // private void Start()
        // {
        //     if (transform.parent != ParentTransform)
        //         transform.SetParent(ParentTransform);
        // }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            GeometryColorizer.RemoveCustomColorSet(ColorSetLayer.PriorityIndex);
        }
    }
}
