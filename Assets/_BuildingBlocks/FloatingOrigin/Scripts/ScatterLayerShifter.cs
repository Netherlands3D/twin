using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    [RequireComponent(typeof(ObjectScatterLayerGameObject))]
    public class ScatterLayerShifter : WorldTransformShifter
    {
        private ObjectScatterLayerGameObject scatterLayerGameObject;

        private void Awake()
        {
            scatterLayerGameObject = GetComponent<ObjectScatterLayerGameObject>();
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            scatterLayerGameObject.RemoveReScatterListeners(); // shifting will move all the polygon points, but this should not regenerate the texture in this case
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            scatterLayerGameObject.ReGeneratePointsWithoutResampling(); //recalculate polygon bounds, recalculate points, and resample the existing texture.
            scatterLayerGameObject.AddReScatterListeners(); //re-add the removed listeners
        }
    }
}