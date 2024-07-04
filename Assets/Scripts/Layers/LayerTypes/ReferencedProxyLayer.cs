using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ReferencedProxyLayer : LayerNL3DBase
    {
        public ReferencedLayer Reference { get; set; }

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

        protected override void OnTransformChildrenChanged()
        {
            base.OnTransformChildrenChanged();
            Reference.OnProxyTransformChildrenChanged();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            Reference.OnProxyTransformParentChanged();
        }

        protected override void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            base.OnSiblingIndexOrParentChanged(newSiblingIndex);
            Reference.OnSiblingIndexOrParentChanged(newSiblingIndex);
        }
    }
}