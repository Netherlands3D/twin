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
        
        private void OnEnable()
        {
            if (Reference)
                ActiveSelf = true;
            
            LayerSelected.AddListener(OnSelect);
            LayerDeselected.AddListener(OnDeselect);
        }

        private void OnDisable()
        {
            if (Reference)
                ActiveSelf = false;

            LayerSelected.RemoveListener(OnSelect);
            LayerDeselected.RemoveListener(OnDeselect);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Reference)
                GameObject.Destroy(Reference.gameObject);
        }

        private void OnSelect(LayerNL3DBase layer)
        {
            Reference.OnSelect();
        }

        private void OnDeselect(LayerNL3DBase layer)
        {
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

        protected override void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            base.OnSiblingIndexOrParentChanged(newSiblingIndex);
            Reference.OnSiblingIndexOrParentChanged(newSiblingIndex);
        }
    }
}