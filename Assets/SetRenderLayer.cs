using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class SetRenderLayer : MonoBehaviour
    {
        [SerializeField] int layerIndex = 1;

        private void OnTransformChildrenChanged()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.renderingLayerMask |= (uint)(1 << layerIndex);
            }
        }
    }
}