using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class RotateTowardsCamera : MonoBehaviour
    {
        private Camera camera;
        private void Start()
        {
            camera = Camera.main;
        }

        void Update()
        {
            transform.LookAt(camera.transform.position);
        }
    }
}
