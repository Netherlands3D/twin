using Netherlands3D.Twin.Samplers;
using System;
using System.Collections;
using System.Data;
using UnityEngine;

namespace Netherlands3D
{
    public class WorldPositionOnGround : MonoBehaviour
    {

        private OpticalRaycaster opticalRaycaster;
        private bool positionFound = false;
        private bool destroyed = false;

        void Start()
        {
            opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (positionFound || destroyed)
                return;

            Vector3 worldXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldXZ);

            opticalRaycaster.GetWorldPointAsync(screenPoint, (hitWorldPoint, hasHit) =>
            {
                if(destroyed) return;

                if (hasHit)
                {
                    positionFound = true;
                    float y = hitWorldPoint.y;
                    Vector3 finalPosition = new Vector3(worldXZ.x, y, worldXZ.z);
                    transform.position = finalPosition;
                }
                else
                {
                    UpdatePosition();
                }
                
            }, 1 << LayerMask.NameToLayer("Terrain"));
        }

        public void SetDirty()
        {
            positionFound = false;
        }

        private void OnDestroy()
        {
            destroyed = true;
        }
    }
}
