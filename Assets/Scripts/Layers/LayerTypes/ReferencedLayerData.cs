using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.Properties;
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
        public ReferencedLayerData(string name, string prefabId, HashSet<LayerPropertyData> layerProperties) : base(name, layerProperties)
        {
            this.prefabId = prefabId;
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            Debug.Log("loading prefab: " + prefab + " with id: " +prefabId);
            Reference = GameObject.Instantiate(prefab);
            Reference.LayerData = this;

            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        ~ReferencedLayerData()
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