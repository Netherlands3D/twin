using DG.Tweening;
using Netherlands3D.FirstPersonViewer.Events;
using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    public enum CameraConstrain { CONTROL_Y, CONTROL_BOTH, CONTROL_NONE }

    public class FirstPersonViewerCamera : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private FirstPersonViewerInput input;
        private Camera firstPersonViewerCamera;

        private float cameraHeightOffset = 1.75f;
        private float currentsensitivity = 10f;

        [Header("Viewer")]
        [SerializeField] private Transform viewerBase;
        private float xRotation;
        private float yRotation;
        public CameraConstrain cameraConstrain;

        private Quaternion startRotation;

        //TEMP
        private bool didSetup;
        [Obsolete("Will be handled differntly (Prob with some kind of transitionState")] public bool DidSetup => didSetup;


        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            currentsensitivity = 3f;
#endif

            ViewerEvents.ChangeCameraConstrain += SetCameraConstrain;
            ViewerEvents.ChangeViewHeight += SetCameraHeight;
            ViewerEvents.ChangeFOV += SetCameraFOV;
            ViewerEvents.OnResetToStart += ResetToStart;
            ViewerEvents.SetCameraNorth += SetCameraNorth;

            firstPersonViewerCamera = GetComponent<Camera>();

            SetupViewer();
        }

        private void OnDestroy()
        {
            ViewerEvents.ChangeCameraConstrain -= SetCameraConstrain;
            ViewerEvents.ChangeViewHeight -= SetCameraHeight;
            ViewerEvents.ChangeFOV -= SetCameraFOV;
            ViewerEvents.OnResetToStart -= ResetToStart;
            ViewerEvents.SetCameraNorth -= SetCameraNorth;
        }

        private void SetupViewer()
        {
            firstPersonViewerCamera.transform.position = Camera.main.transform.position;
            firstPersonViewerCamera.transform.rotation = Camera.main.transform.rotation;

            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);

            firstPersonViewerCamera.transform.DOLocalMove(Vector3.zero + Vector3.up * cameraHeightOffset, 2f).SetEase(Ease.InOutSine);
            firstPersonViewerCamera.transform.DORotateQuaternion(targetRot, 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                xRotation = transform.localEulerAngles.x;
                startRotation = transform.rotation;
                didSetup = true;
                ViewerEvents.OnCameraRotation.Invoke(firstPersonViewerCamera.transform.forward);
            });
        }

        private void Update()
        {
            if (input.LockCamera) return;

            if (didSetup)
            {
                Vector2 cameraMovement = input.LookInput.ReadValue<Vector2>();

                if (cameraMovement.magnitude > 0)
                {
                    PointerDelta(cameraMovement);
                }
            }
        }

        private void PointerDelta(Vector2 pointerDelta)
        {
            Vector2 mouseLook = pointerDelta * currentsensitivity * Time.deltaTime;

            xRotation = Mathf.Clamp(xRotation - mouseLook.y, -90, 90);
            yRotation = yRotation + mouseLook.x;

            switch (cameraConstrain)
            {
                case CameraConstrain.CONTROL_Y:
                    transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
                    viewerBase.Rotate(Vector3.up * mouseLook.x);
                    ViewerEvents.OnCameraRotation?.Invoke(viewerBase.forward);
                    break;
                case CameraConstrain.CONTROL_BOTH:
                    viewerBase.rotation = Quaternion.Euler(xRotation, yRotation, 0);
                    ViewerEvents.OnCameraRotation?.Invoke(viewerBase.forward);
                    break;
                case CameraConstrain.CONTROL_NONE:
                    transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
                    ViewerEvents.OnCameraRotation?.Invoke(transform.forward);
                    break;
            }
        }

        private void SetCameraConstrain(CameraConstrain state)
        {
            if (state == CameraConstrain.CONTROL_BOTH) yRotation = transform.eulerAngles.y;
            else yRotation = transform.localEulerAngles.y;

            cameraConstrain = state;
        }

        private void SetCameraHeight(float height)
        {
            if (!didSetup) return;

            cameraHeightOffset = height;
            transform.localPosition = Vector3.up * cameraHeightOffset;
        }

        private void SetCameraNorth()
        {
            //switch (cameraConstrain)
            //{
            //    case CameraConstrain.CONTROL_Y:
            //        transform.DORotate(Vector3.zero, .2f); break;
            //    case CameraConstrain.CONTROL_BOTH:
            //        transform.DORotate(Vector3.zero, .2f); break;
            //    case CameraConstrain.CONTROL_NONE:
            //         break;
            //}
            transform.DORotate(Vector3.zero, .4f).SetEase(Ease.InOutCubic); 
            viewerBase.DORotate(Vector3.zero, .4f).SetEase(Ease.InOutCubic);
            xRotation = 0; yRotation = 0;

            ViewerEvents.OnCameraRotation.Invoke(Vector3.zero);
        }

        private void SetCameraFOV(float FOV) => firstPersonViewerCamera.fieldOfView = FOV;

        public Vector3 GetEulerRotation()
        {
            switch (cameraConstrain)
            {
                case CameraConstrain.CONTROL_Y:
                    return transform.eulerAngles;
                case CameraConstrain.CONTROL_BOTH:
                    return transform.eulerAngles;
                case CameraConstrain.CONTROL_NONE:
                    return transform.parent.eulerAngles;
            }

            return default;
        }

        private void ResetToStart() => transform.rotation = startRotation;
    }
}