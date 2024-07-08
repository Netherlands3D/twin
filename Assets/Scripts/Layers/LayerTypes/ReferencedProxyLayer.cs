using System;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ReferencedProxyLayer : LayerNL3DBase, IDisposable
    {
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

        protected override void OnChildrenChanged()
        {
            base.OnChildrenChanged();
            Reference.OnProxyTransformChildrenChanged();
        }

        protected void OnParentChanged()
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
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        // Protected dispose method to handle cleanup
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ParentChanged.RemoveListener(OnParentChanged);
            }
        }

        // Finalizer to ensure cleanup
        ~ReferencedProxyLayer()
        {
            Dispose(false);
        }
    }
}