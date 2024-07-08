using System;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [Serializable]
    public class ReferencedProxyLayer : LayerNL3DBase
    {
        [JsonIgnore]
        public ReferencedLayer Reference { get; }

        public override void DestroyLayer()
        {
            base.DestroyLayer();
            if (Reference)
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

        protected override void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            base.OnSiblingIndexOrParentChanged(newSiblingIndex);
            Reference.OnSiblingIndexOrParentChanged(newSiblingIndex);
        }

        public ReferencedProxyLayer(string name, ReferencedLayer reference) : base(name)
        {
            Reference = reference;  
            ProjectData.Current.AddStandardLayer(this); //AddDefaultLayer should be after setting the reference so the reference is assigned when the NewLayer event is called
            ParentChanged.AddListener(OnParentChanged);
            ChildrenChanged.AddListener(OnChildrenChanged);
        }

        ~ReferencedProxyLayer()
        {
            ParentChanged.RemoveListener(OnParentChanged);
            ChildrenChanged.RemoveListener(OnChildrenChanged);
        }
    }
}