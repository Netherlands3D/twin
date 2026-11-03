using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
{
    public class HierarchicalObjectMapping : IMapping
    {
        public object MappingObject => feature;
        public string Id => null;
        public HierarchicalObjectLayerGameObject Visualisation { get { return feature; } }
        public BoundingBox BoundingBox => Visualisation.Bounds;
        public LayerData LayerData => Visualisation.LayerData;
        private HierarchicalObjectLayerGameObject feature;

        public void SetFeature(HierarchicalObjectLayerGameObject feature)
        {
            this.feature = feature;
        }

        public void SetVisiblity(bool visible)
        {
            Visualisation.SetVisualisationVisible(visible);
        }

        public void Select(string subId = null)
        {
            throw new NotImplementedException();
        }

        public void Deselect()
        {
            throw new NotImplementedException();
        }
        
#if UNITY_EDITOR
        public void DebugMapping()
        {
            BoundingBox.Debug(Color.blueViolet);
            //UnityEngine.Debug.DrawLine(feature.gameObject.transform.position, boundingBox.Center.ToUnity(), Color.blueViolet);
        }
#endif
    }
}
