using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    /// <summary>
    /// Specialized LayerGameObject for visualising placeholder that will be replaced by actual LayerGameObjects
    /// specific to the prefab id in the LayerData.
    /// </summary>
    public class PlaceholderLayerGameObject : LayerGameObject
    {
        public override BoundingBox Bounds => new(new Bounds());
        
        public PlaceholderLayerGameObject Instantiate(ReferencedLayerData layerData)
        {
            // Because a placeholder doesn't have a placeholder and this is a synchronous call - Awake does nothing and
            // we can set LayerData as the next call, which will trigger the OnLayerInitialize
            var layerGameObject = Instantiate(this);
            layerGameObject.LayerData = layerData;

            return layerGameObject;
        }
        
        /// <summary>
        /// Replace this placeholder with the given LayerGameObject.
        ///
        /// This method is expected to be called from the provided LayerGameObject's Awake method before LayerData is
        /// used, this way we can curry the LayerData from this placeholder onto an asynchronously instantiated
        /// LayerGameObject.
        ///
        /// This is needed because the Awake's of LayerGameObject rely on the existence of an externally injected
        /// LayerData - and since we need to instantiate the final Layer Game Objects asynchronously we cannot rely on
        /// static field propagation but instead curry it by having a parent placeholder and in the Awake of the final
        /// Layer Game Object replace the LayerData with the placeholder using this method. 
        /// </summary>
        public void ReplaceWith(LayerGameObject layerGameObject)
        {
            var layerData = LayerData;
                
            // Reparent the final layerGameObject to the parent of the placeholder so that the situation is as 
            // it should be before doing the switcheroo
            layerGameObject.transform.SetParent(transform.parent, false);
            
            // Does the whole switcheroo, including transplanting the LayerData and destroying the placeholder
            layerData.SetReference(layerGameObject);
        }
    }
}