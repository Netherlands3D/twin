using System;
using UnityEngine;

namespace Netherlands3D
{
    public class LightColor : MonoBehaviour
    {
        [SerializeField] private MeshRenderer meshRenderer;
        
        public void SetMaterial(Material material)
        {
            meshRenderer.material = material;
        }
    }
}
