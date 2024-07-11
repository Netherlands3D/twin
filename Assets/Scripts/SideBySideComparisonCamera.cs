using System;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class SideBySideComparisonCamera : MonoBehaviour
    {
        public List<LayerNL3DBase> layerz = new();
        public Camera targetCameraz;

        public static List<LayerNL3DBase> layers = new();
        public static Camera targetCamera;

        private void Awake()
        {
            targetCamera = targetCameraz;
        }

        private void Update()
        {
            layers = layerz;
        }
    }
}