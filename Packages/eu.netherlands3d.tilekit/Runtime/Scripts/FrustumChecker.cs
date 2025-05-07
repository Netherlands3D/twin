using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    public class FrustumChecker
    {
        private Camera mainCamera;
        private Plane[] frustumPlanes = new Plane[6];
        private Plane[] previousFrustumPlanes = new Plane[6];

        public FrustumChecker(Camera camera)
        {
            mainCamera = camera ?? Camera.main;
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);
        }
        
        public bool IsFrustumChanged()
        {
            // Every time we repopulate the frustumPlanes array - saving allocations
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);

            for (int i = 0; i < frustumPlanes.Length; i++)
            {
                if (frustumPlanes[i].Equals(previousFrustumPlanes[i])) continue;
                
                // Here we use the CalculateFrustumPlanes version but we want new allocation so that the check above
                // actually triggers - otherwise it will always return true
                previousFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

                return true;
            }

            return false;
        }
    }
}