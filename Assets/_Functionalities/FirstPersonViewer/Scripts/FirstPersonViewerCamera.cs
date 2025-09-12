using DG.Tweening;
using Netherlands3D.Events;
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
        [SerializeField] private InputActionAsset inputActionAsset;

        private InputAction lookInput;
        private InputAction exitInput; //Should be moved to other script. Currently not possible due to how Camera.Main is handled.

        private Camera firstPersonViewerCamera;

        [SerializeField] private FloatEvent cameraHeight;
        [SerializeField] private FloatEvent sensitivty;

        private float cameraHeightOffset = 1.75f;
        private float currentsensitivity = 10f;

        [SerializeField] private Transform viewerBase;
        private float xRotation;
        private float yRotation;

        private bool didSetup;

        private float exitTimer;
        [SerializeField] private float exitDuration = .75f;

        //TEMP
        private Camera mainCam;

        public CameraConstrain cameraState;

        private void Start()
        {
            //ViewerEvents.UpdateCameraState += UpdateCameraState;

            cameraHeight.AddListenerStarted(SetCameraHeight);

            lookInput = inputActionAsset.FindAction("Look");
            exitInput = inputActionAsset.FindAction("Exit");

            firstPersonViewerCamera = GetComponent<Camera>();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SetupViewer();
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
            Camera.SetupCurrent(firstPersonViewerCamera);
        }

        private void Update()
        {
            if (didSetup)
            {
                Vector2 cameraMovement = lookInput.ReadValue<Vector2>();

                if (cameraMovement.magnitude > 0)
                {
                    PointerDelta(cameraMovement);
                }
            }

            if (exitInput.IsPressed()) //Should be moved to other script. Currently not possible due to how Camera.Main is handled.
            {
                exitTimer = Mathf.Max(exitTimer - Time.deltaTime, 0);

                if (exitTimer == 0) ExitViewer();
            }
            else exitTimer = exitDuration;
        }

        public void UpdateCameraConstrain(CameraConstrain state) => cameraState = state;
        

        private void PointerDelta(Vector2 pointerDelta)
        {
            Vector2 mouseLook = pointerDelta * 10 * Time.deltaTime;

            xRotation = Mathf.Clamp(xRotation - mouseLook.y, -90, 90);
            yRotation = yRotation + mouseLook.x;

            switch (cameraState)
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

        [Obsolete("Should be moved to other script. Currently not possible due to how Camera.Main is handled.")]
        private void ExitViewer()
        {
            mainCam.targetDisplay = 0;
            mainCam.GetComponent<FreeCamera>().enabled = true; //Somehow we need to stop using this to support modularity. Without breaking the camera.

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(transform.parent.gameObject);
        }

        private void SetCameraHeight(float height) => cameraHeightOffset = height;
    }
}
