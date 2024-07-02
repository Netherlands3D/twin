using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        [SerializeField, JsonProperty] private string name;
        [SerializeField, JsonProperty] private bool activeSelf = true;
        [SerializeField, JsonProperty] private Color color = new Color(86f / 256f, 160f / 256f, 227f / 255f);
        [SerializeField, JsonProperty] private LayerNL3DBase parent;
        [SerializeField, JsonProperty] private List<LayerNL3DBase> children = new();
        
        private static LayerNL3DBase rootLayer;
        public LayerNL3DBase ParentLayer => parent;
        public List<LayerNL3DBase> ChildrenLayers => children;
        
        [JsonIgnore]
        public string Name
        {
            get => name;
            set
            {
                name = value;
                NameChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public bool ActiveSelf
        {
            get => activeSelf;
            set
            {
                activeSelf = value;
                // gameObject.SetActive(value);
                foreach (var child in ChildrenLayers)
                {
                    child.LayerActiveInHierarchyChanged.Invoke(child.ActiveInHierarchy);
                }

                LayerActiveInHierarchyChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                ColorChanged.Invoke(value);
            }
        }

        public UnityEvent<string> NameChanged = new();
        public UnityEvent<bool> LayerActiveInHierarchyChanged = new();
        public UnityEvent<Color> ColorChanged = new();
        public UnityEvent LayerDestroyed = new();

        public LayerUI UI { get; set; } //todo: remove

        public int Depth { get; private set; } = 0; //todo: remove if possible

        public bool ActiveInHierarchy
        {
            get
            {
                if (ParentLayer != null) //todo: if root layer is also of this type, maybe this check is unneeded
                    return ParentLayer.ActiveInHierarchy && activeSelf;

                return activeSelf;
            }
        }


        public virtual void OnSelect()
        {
        }

        public virtual void OnDeselect()
        {
        }

        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;

            LayerDestroyed.Invoke();

            LayerData.RemoveLayer(this);
        }

        private void Awake()
        {
            Name = gameObject.name;
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

            children = childLayers.ToList();
            UI?.RecalculateCurrentTreeStates();
        }

        protected virtual void OnTransformParentChanged()
        {
            parent = transform.parent.GetComponent<LayerNL3DBase>();
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

            LayerActiveInHierarchyChanged.Invoke(UI?.State == LayerActiveState.Enabled || UI?.State == LayerActiveState.Mixed); // Update the active state to match the calculated state

            if (siblingIndex == -1)
                siblingIndex = newParent.childCount - 1;
            if (parentChanged || siblingIndex != oldSiblingIndex)
            {
                OnSiblingIndexOrParentChanged(siblingIndex);
            }

            // ProjectData.SetParent(newParentLayer?.ProjectData, siblingIndex);
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