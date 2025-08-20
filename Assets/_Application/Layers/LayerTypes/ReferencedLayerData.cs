using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Prefab")]
    public class ReferencedLayerData : LayerData
    {
        [DataMember] private string prefabId;
        public string TypeOfLayer => prefabId;

        [JsonIgnore] private LayerGameObject reference;
        [JsonIgnore]
        public LayerGameObject Reference
        {
            get => reference;
            private set
            {
                if (reference == value) return;

                if (reference)
                {
                    reference.DestroyLayerGameObject();
                }

                reference = value;
                if (!reference) return;
    
                reference.LayerData = this;
                reference.gameObject.name = Name;
                prefabId = reference.PrefabIdentifier;
            }
        }

        [JsonIgnore] public bool KeepReferenceOnDestroy { get; set; } = false;
        [JsonIgnore] public UnityEvent OnReferenceChanged = new();

        public ReferencedLayerData(string name, LayerGameObject reference) : base(name)
        {
            Reference = reference;

            //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ProjectData.Current.AddStandardLayer(this);
            RegisterEventListeners();
        }

        public ReferencedLayerData(string name, string prefabId, LayerGameObject reference) : this(name, reference)
        {
            this.prefabId = prefabId;
        }

        [JsonConstructor]
        public ReferencedLayerData(string name, string prefabId, List<LayerPropertyData> layerProperties) : base(name, layerProperties)
        {
            Name = name;
            this.prefabId = prefabId;
            SpawnLayer(layerProperties);
        }

        private async void SpawnLayer(List<LayerPropertyData> layerProperties)
        {
            var prefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            
            var layerGameObjects = await Object.InstantiateAsync(prefab);
            var layerGameObject = layerGameObjects.FirstOrDefault();
            if (!layerGameObject)
            {
                Debug.LogError("Prefab not found: " + prefabId);
                return;
            }
            
            this.ReplaceReference(layerGameObject);
            this.layerProperties = layerProperties;

            //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ProjectData.Current.AddStandardLayer(this); 
            RegisterEventListeners();
        }

        ~ReferencedLayerData()
        {
            UnregisterEventListeners();
        }

        private void RegisterEventListeners()
        {
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.AddListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
        }

        private void UnregisterEventListeners()
        {
            ParentChanged.RemoveListener(OnParentChanged);
            ChildrenChanged.RemoveListener(OnChildrenChanged);
            ParentOrSiblingIndexChanged.RemoveListener(OnSiblingIndexOrParentChanged);
            LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
        }

        public virtual void ReplaceReference(LayerGameObject layerGameObject)
        {
            bool reselect = false;
            if (IsSelected)
            {
                DeselectLayer();
                reselect = true;
            }

            Reference = layerGameObject;

            OnReferenceChanged.Invoke();

            if (reselect)
            {
                SelectLayer();
            }
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