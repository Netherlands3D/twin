using System;
using Netherlands3D.Tilekit;
using Netherlands3D.Twin.Tilekit.Events;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    public class FrustrumChecker : MonoBehaviour
    {
        [SerializeField] private BaseTileSetProvider tileSetProvider;
        [SerializeField] private Camera mainCamera;
        private Plane[] frustumPlanes = new Plane[6];
        private Plane[] previousFrustumPlanes = new Plane[6];
        private TileSetEventStream EventStream { get; set; }

        private void Start()
        {
            EventStream = EventBus.Stream(tileSetProvider.TileSetId);

            mainCamera ??= Camera.main;
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);
            EventStream.UpdateTriggered.AddListener(OnTick);
        }

        private void OnDestroy()
        {
            EventStream.UpdateTriggered.RemoveListener(OnTick);
        }

        private void OnTick(TileSetEventStreamContext tileSetEventStreamContext)
        {
            if (!IsFrustumChanged()) return;

            EventStream.FrustumChanged.Invoke(tileSetEventStreamContext, frustumPlanes);
        }

        private bool IsFrustumChanged()
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