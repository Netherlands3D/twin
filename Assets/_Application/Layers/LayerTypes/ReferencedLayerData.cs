using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Prefab")]
    public class ReferencedLayerData : LayerData
    {
        [DataMember] private string prefabId;
        [JsonIgnore] public LayerGameObject Reference { get; private set; }
        [JsonIgnore] public bool KeepReferenceOnDestroy { get; set; } = false;

        public ReferencedLayerData(string name, LayerGameObject reference) : base(name)
        {
            Reference = reference;
            prefabId = reference.PrefabIdentifier;

            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        [JsonConstructor]
        public ReferencedLayerData(string name, string prefabId, List<LayerPropertyData> layerProperties) : base(name, layerProperties)
        {
            this.prefabId = prefabId;
            ProjectData.Current.PrefabLibrary
                .Instantiate(prefabId)
                .Then(layerGameObject => OnSuccessfullyInstantiated(layerGameObject, layerProperties));
        }

        private LayerGameObject OnSuccessfullyInstantiated(
            LayerGameObject layerGameObject, 
            List<LayerPropertyData> layerProperties
        ) {
            Reference = layerGameObject;
            Reference.LayerData = this;
            Reference.gameObject.name = Name;
            this.layerProperties = layerProperties;

            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
            
            return layerGameObject;
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
                Reference.DestroyLayerGameObject();
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

        private void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            Reference.OnLayerActiveInHierarchyChanged(activeInHierarchy);
        }
    }
}