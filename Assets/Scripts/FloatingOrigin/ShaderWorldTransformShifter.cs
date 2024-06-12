using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ShaderWorldTransformShifter : WorldTransformShifter
    {
        [SerializeField] [Tooltip("Global shader variable used in shaders/shadergraphs")] private string shaderKeyWord = "_WorldOriginOffset";
        [SerializeField]  private Vector2 shaderOffset = Vector2.zero;

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin){}

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            //Simply use the new RD origin as our offset
            var rdTo = CoordinateConverter.ConvertTo(toOrigin, CoordinateSystem.RD);
            shaderOffset = new Vector2(
                (float)rdTo.Points[0],
                (float)rdTo.Points[1]
            );

            UpdateShaders();
        }

        private void OnValidate() {
            UpdateShaders();
        }

        private void UpdateShaders()
        {
            Shader.SetGlobalVector(shaderKeyWord, shaderOffset);
        }
    }
}
