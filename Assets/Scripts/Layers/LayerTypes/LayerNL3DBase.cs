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

        [JsonIgnore] private RootLayer root;
        [JsonIgnore]
        public RootLayer Root
        {
            get
            {
                return root;
            }
            set
            {
                root = value;
                parent = root;
                root.children.Add(this);
                root.ChildrenChanged.Invoke();
            }
        }
        [JsonIgnore] public LayerNL3DBase ParentLayer => parent;
        [JsonIgnore] public List<LayerNL3DBase> ChildrenLayers => children;
        [JsonIgnore] public bool IsSelected { get; private set; }

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
        public int SiblingIndex => parent.ChildrenLayers.IndexOf(this);

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
        
        public readonly UnityEvent<string> NameChanged = new();
        public readonly UnityEvent<bool> LayerActiveInHierarchyChanged = new();
        public readonly UnityEvent<Color> ColorChanged = new();
        public readonly UnityEvent LayerDestroyed = new();

        public readonly UnityEvent<LayerNL3DBase> LayerSelected = new();
        public readonly UnityEvent<LayerNL3DBase> LayerDeselected = new();

        public readonly UnityEvent ParentChanged = new();
        public readonly UnityEvent ChildrenChanged = new();
        public readonly UnityEvent<int> ParentOrSiblingIndexChanged = new();
        
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

        public void SetParent(LayerNL3DBase newParent, int siblingIndex = -1)
        {
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

                parent.ChildrenChanged.Invoke();
            }

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;

            parent = newParent;

            newParent.children.Insert(siblingIndex, this);

            if (parentChanged || siblingIndex != oldSiblingIndex)
            {
                LayerActiveInHierarchyChanged.Invoke(ActiveInHierarchy); // Update the active state to match the calculated state
                ParentOrSiblingIndexChanged.Invoke(siblingIndex);
            }

            if (parentChanged)
            {
                ParentChanged.Invoke();
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