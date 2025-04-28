using System;
using Netherlands3D.Twin.Tilekit.Events;
using UnityEngine;

namespace Netherlands3D.Twin.Tilekit
{
    [RequireComponent(typeof(TileMapper2))]
    public class FrustrumChecker : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        private Plane[] frustumPlanes = new Plane[6];
        private Plane[] previousFrustumPlanes = new Plane[6];
        private EventChannel EventChannel { get; set; }

        private void Awake()
        {
            // TODO: I do not like this reliance - refactor this
            EventChannel = GetComponent<TileMapper2>().EventChannel;
        }

        private void Start()
        {
            mainCamera ??= Camera.main;
            GeometryUtility.CalculateFrustumPlanes(mainCamera, frustumPlanes);
            EventChannel.Tick += OnTick;
        }

        private void OnDestroy()
        {
            EventChannel.Tick -= OnTick;
        }

        private void OnTick(EventSource eventSource)
        {
            if (!IsFrustumChanged()) return;

            EventChannel.FrustumChanged(eventSource, frustumPlanes);
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