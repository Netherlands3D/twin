using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    [Serializable]
    public class LayerNL3DBase
    {
        [SerializeField, JsonProperty] private string name;
        [SerializeField, JsonProperty] private bool activeSelf = true;
        [SerializeField, JsonProperty] private Color color = new Color(86f / 256f, 160f / 256f, 227f / 255f);
        [SerializeField, JsonProperty] private LayerNL3DBase parent;
        [SerializeField, JsonProperty] private List<LayerNL3DBase> children = new();

        [JsonIgnore] public bool IsSelected { get; private set; }

        [JsonIgnore] public RootLayer Root => ProjectData.Current.RootLayer; //todo: when creating a layer the root layer reference should be set instead of this static reference
        [JsonIgnore] public LayerNL3DBase ParentLayer => parent;
        [JsonIgnore] public List<LayerNL3DBase> ChildrenLayers => children;

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
                foreach (var child in ChildrenLayers)
                {
                    child.LayerActiveInHierarchyChanged.Invoke(child.ActiveInHierarchy);
                }

                OnLayerActiveInHierarchyChanged(ActiveInHierarchy);
                LayerActiveInHierarchyChanged.Invoke(ActiveInHierarchy);
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

        [JsonIgnore]
        public int SiblingIndex
        {
            get
            {
                if (ParentLayer == null) //todo: this is only needed for the initial SetParent and should be removed if possible
                    return -1;

                return parent.ChildrenLayers.IndexOf(this);
            }
        }

        public readonly UnityEvent<string> NameChanged = new();
        public readonly UnityEvent<bool> LayerActiveInHierarchyChanged = new();
        public readonly UnityEvent<Color> ColorChanged = new();
        public readonly UnityEvent LayerDestroyed = new();

        public readonly UnityEvent<LayerNL3DBase> LayerSelected = new();
        public readonly UnityEvent<LayerNL3DBase> LayerDeselected = new();

        public readonly UnityEvent ParentChanged = new();
        public readonly UnityEvent ChildrenChanged = new();
        public readonly UnityEvent ParentOrSiblingIndexChanged = new();


        [JsonIgnore] public LayerUI UI { get; set; } //todo: remove

        [JsonIgnore] 
        public int Depth //todo: remove if possible
        {
            get
            {
                if (ParentLayer != Root)
                    return ParentLayer.Depth + 1;

                return 0;
            }
        }

        [JsonIgnore] 
        public bool ActiveInHierarchy
        {
            get
            {
                if (ParentLayer != null) //todo: if root layer is also of this type, maybe this check is unneeded
                    return ParentLayer.ActiveInHierarchy && activeSelf;

                return activeSelf;
            }
        }

        public virtual void SelectLayer(bool deselectOthers = false)
        {
            if (deselectOthers)
                Root.DeselectAllLayers();

            IsSelected = true;
            Root.AddLayerToSelection(this);
            LayerSelected.Invoke(this);
        }

        public virtual void DeselectLayer()
        {
            IsSelected = false;
            Root.RemoveLayerFromSelection(this);
            LayerDeselected.Invoke(this);
        }

        protected virtual void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
        }

        public LayerNL3DBase(string name)
        {
            Name = name;
        }

        protected virtual void OnSiblingIndexOrParentChanged(int newSiblingIndex) //called when the sibling index changes, or when the parent changes but the sibling index stays the same
        {
            UI?.SetParent(ParentLayer?.UI, newSiblingIndex);
            LayerActiveInHierarchyChanged.Invoke(UI?.State == LayerActiveState.Enabled || UI?.State == LayerActiveState.Mixed); // Update the active state to match the calculated state
        }

        public void SetParent(LayerNL3DBase newParent, int siblingIndex = -1)
        {
            Debug.Log("setting parent of: " + Name + " to: " + newParent?.Name);

            if (newParent == null)
                newParent = Root;

            if (newParent == this)
                return;

            var parentChanged = ParentLayer != newParent;
            var oldSiblingIndex = SiblingIndex;

            if (parent != null)
            {
                parent.children.Remove(this);
                if (!parentChanged && siblingIndex > oldSiblingIndex) //if the parent did not change, and the new sibling index is larger than the old sibling index, we need to decrease the new siblingIndex by 1 because we previously removed one item from the children list
                    siblingIndex--;

                parent.UI?.RecalculateCurrentTreeStates(); //todo: move to LayerUI
                parent.ChildrenChanged.Invoke();
            }

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;

            parent = newParent;
            Debug.Log("new parent: " + newParent);
            Debug.Log("children: " + children);

            newParent.children.Insert(siblingIndex, this);

            if (parentChanged || siblingIndex != oldSiblingIndex)
            {
                OnSiblingIndexOrParentChanged(siblingIndex);
            }

            if (parentChanged)
            {
                ParentChanged.Invoke();
                newParent.UI?.RecalculateCurrentTreeStates(); //todo: move to LayerUI
                newParent.ChildrenChanged.Invoke();
            }
        }

        public virtual void DestroyLayer()
        {
            DeselectLayer();
            ProjectData.Current.RemoveLayer(this);
            foreach (var child in ChildrenLayers)
            {
                child.DestroyLayer();
            }

            LayerDestroyed.Invoke();
        }
    }
}