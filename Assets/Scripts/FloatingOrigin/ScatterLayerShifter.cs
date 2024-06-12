using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ScatterLayerShifter : WorldTransformShifter
    {
        private ObjectScatterLayer scatterLayer;

        private void Awake()
        {
            scatterLayer = GetComponent<ObjectScatterLayer>();
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            scatterLayer.RemoveReScatterListeners(); // shifting will move all the polygon points, but this should not regenerate the texture in this case
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            scatterLayer.ReGeneratePointsWithoutResampling(); //recalculate polygon bounds, recalculate points, and resample the existing texture.
            scatterLayer.AddReScatterListeners(); //re-add the removed listeners
        }
    }
}
