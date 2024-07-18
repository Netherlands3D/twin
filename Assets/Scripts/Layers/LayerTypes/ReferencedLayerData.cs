using System;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    [Serializable]
    public class ReferencedLayerData : LayerData
    {
        [SerializeField, JsonProperty] private string prefabId;
        [JsonIgnore] public LayerGameObject Reference { get; }
        [JsonIgnore] public bool KeepReferenceOnDestroy { get; set; } = false;

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

        public ReferencedLayerData(string name, LayerGameObject reference) : base(name)
        {
            Reference = reference;
            prefabId = reference.PrefabIdentifier;
            
            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
        }

        [JsonConstructor]
        public ReferencedLayerData(string name, string prefabId) : base(name)
        {
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            Reference = GameObject.Instantiate(prefab);
            Reference.LayerData = this;
            
            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
        }

        ~ReferencedLayerData()
        {
            ParentChanged.RemoveListener(OnParentChanged);
            ChildrenChanged.RemoveListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.RemoveListener(OnSiblingIndexOrParentChanged);
        }
    }
}