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
            Reference = reference;
            prefabId = reference.PrefabIdentifier;

            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            
            RegisterEventListeners();
        }

        [JsonConstructor]
        public ReferencedLayerData(string name, string prefabId, List<LayerPropertyData> layerProperties) : base(name, layerProperties)
        {
            this.prefabId = prefabId;
            this.layerProperties = layerProperties;
            
            ReplaceReferenceWith(Object.Instantiate(ProjectData.Current.PrefabLibrary.fallbackPrefab));

            ProjectData.Current.PrefabLibrary
                .GetPrefabById(prefabId)
                .Then(Object.Instantiate)
                .Then(ReplaceReferenceWith);
        }

        /// <summary>
        /// Replaces the active reference to a layer game object with another.
        ///
        /// This method will replace the active reference -if any- with another one if you want to swap out the
        /// visualisation with another. This can be used in an asynchronous process to first load a loader and then
        /// the real object, or when you need to temporarily change the visualisation.
        ///
        /// This method will not replace the prefab identifier, meaning that you can use layer game objects that are
        /// not registered with the prefablibrary, but also that the resulting layer game object is not persisted to
        /// the saved project. To do that, you need to change the prefabId on this layer data.
        /// </summary>
        /// <param name="layerGameObject">The new -instantiated- game object to register to this layer data</param>
        /// <returns>The provided layer game object after it has been registered to this layer data</returns>
        protected LayerGameObject ReplaceReferenceWith(LayerGameObject layerGameObject)
        {
            if (Reference)
            {
                RemoveReferenceToLayerGameObject();
                // to make sure the registration of the new reference works - we remove and re-add the
                // layer and thus notify the system that -effectively- a new layer is in town
                ProjectData.Current.RemoveLayer(this); 
            }

            AddReferenceToLayerGameObject(layerGameObject);

            // AddStandardLayer should be after setting the reference, so the reference is assigned when the
            // NewLayer event is called
            ProjectData.Current.AddStandardLayer(this); 
                
            return layerGameObject;
        }

        private void AddReferenceToLayerGameObject(LayerGameObject layerGameObject)
        {
            Reference = layerGameObject;
            Reference.LayerData = this;
            Reference.gameObject.name = Name;
            RegisterEventListeners();
        }

        private void RemoveReferenceToLayerGameObject()
        {
            // The event listeners depend on the reference - so we cancel them
            RemoveEventListeners();
            Reference.DestroyLayerGameObject();
            Reference = null;
        }

        ~ReferencedLayerData()
        {
            RemoveEventListeners();
        }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (!KeepReferenceOnDestroy && Reference)
            {
                RemoveReferenceToLayerGameObject();
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