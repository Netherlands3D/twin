using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public interface IMaterialApplicatorAdapter
    {
        public Material CreateMaterial();

        public void SetMaterial(Material material);

        public Material GetMaterial();
    }
}