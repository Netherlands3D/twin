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
    public abstract class LayerNL3DBase : MonoBehaviour
    {
        [SerializeField, JsonProperty] private string name;
        [SerializeField, JsonProperty] private bool activeSelf = true;
        [SerializeField, JsonProperty] private Color color = new Color(86f / 256f, 160f / 256f, 227f / 255f);
        [SerializeField, JsonProperty] private LayerNL3DBase parent;
        [SerializeField, JsonProperty] private List<LayerNL3DBase> children = new();
        [JsonIgnore] public bool IsSelected { get; private set; }

        public RootLayer Root => ProjectData.RootLayer; //todo: when creating a layer the root layer reference should be set instead of this static reference
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

        public UnityEvent<LayerNL3DBase> LayerSelected = new();
        public UnityEvent<LayerNL3DBase> LayerDeselected = new();
        
        public LayerUI UI { get; set; } //todo: remove

        public int Depth //todo: remove if possible
        {
            get
            {
                if (transform.parent != LayerData.Instance.transform)
                    return ParentLayer.Depth + 1;
                else
                    return 0;
            }
        } 

        public bool ActiveInHierarchy
        {
            get
            {
                if (ParentLayer != null) //todo: if root layer is also of this type, maybe this check is unneeded
                    return ParentLayer.ActiveInHierarchy && activeSelf;

                return activeSelf;
            }
        }

        public void SelectLayer(bool deselectOthers = false)
        {
            if(deselectOthers)
                Root.DeselectAllLayers();
            
            IsSelected = true;
            Root.AddLayerToSelection(this);
            LayerSelected.Invoke(this);
        }

        public void DeselectLayer()
        {
            IsSelected = false;
            Root.RemoveLayerFromSelection(this);
            LayerDeselected.Invoke(this);
        }
        
        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;

            LayerDestroyed.Invoke();

            LayerData.RemoveLayer(this);
            DeleteLayer();
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

        // public void SetParent2(LayerNL3DBase newParent, int siblingIndex)
        // {
        //     Debug.Log("setting parent of: " + Name + " to: " + newParent?.Name);
        //     
        //     if (newParent == null)
        //         newParent = Root;
        //
        //     if (parent != null)
        //         parent.children.Remove(this);
        //
        //     if (siblingIndex < 0)
        //         siblingIndex = newParent.children.Count;
        //
        //     parent = newParent;
        //     Debug.Log("new parent: " + newParent);
        //     Debug.Log("children: " + children);
        //     newParent.children.Insert(siblingIndex, this);
        // }
        
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

            UI?.SetParent(newParentLayer?.UI, siblingIndex);

            LayerActiveInHierarchyChanged.Invoke(UI?.State == LayerActiveState.Enabled || UI?.State == LayerActiveState.Mixed); // Update the active state to match the calculated state

            if (siblingIndex == -1)
                siblingIndex = newParent.childCount - 1;
            if (parentChanged || siblingIndex != oldSiblingIndex)
            {
                OnSiblingIndexOrParentChanged(siblingIndex);
            }
        }

        public void DeleteLayer()
        {
            DeselectLayer();
            Destroy(UI.gameObject);
        }
        
    }
}