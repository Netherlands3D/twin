using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        public LayerProjectData ProjectData = new();
        public string Name
        {
            get => ProjectData.Name;
            set
            {
                gameObject.name = value;
                ProjectData.Name = value;
            }
        }
        
        public LayerUI UI { get; set; }

        public int Depth { get; private set; } = 0;

        public bool ActiveSelf
        {
            get { return ProjectData.IsActive; }
            set
            {
                gameObject.SetActive(value);
                ProjectData.IsActive = value;
                OnLayerActiveInHierarchyChanged(value);
                foreach (var child in ChildrenLayers)
                {
                    child.OnLayerActiveInHierarchyChanged(child.ActiveInHierarchy);
                }

                UI?.UpdateEnabledToggle(value);
            }
        }

        public bool ActiveInHierarchy
        {
            get { return gameObject.activeInHierarchy; }
        }

        private Color color = new Color(86f / 256f, 160f / 256f, 227f / 255f);
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                if (UI)
                    UI.SetColor(value);
            }
        }

        protected abstract void OnLayerActiveInHierarchyChanged(bool activeInHierarchy);

        public LayerNL3DBase ParentLayer { get; private set; } //=> transform.parent.GetComponent<LayerNL3DBase>();

        public LayerNL3DBase[] ChildrenLayers { get; private set; }

        public virtual void OnSelect(){ }

        public virtual void OnDeselect(){ }

        protected virtual void OnDestroy()
        {
            if(!Application.isPlaying) return;

            if(UI) UI.DestroyUI();
            
            LayerData.RemoveLayer(this);
        }

        private void Awake()
        {
            ProjectData.Name = name;
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
        
        protected virtual void OnTransformChildrenChanged()
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

        protected virtual void OnTransformParentChanged()
        {
            ParentLayer = transform.parent.GetComponent<LayerNL3DBase>();
        }

        protected virtual void OnSiblingIndexOrParentChanged(int newSiblingIndex) //called when the sibling index changes, or when the parent changes but the sibling index stays the same
        {
        }

        public void SetParent(LayerNL3DBase newParentLayer, int siblingIndex = -1)
        {
            if (newParentLayer == this)
                return;

            var parentChanged = ParentLayer != newParentLayer;
            var oldSiblingIndex = transform.GetSiblingIndex();
            var newParent = newParentLayer ? newParentLayer.transform : LayerData.Instance.transform;

            if (newParentLayer == null)
                transform.SetParent(LayerData.Instance.transform);
            else
                transform.SetParent(newParent);

            transform.SetSiblingIndex(siblingIndex);

            RecalculateCurrentSubTreeDepthValuesRecursively();
            UI?.SetParent(newParentLayer?.UI, siblingIndex);

            OnLayerActiveInHierarchyChanged(UI?.State == LayerActiveState.Enabled || UI?.State == LayerActiveState.Mixed); // Update the active state to match the calculated state

            if (siblingIndex == -1)
                siblingIndex = newParent.childCount -1;
            if (parentChanged || siblingIndex != oldSiblingIndex)
            {
                OnSiblingIndexOrParentChanged(siblingIndex);
            }

            ProjectData.SetParent(newParentLayer?.ProjectData, siblingIndex);
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