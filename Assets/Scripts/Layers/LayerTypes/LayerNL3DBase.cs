using System;
using System.Linq;
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

        public int Depth { get; private set; } = 0;

        // public abstract bool IsActiveInScene { get; set; }
        // public bool ActiveSelf { get; set; } = true;
        //
        // public bool IsActiveInHierarchy
        // {
        //     get
        //     {
        //         if (ParentLayer)
        //             return ActiveSelf && ParentLayer.IsActiveInHierarchy;
        //         return ActiveSelf;
        //     }
        // }

        public bool ActiveSelf
        {
            get { return gameObject.activeSelf; }
            set
            {
                gameObject.SetActive(value);
                OnLayerActiveInHierarchyChanged(value);
                foreach (var child in ChildrenLayers)
                {
                    child.OnLayerActiveInHierarchyChanged(child.ActiveInHierarchy);
                }
            }
        }

        public bool ActiveInHierarchy
        {
            get { return gameObject.activeInHierarchy; }
        }

        protected abstract void OnLayerActiveInHierarchyChanged(bool activeInHierarchy);

        public LayerNL3DBase ParentLayer => transform.parent.GetComponent<LayerNL3DBase>();

        public LayerNL3DBase[] ChildrenLayers
        {
            get
            {
                LayerNL3DBase[] childLayers = GetComponentsInChildren<LayerNL3DBase>(true);

                LayerNL3DBase selfLayer = GetComponent<LayerNL3DBase>();
                if (selfLayer != null)
                {
                    childLayers = childLayers.Where(layer => layer != selfLayer).ToArray();
                }

                return childLayers;
            }
        }

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
            UI?.SetParent(null); //unparent before deleting to avoid UI being destroyed multiple times (through DestroyUI and as a consequence of Destroying the parent) 
            UI?.DestroyUI();
            LayerData.RemoveLayer(this);
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
                Depth = ParentLayer.Depth + 1;
            else
                Depth = 0;

            foreach (var child in GetComponentsInChildren<LayerNL3DBase>())
            {
                if (child == this)
                    continue;

                child.RecalculateCurrentSubTreeDepthValuesRecursively();
            }
        }
    }
}