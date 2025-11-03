using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "Prefab")]
    public class ReferencedLayerData : LayerData
    {
        [DataMember] private string prefabId;
        public string PrefabIdentifier => prefabId;

        //[JsonIgnore] private LayerGameObject reference;
        //[JsonIgnore]
        //public LayerGameObject Reference
        //{
        //    get => reference;
        //    private set
        //    {
        //        if (reference == value) return;

        //        if (reference)
        //        {
        //            reference.DestroyLayerGameObject();
        //        }

        //        reference = value;
        //        if (!reference) return;

        //        reference.gameObject.name = Name;
        //        prefabId = reference.PrefabIdentifier;
        //        reference.LayerData = this;
        //    }
        //}

        private LayerGameObject reference;
        public LayerGameObject Reference => reference;


        [JsonIgnore] public bool KeepReferenceOnDestroy { get; set; } = false;
        [JsonIgnore] public UnityEvent OnReferenceChanged = new();

        public ReferencedLayerData(string name, LayerGameObject reference) : base(name)
        {
            //Reference = reference;

            // AddDefaultLayer should be after setting the reference so the reference is assigned
            // when the NewLayer event is called
            ProjectData.Current.AddStandardLayer(this);
            RegisterEventListeners();
        }

        public ReferencedLayerData(string name, string prefabId, LayerGameObject reference) : this(name, reference)
        {
            this.prefabId = prefabId;
        }

        [JsonConstructor]
        public ReferencedLayerData(
            string name, 
            string prefabId, 
            List<LayerPropertyData> layerProperties
        ) : base(name, layerProperties) {
            this.prefabId = prefabId;
        }

        public ReferencedLayerData(
            string name, 
            string prefabId, 
            List<LayerPropertyData> layerProperties,
            Action<ReferencedLayerData> onSpawn = null
        ) : base(name, layerProperties) {
            this.prefabId = prefabId;

            // TODO: In the future this should be refactored out of this class - it is now needed because
            // deserialisation of the project data and reconstitution of the visualisation classes is not
            // separated but this would be an awesome future step
            AddToProject(onSpawn);
        }

        [OnDeserialized]
        private async void OnDeserialized(StreamingContext ctx)
        {
            // TODO: In the future this should be refactored out of this class - it is now needed because
            // deserialisation of the project data and reconstitution of the visualisation classes is not
            // separated but this would be an awesome future step
            await App.Layers.SpawnLayer(this);
            RegisterEventListeners();
        }

        private async void AddToProject(Action<ReferencedLayerData> onComplete = null)
        {
            await App.Layers.SpawnLayer(this);
            
            // AddDefaultLayer should be after setting the reference so the reference is assigned when
            // the NewLayer event is called
            ProjectData.Current.AddStandardLayer(this);
            RegisterEventListeners();

            if (onComplete != null)
            {
                onComplete(this);
            }
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

        //public virtual void SetReference(LayerGameObject layerGameObject, bool keepPrefabIdentifier = false)
        //{
        //    string previousPrefabId = null;
        //    if (keepPrefabIdentifier && !string.IsNullOrEmpty(prefabId))
        //    {
        //        previousPrefabId = prefabId;
        //    }
        //    if (!reference)
        //    {
        //        Reference = layerGameObject;
        //        if (keepPrefabIdentifier && !string.IsNullOrEmpty(previousPrefabId))
        //        {
        //            prefabId = previousPrefabId;
        //        }
        //        return;
        //    }

        //    bool reselect = false;
        //    if (IsSelected)
        //    {
        //        DeselectLayer();
        //        reselect = true;
        //    }

        //    Reference = layerGameObject;
        //    if (keepPrefabIdentifier && !string.IsNullOrEmpty(previousPrefabId))
        //    {
        //        prefabId = previousPrefabId;
        //    }

        //    OnReferenceChanged.Invoke();

        //    if (reselect)
        //    {
        //        SelectLayer();
        //    }
        //}

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