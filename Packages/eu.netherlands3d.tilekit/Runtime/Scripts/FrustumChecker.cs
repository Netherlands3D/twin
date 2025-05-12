using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    public class FrustumChecker
    {
        public Plane[] Planes => frustumPlanes;

        private readonly Camera mainCamera;
        private readonly Plane[] frustumPlanes = new Plane[6];
        private Plane[] previousFrustumPlanes = new Plane[6];

        public FrustumChecker(Camera camera)
        {
            mainCamera = camera ?? Camera.main;
            GeometryUtility.CalculateFrustumPlanes(mainCamera, Planes);
        }

        public bool IsFrustumChanged()
        {
            // Every time we repopulate the frustumPlanes array - saving allocations
            GeometryUtility.CalculateFrustumPlanes(mainCamera, Planes);

            for (int i = 0; i < Planes.Length; i++)
            {
                if (Planes[i].Equals(previousFrustumPlanes[i])) continue;
                
                // Here we use the CalculateFrustumPlanes version but we want new allocation so that the check above
                // actually triggers - otherwise it will always return true
                previousFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

                return true;
            }

            return false;
        }
    }
}