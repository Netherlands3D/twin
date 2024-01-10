using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        public LayerUI UI { get; set; }

        private LayerNL3DBase ParentLayer { get; set; }
        private LayerNL3DBase[] ChildrenLayers { get; set; }
        public int Depth { get; private set; } = 0;
        
        public abstract bool IsActiveInScene { get; set; }

        public virtual void OnSelect()
        {
        }

        public virtual void OnDeselect()
        {
        }

        protected virtual void Awake()
        {
            if (!LayerData.AllLayers.Contains(this))
                LayerData.AddStandardLayer(this);
        }

        protected virtual void OnDestroy()
        {
            LayerData.RemoveLayer(this);
            UI.del
        }

        public void SetParent(LayerNL3DBase newParentLayer, int siblingIndex = -1)
        {
            if (newParentLayer == this)
                return;
            
            var newParent = newParentLayer ? newParentLayer.transform : LayerData.Instance.transform;

            if (newParentLayer == null)
                transform.SetParent(LayerData.Instance.transform);
            else
                transform.SetParent(newParent);
            
            transform.SetSiblingIndex(siblingIndex);
            
            print("setting parent of " + name + " to " + newParentLayer?.name);
            RecalculateCurrentSubTreeDepthValuesRecursively();
            UI?.SetParent(newParentLayer?.UI, siblingIndex);
        }
        
        private void RecalculateCurrentSubTreeDepthValuesRecursively()
        {
            if (transform.parent != LayerData.Instance.transform)
                Depth = transform.parent.GetComponent<LayerNL3DBase>().Depth + 1;
            else
                Depth = 0;

            foreach (var child in GetComponentsInChildren<LayerNL3DBase>())
            {
                if(child == this)
                    continue;
                
                child.RecalculateCurrentSubTreeDepthValuesRecursively();
            }
        }
    }
}