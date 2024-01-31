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

        public LayerNL3DBase ParentLayer { get; private set; } //=> transform.parent.GetComponent<LayerNL3DBase>();

        public LayerNL3DBase[] ChildrenLayers { get; private set; }

        public virtual void OnSelect()
        {
        }

        public virtual void OnDeselect()
        {
        }

        protected virtual void OnDestroy()
        {
            UI?.DestroyUI();
            LayerData.RemoveLayer(this);
        }

        protected virtual void Start()
        {
            if (!LayerData.AllLayers.Contains(this))
                LayerData.AddStandardLayer(this);

            //for initialization calculate the parent and children here
            OnTransformParentChanged();
            OnTransformChildrenChanged();

            foreach (var child in ChildrenLayers)
            {
                child.UI.SetParent(UI); //Update the parents to be sure the hierarchy matches. needed for example when grouping selected layers that make multiple hierarchy adjustments in one frame
            }
        }

        private void OnTransformChildrenChanged()
        {
            LayerNL3DBase[] childLayers = GetComponentsInChildren<LayerNL3DBase>(true);

            LayerNL3DBase selfLayer = GetComponent<LayerNL3DBase>();
            if (selfLayer != null)
            {
                childLayers = childLayers.Where(layer => layer != selfLayer).ToArray();
            }

            ChildrenLayers = childLayers;
            UI?.RecalculateCurrentTreeStates();
        }

        private void OnTransformParentChanged()
        {
            ParentLayer = transform.parent.GetComponent<LayerNL3DBase>();
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