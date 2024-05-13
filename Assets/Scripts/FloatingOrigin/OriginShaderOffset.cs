using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OriginShaderOffset : MonoBehaviour
    {
        [SerializeField] [Tooltip("Global shader variable used in shaders/shadergraphs")] private string shaderKeyWord = "_WorldOriginOffset";
        [SerializeField] private Origin origin;
        [SerializeField] private float remainderOf = 10000;

        private void OnEnable()
        {
            if (origin == null)
                origin = FindObjectOfType<Origin>();

            origin.onPostShift.AddListener(OnNewOrigin);

            Shader.SetGlobalVector(shaderKeyWord, Vector2.zero);
        }

        private void OnDisable()
        {
            origin.onPostShift.RemoveListener(OnNewOrigin);
        }

        private void OnNewOrigin(Coordinate from, Coordinate to)
        {
            Debug.Log("Shader New Origin: " + to.ToString());
            var beforeUnityCoordinate = CoordinateConverter.ConvertTo(from, CoordinateSystem.Unity);
            var afterUnityCoordinate = CoordinateConverter.ConvertTo(to, CoordinateSystem.Unity);
            Vector3 unityBefore = new Vector3((float)beforeUnityCoordinate.Points[0], (float)beforeUnityCoordinate.Points[1], (float)beforeUnityCoordinate.Points[2]);
            Vector3 unityAfter = new Vector3((float)afterUnityCoordinate.Points[0], (float)afterUnityCoordinate.Points[1], (float)afterUnityCoordinate.Points[2]);

            //Limit so our shaders also keeps precision by limiting the offset
            Vector3 offset = unityAfter - unityBefore;
            offset.x %= remainderOf;
            offset.y %= remainderOf;
            offset.z %= remainderOf;

            offset.x = -offset.x;
            offset.y = -offset.y;
            offset.z = -offset.z;

            Debug.Log("Shader new offset: " + offset.ToString());

            Shader.SetGlobalVector(shaderKeyWord, offset);
        }

        private void OnValidate() {
            //Make sure the shader keyword is set (global shader variables do not use their default in ShaderGraph)
            Shader.SetGlobalVector(shaderKeyWord, Vector2.zero);
        }
    }
}
