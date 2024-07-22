using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class LayerNL3DBase
    {
        [SerializeField, JsonProperty] protected string name;
        [SerializeField, JsonProperty] protected bool activeSelf = true;
        [SerializeField, JsonProperty] protected Color color = new Color(86f / 256f, 160f / 256f, 227f / 255f);
        [SerializeField, JsonProperty] protected List<LayerNL3DBase> children = new();
        [JsonIgnore] protected LayerNL3DBase parent; //not serialized to avoid a circular reference

        [JsonIgnore] public RootLayer Root => ProjectData.Current.RootLayer;
        [JsonIgnore] public LayerNL3DBase ParentLayer => parent;

        [JsonIgnore] public List<LayerNL3DBase> ChildrenLayers => children;
        [JsonIgnore] public bool IsSelected => Root.SelectedLayers.Contains(this);

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

        [JsonIgnore] public int SiblingIndex => parent.ChildrenLayers.IndexOf(this);

        [JsonIgnore]
        public bool ActiveInHierarchy
        {
            get
            {
                if (this is RootLayer)
                    return activeSelf;

                return ParentLayer.ActiveInHierarchy && activeSelf;
            }
        }

        [JsonIgnore] public readonly UnityEvent<string> NameChanged = new();
        [JsonIgnore] public readonly UnityEvent<bool> LayerActiveInHierarchyChanged = new();
        [JsonIgnore] public readonly UnityEvent<Color> ColorChanged = new();
        [JsonIgnore] public readonly UnityEvent LayerDestroyed = new();

        [JsonIgnore] public readonly UnityEvent<LayerNL3DBase> LayerSelected = new();
        [JsonIgnore] public readonly UnityEvent<LayerNL3DBase> LayerDeselected = new();

        [JsonIgnore] public readonly UnityEvent ParentChanged = new();
        [JsonIgnore] public readonly UnityEvent ChildrenChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> ParentOrSiblingIndexChanged = new();

        public void InitializeParent(LayerNL3DBase initialParent)
        { 
            parent = initialParent;
            
            if (initialParent == null)
            {
                parent = Root;
            }

            // if (!parent.ChildrenLayers.Contains(this))
            // {
            //     parent.ChildrenLayers.Add(this);
            //     parent.ChildrenChanged.Invoke();
            // }
        }

        //needed because after deserialization of the Layer objects, the parent field is not set yet.
        // public void ReconstructParentsRecursive()
        // {
        //     Debug.Log( "reconstructing recursive: " + Name +"\t"+ ChildrenLayers.Count);
        //     foreach (var layer in ChildrenLayers)
        //     {
        //         ReconstructParentsRecursive(layer, this);
        //     }
        // }

        // public void ReconstructParentsRecursive(LayerNL3DBase layer, LayerNL3DBase parent)
        // {
        //     Debug.Log(layer.Name + " setting parent to: " + parent.name);
        //     layer.parent = parent;
        //     foreach (var child in layer.ChildrenLayers)
        //     {
        //         ReconstructParentsRecursive(child, layer);
        //     }
        // }

        public virtual void SelectLayer(bool deselectOthers = false)
        {
            if (deselectOthers)
                Root.DeselectAllLayers();

            Root.AddLayerToSelection(this);
            LayerSelected.Invoke(this);
        }

        public virtual void DeselectLayer()
        {
            Root.RemoveLayerFromSelection(this);
            LayerDeselected.Invoke(this);
        }

        protected virtual void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
        }

        public LayerNL3DBase(string name)
        {
            Name = name;
            if(this is not RootLayer) //todo: maybe move to inherited classes so this check is not needed?
                InitializeParent(null);

            // Debug.Log(parent);
            // if (this is not RootLayer)
            // {
            //     if (parent == null)
            //         parent = Root;
            // }

            // Root.ReconstructParentsRecursive();
        }

        public void SetParent(LayerNL3DBase newParent, int siblingIndex = -1)
        {
            if (newParent == null)
                newParent = Root;

            if (newParent == this)
                return;

            var parentChanged = ParentLayer != newParent;
            var oldSiblingIndex = SiblingIndex;

            parent.children.Remove(this);
            if (!parentChanged && siblingIndex > oldSiblingIndex) //if the parent did not change, and the new sibling index is larger than the old sibling index, we need to decrease the new siblingIndex by 1 because we previously removed one item from the children list
                siblingIndex--;
            parent.ChildrenChanged.Invoke(); //call event on old parent

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
                newParent.ChildrenChanged.Invoke(); //call event on new parent
            }
        }

        public virtual void DestroyLayer()
        {
            DeselectLayer();

            foreach (var child in ChildrenLayers.ToList()) //use ToList to make a copy and avoid a CollectionWasModified error
            {
                child.DestroyLayer();
            }

            ParentLayer.ChildrenLayers.Remove(this);
            parent.ChildrenChanged.Invoke(); //call event on old parent

            ProjectData.Current.RemoveLayer(this);
            LayerDestroyed.Invoke();
        }
    }
}