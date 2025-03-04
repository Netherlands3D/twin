using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

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
            Debug.Log("Created referenced layer data from scratch");
            prefabId = reference.PrefabIdentifier;
            SetReference(reference);

            RegisterEventListeners();
        }

        [JsonConstructor]
        public ReferencedLayerData(string name, string prefabId, List<LayerPropertyData> layerProperties) : base(name, layerProperties)
        {
            Debug.Log("Created referenced layer data from project");
            this.prefabId = prefabId;
            this.layerProperties = layerProperties;

            ProjectData.Current.AddStandardLayer(this);

            SetReference(Object.Instantiate(ProjectData.Current.PrefabLibrary.fallbackPrefab));

            RegisterEventListeners();

            ProjectData.Current.PrefabLibrary
               .Instantiate(prefabId, this)
               .Then(SetReference);
        }

        ~ReferencedLayerData()
        {
            RemoveEventListeners();
        }

        private void SetReference(LayerGameObject newReference)
        {
            if (Reference && Reference != newReference)
            {
                Reference.DestroyLayerGameObject();
                ProjectData.Current.RemoveLayer(this);
            }
            Reference = newReference;
            Reference.LayerData = this;
            Reference.gameObject.name = Name;
           
            // Trigger all events on the new layer game object so that it is properly initialized
            // OnParentChanged();
            // OnChildrenChanged();
            // OnSiblingIndexOrParentChanged(SiblingIndex);
            // OnLayerActiveInHierarchyChanged(ActiveInHierarchy);

            // Cause any selection handling on the Reference to trigger so that it is in the right state
            if (IsSelected)
            {
                Reference.OnSelect();
            }
            else
            {
                Reference.OnDeselect();
            }
        }

        private void RegisterEventListeners()
        {
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        private void RemoveEventListeners()
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
            {
                Reference.DestroyLayerGameObject();
            }
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
            Debug.Log(Reference);
            Reference.OnLayerActiveInHierarchyChanged(activeInHierarchy);
        }
    }
}