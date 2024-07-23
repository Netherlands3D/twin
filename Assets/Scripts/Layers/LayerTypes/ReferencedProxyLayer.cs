using System;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class ReferencedProxyLayer : LayerNL3DBase
    {
        [SerializeField, JsonProperty] private string prefabId;
        [JsonIgnore] public ReferencedLayer Reference { get; }
        [JsonIgnore] public bool KeepReferenceOnDestroy { get; set; } = false;
        
        public ReferencedProxyLayer(string name, ReferencedLayer reference) : base(name)
        {
            Reference = reference;
            prefabId = reference.PrefabIdentifier;
            
            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
        }

        [JsonConstructor]
        public ReferencedProxyLayer(string name, string prefabId) : base(name)
        {
            this.prefabId = prefabId;
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            Reference = GameObject.Instantiate(prefab);
            Reference.ReferencedProxy = this;
            
            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        ~ReferencedProxyLayer()
        {
            ParentChanged.RemoveListener(OnParentChanged);
            ChildrenChanged.RemoveListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.RemoveListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (!KeepReferenceOnDestroy && Reference)
                GameObject.Destroy(Reference.gameObject);
        }

        public override void SelectLayer(bool deselectOthers = false)
        {
            base.SelectLayer(deselectOthers);
            Reference.OnSelect();
        }

        public override void DeselectLayer()
        {
            base.DeselectLayer();
            Reference.OnDeselect();
        }

        private void OnChildrenChanged()
        {
            Reference.OnProxyTransformChildrenChanged();
        }

        private void OnParentChanged()
        {
            Reference.OnProxyTransformParentChanged();
        }

        private void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            Reference.OnSiblingIndexOrParentChanged(newSiblingIndex);
        }

        protected override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            base.OnLayerActiveInHierarchyChanged(activeInHierarchy);
            Reference.OnLayerActiveInHierarchyChanged(activeInHierarchy);
        }
    }
}