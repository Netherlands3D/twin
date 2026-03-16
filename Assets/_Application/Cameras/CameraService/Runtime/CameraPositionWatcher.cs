using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Cameras
{
    [RequireComponent(typeof(CameraService))]
    public class CameraPositionWatcher : MonoBehaviour
    {
        public UnityEvent<Vector3> OnPositionChanged;

        private CameraService cameraService;
        private Transform cameraTransform;
        private Vector3 lastPosition;

        private void Awake()
        {
            cameraService = GetComponent<CameraService>();
            cameraService.OnSwitchCamera.AddListener(SwitchCamera);
        }

        private void OnDestroy()
        {
            cameraService.OnSwitchCamera.RemoveListener(SwitchCamera);
        }

        private void Start()
        {
            // Ensure the behaviour is initialized and the cached camera transform and position is set.
            SwitchCamera(cameraService.ActiveCamera);
        }

        /// <summary>
        /// Only after the scene's updates and transformations have passed, check if the position of the currently
        /// active camera has changed.
        /// </summary>
        private void LateUpdate()
        {
            if (cameraTransform == null) return;

            if (cameraTransform.position == lastPosition) return;

            PositionChanged();
        }

        /// <summary>
        /// When the active camera switches, replace the cached transform and trigger an event flow.
        /// </summary>
        private void SwitchCamera(Camera cam)
        {
            cameraTransform = cam.transform;
            PositionChanged();
        }

        /// <summary>
        /// When the position changed - emit an event and update the cached position
        /// </summary>
        private void PositionChanged()
        {
            OnPositionChanged.Invoke(cameraTransform.position);
            lastPosition = cameraTransform.position;
        }
    }
}