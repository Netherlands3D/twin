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
        [SerializeField] private float remainderOf = 10000;

        private Vector3 cameraPosition = Vector3.zero;

        [SerializeField] private Vector2 shaderOffset;

        private void OnEnable()
        {
            Shader.SetGlobalVector(shaderKeyWord, Vector2.one);
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            cameraPosition = Camera.main.transform.position;
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            UpdateShaders();
        }

        private void UpdateShaders()
        {
            shaderOffset = new Vector2(
                cameraPosition.x,
                cameraPosition.z
            );
        }
    }
}
