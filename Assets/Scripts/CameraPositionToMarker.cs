using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Netherlands3D.Twin.FloatingOrigin;

namespace Netherlands3D.Twin
{
#if UNITY_EDITOR
    public class CameraPositionToMarker : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                CreateMarker(Camera.main.transform);
            }
        }

        private void CreateMarker(Transform transform)
        {
            var marker = SavePersistentGameObject.CreatePlayModeObject("Virtual Camera Marker");
            marker.AddComponent<WorldTransform>();
            marker.transform.position = transform.position;
            marker.transform.rotation = transform.rotation;
            marker.transform.localScale = transform.localScale;
        }
    }
#endif
}