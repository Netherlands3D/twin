using DG.Tweening;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    public enum CameraConstrain { CONTROL_Y, CONTROL_BOTH, CONTROL_NONE }

    public class FirstPersonViewerCamera : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private FirstPersonViewerInput input;
        [SerializeField] private FirstPersonViewer viewer;
        private Camera firstPersonViewerCamera;
        public static Camera FPVCamera;

        public float CameraHeightOffset { private set; get; } = 1.75f;
        private float currentsensitivity = 10f;

        [Header("Viewer")]
        [SerializeField] private Transform viewerBase;
        private float xRotation;
        private float yRotation;
        public CameraConstrain cameraConstrain;

        private Quaternion startRotation;

        [Header("Settings")]
        [SerializeField] private MovementFloatSetting fovSetting;
        [SerializeField] private MovementFloatSetting viewHeightSetting;

        [Header("Main Cam")]
        [SerializeField] private float cameraHeightAboveGround;
        [SerializeField] private float returnFocusDistance = 150;
        private Camera mainCam;
        private Vector3 prevCameraPosition;
        private Quaternion prevCameraRotation;
        private int prevCameraCullingMask;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            currentsensitivity = 3f;
#endif

            firstPersonViewerCamera = GetComponent<Camera>();
            FPVCamera = firstPersonViewerCamera;
        }

        private void Start()
        {
            input.AddInputLockConstrain(this);
            SetupViewer();
        }

        private void OnDestroy()
        {
            fovSetting.OnValueChanged.RemoveListener(SetCameraFOV);
            viewHeightSetting.OnValueChanged.RemoveListener(SetCameraHeight);

            viewer.OnResetToStart -= ResetToStart;
            viewer.OnSetCameraNorth -= SetCameraNorth;
            FirstPersonViewer.OnViewerExited -= ExitViewer;
        }

        private void SetupViewer()
        {
            firstPersonViewerCamera.transform.position = Camera.main.transform.position;
            firstPersonViewerCamera.transform.rotation = Camera.main.transform.rotation;

            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();

            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);

            firstPersonViewerCamera.transform.DOLocalMove(Vector3.zero + Vector3.up * CameraHeightOffset, 2f).SetEase(Ease.InOutSine);
            firstPersonViewerCamera.transform.DORotateQuaternion(targetRot, 2f).SetEase(Ease.InOutSine).OnComplete(CameraSetupComplete);
        }

        //From Setup Viewer
        private void CameraSetupComplete()
        {
            xRotation = transform.localEulerAngles.x;
            startRotation = transform.rotation;
            input.RemoveInputLockConstrain(this);

            //Setup events when done with animation.
            fovSetting.OnValueChanged.AddListener(SetCameraFOV);
            viewHeightSetting.OnValueChanged.AddListener(SetCameraHeight);

            viewer.OnResetToStart += ResetToStart;
            viewer.OnSetCameraNorth += SetCameraNorth;
            FirstPersonViewer.OnViewerExited += ExitViewer;

            SetupMainCam();
            ServiceLocator.GetService<MovementModusSwitcher>().LoadMovementPreset(0);
        }

        //Disable the Main Camera through rendering.
        private void SetupMainCam()
        {
            mainCam = Camera.main;

            prevCameraPosition = mainCam.transform.position;
            prevCameraRotation = mainCam.transform.rotation;
            prevCameraCullingMask = mainCam.cullingMask;

            mainCam.transform.position = transform.position + Vector3.up * cameraHeightAboveGround;
            mainCam.transform.rotation = Quaternion.Euler(90, 0, 0);
            mainCam.cullingMask = 0;

            mainCam.orthographic = true;
            mainCam.targetDisplay = 1;
        }

        private void Update()
        {
            if (input.LockInput) return;

            Vector2 cameraMovement = input.LookInput.ReadValue<Vector2>();

            if (cameraMovement.magnitude > 0)
            {
                RotateCamera(cameraMovement);
            }

            //Update Main Cam position
            Vector3 camPos = transform.position;
            camPos.y = cameraHeightAboveGround;
            mainCam.transform.position = camPos;
        }

        //Sets the rotation of the camera or the viewerBase based on the current Camera Constrain.
        private void RotateCamera(Vector2 pointerDelta)
        {
            Vector2 mouseLook = pointerDelta * currentsensitivity * Time.deltaTime;

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

        public void SetCameraConstrain(CameraConstrain state)
        {
            if (state == CameraConstrain.CONTROL_BOTH)
                yRotation = transform.eulerAngles.y;
            else
                yRotation = transform.localEulerAngles.y;

            cameraConstrain = state;
        }

        private void SetCameraHeight(float height)
        {
            CameraHeightOffset = height;
            transform.localPosition = Vector3.up * CameraHeightOffset;
        }

        private void SetCameraNorth()
        {
            input.AddInputLockConstrain(this);
            transform.DORotate(Vector3.zero, .4f).SetEase(Ease.InOutCubic);
            viewerBase.DORotate(Vector3.zero, .4f).SetEase(Ease.InOutCubic).OnComplete(() => input.RemoveInputLockConstrain(this));

            xRotation = 0;
            yRotation = 0;
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

        private void ExitViewer(bool modified)
        {
            mainCam.cullingMask = prevCameraCullingMask;
            mainCam.orthographic = false;
            mainCam.targetDisplay = 0;
            if (modified)
            {
                mainCam.transform.position = prevCameraPosition;
                mainCam.transform.rotation = prevCameraRotation;
            }
            else
            {
                mainCam.GetComponent<FreeCamera>().FocusOnPoint(transform.position, returnFocusDistance);
            }
        }
    }
}