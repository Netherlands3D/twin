using Netherlands3D.Twin.Samplers;
using System;
using System.Collections;
using System.Data;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WorldPositionOnGround : MonoBehaviour
    {
        private OpticalRaycaster opticalRaycaster;
        private bool positionFound = false;
        private Vector3 worldXZ;
        
        void Start()
        {
            opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (positionFound)
                return;

            worldXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(worldXZ);

            // Destroy(this);

            opticalRaycaster.GetWorldPointAsync(screenPoint, Callback, 1 << LayerMask.NameToLayer("Terrain"));
        }
        
        private void Callback(Vector3 hitWorldPoint, bool hasHit)
        {
            if (hasHit)
            {
                positionFound = true;
                float y = hitWorldPoint.y;
                Vector3 finalPosition = new Vector3(worldXZ.x, y, worldXZ.z);
                transform.position = finalPosition;
            }
            else
            {
                Invoke(nameof(UpdatePosition), 1f); //lets retry in 1 sec or it will cause a stackoverflow
            }
        }
        
        public void SetDirty()
        {
            positionFound = false;
        }

        private void OnDestroy()
        {
            opticalRaycaster.CancelRequest(Callback);
        }
    }
}
