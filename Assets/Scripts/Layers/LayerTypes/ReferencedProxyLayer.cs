using System;
using System.Collections;
using System.Collections.Generic;
using CsvHelper;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ReferencedProxyLayer : LayerNL3DBase
    {
        public ReferencedLayer Reference { get; set; }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            Reference.IsActiveInScene = activeInHierarchy;
        }

        private void OnEnable()
        {
            if (Reference)
                Reference.IsActiveInScene = true;
        }

        private void OnDisable()
        {
            if (Reference)
                Reference.IsActiveInScene = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Reference)
                Destroy(Reference.gameObject);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            Reference.OnSelect();
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            Reference.OnDeselect();
        }

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            Reference.OnProxyTransformChildrenChanged();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            Reference.OnProxyTransformParentChanged();
        }
    }
}