using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WorldPositionFollower : MonoBehaviour
    {
        [SerializeField] private float disappearDistance = 2000f;
        private Camera mainCamera;
        private RectTransform rectTransform;
        private Coordinate? stuckToWorldPosition = null;

        private void Awake()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
        }
        
        public void StickTo(Coordinate? atWorldPosition)
        {
            stuckToWorldPosition = atWorldPosition;
        }

        public void MoveTo(Vector3 atScreenPosition)
        {
            // Canvas renders UI elements with z values between -1000 and +1000
            // this range is affected by the canvas scale, but the atScreenPosition z is also scaled so no further correction is needed

            var scaledZ = atScreenPosition.z / disappearDistance * 1000;
            atScreenPosition.z = scaledZ;
            rectTransform.position = atScreenPosition;
        }
        
        private void LateUpdate()
        {
            if (stuckToWorldPosition == null) return;

            MoveTo(mainCamera.WorldToScreenPoint(stuckToWorldPosition.Value.ToUnity()));
        }
    }
}
