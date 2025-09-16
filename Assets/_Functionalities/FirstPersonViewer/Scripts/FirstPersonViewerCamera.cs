using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Twin.Cameras;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        //TEMP
        private Camera mainCam;
        private bool didSetup;
        [Obsolete("Will be handled differntly (Prob with some kind of transitionState")] public bool DidSetup => didSetup;


        private void Start()
        {
            ViewerEvents.ChangeCameraConstrain += SetCameraConstrain;
            ViewerEvents.ChangeViewHeight += SetCameraHeight;
            ViewerEvents.ChangeFOV += SetCameraFOV;

            ViewerEvents.OnViewerExited += ExitViewer;

            firstPersonViewerCamera = GetComponent<Camera>();

            SetupViewer();
        }

        private void OnDestroy()
        {
            ViewerEvents.ChangeCameraConstrain -= SetCameraConstrain;
            ViewerEvents.ChangeViewHeight -= SetCameraHeight;
            ViewerEvents.ChangeFOV -= SetCameraFOV;

            ViewerEvents.OnViewerExited -= ExitViewer;
        }

        private void SetupViewer()
        {
            mainCam = Camera.main;

            firstPersonViewerCamera.transform.position = mainCam.transform.position;
            firstPersonViewerCamera.transform.rotation = mainCam.transform.rotation;

            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);

            firstPersonViewerCamera.transform.DOLocalMove(Vector3.zero + Vector3.up * cameraHeightOffset, 2f).SetEase(Ease.InOutSine);
            firstPersonViewerCamera.transform.DORotateQuaternion(targetRot, 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                xRotation = transform.localEulerAngles.x;
                didSetup = true;
            });

            //Somehow we need to stop using this to support modularity. Without breaking the camera.
            mainCam.GetComponent<FreeCamera>().enabled = false; //TEMP FIX FOR CAMERA MOVEMENT WHILE IN FPV. $$
            mainCam.targetDisplay = 1;
            //mainCam.enabled = false; //Creates a lot of errors
            //Camera.SetupCurrent(firstPersonViewerCamera);
        }

        private void Update()
        {
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
            Vector2 mouseLook = pointerDelta * 10 * Time.deltaTime;

            xRotation = Mathf.Clamp(xRotation - mouseLook.y, -90, 90);
            yRotation = yRotation + mouseLook.x;

            switch (cameraConstrain)
            {
                case CameraConstrain.CONTROL_Y:
                    transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
                    viewerBase.Rotate(Vector3.up * mouseLook.x);
                    break;
                case CameraConstrain.CONTROL_BOTH:
                    viewerBase.rotation = Quaternion.Euler(xRotation, yRotation, 0);
                    break;
                case CameraConstrain.CONTROL_NONE:
                    transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0);
                    break;
            }
        }

        [Obsolete("Should be moved to other script. Currently not possible due to how Camera.Main is handled. Current state: Input is no longer handled here it now uses an event.")]
        private void ExitViewer()
        {
            mainCam.targetDisplay = 0;
            mainCam.GetComponent<FreeCamera>().enabled = true; //Somehow we need to stop using this to support modularity. Without breaking the camera.
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
    }
}