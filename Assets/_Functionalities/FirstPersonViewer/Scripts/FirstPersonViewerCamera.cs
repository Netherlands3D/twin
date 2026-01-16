using DG.Tweening;
using Netherlands3D.FirstPersonViewer.ViewModus;
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
        private float previousCameraHeight;
        private float currentSensitivity = .11f;

        [Header("Viewer")]
        [SerializeField] private Transform viewerBase;
        public CameraConstrain cameraConstrain;

        //Smoothing
        private bool useRotationDampening = true;
        private float smoothTime = 0.15f;
        private Quaternion rotationVelocity; // must be stored!
        private Quaternion viewerRotationVelocity;
        private Quaternion cameraRotationVelocity;
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
        private bool prevCameraOrthographic;
        private const float DAMPENING_MULTIPLIER = 3;

        private void Awake()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            currentSensitivity = .035f;
#endif

            firstPersonViewerCamera = GetComponent<Camera>();
            FPVCamera = firstPersonViewerCamera;
        }

        public void SetupViewer()
        {
            mainCam = Camera.main;
            prevCameraPosition = mainCam.transform.position;
            prevCameraRotation = mainCam.transform.rotation;
            prevCameraCullingMask = mainCam.cullingMask;
            prevCameraOrthographic = mainCam.orthographic;

            input.AddInputLockConstrain(this);
            viewer.OnViewerExited += ExitViewer;

            transform.position = mainCam.transform.position;
            transform.rotation = mainCam.transform.rotation;

            Vector3 forward = mainCam.transform.forward;
            forward.y = 0;
            forward.Normalize();

            SetupMainCam();
            Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up);

            transform.DOLocalMove(Vector3.zero + Vector3.up * CameraHeightOffset, 2f).SetEase(Ease.InOutSine);
            transform.DORotateQuaternion(targetRot, 2f).SetEase(Ease.InOutSine).OnComplete(CameraSetupComplete);
        }

        //From Setup Viewer
        private void CameraSetupComplete()
        {
            startRotation = transform.rotation;

            //Setup events when done with animation.
            fovSetting.OnValueChanged.AddListener(SetCameraFOV);
            viewHeightSetting.OnValueChanged.AddListener(SetCameraHeight);

            viewer.OnResetToStart += ResetToStart;
            viewer.OnSetCameraNorth += SetCameraNorth;

            viewer.MovementSwitcher.LoadMovementPreset(0);
            input.RemoveInputLockConstrain(this);
        }

        //Disable the Main Camera through rendering.
        private void SetupMainCam()
        {
            mainCam.transform.position = transform.position + Vector3.up * cameraHeightAboveGround;
            mainCam.transform.rotation = Quaternion.Euler(90, 0, 0);
            mainCam.cullingMask = 0;

            mainCam.orthographic = true;
            mainCam.targetDisplay = 1;
        }

        private void ExitViewer(bool modified)
        {
            //Make sure the tween is not running.
            transform.DOKill();

            mainCam.cullingMask = prevCameraCullingMask;
            mainCam.orthographic = prevCameraOrthographic;
            mainCam.targetDisplay = 0;

            fovSetting.OnValueChanged.RemoveListener(SetCameraFOV);
            viewHeightSetting.OnValueChanged.RemoveListener(SetCameraHeight);

            viewer.OnResetToStart -= ResetToStart;
            viewer.OnSetCameraNorth -= SetCameraNorth;
            viewer.OnViewerExited -= ExitViewer;


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

        private void Update()
        {
            if (input.LockInput || input.LockCamera) return;

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
            float localCurrentSensitivity = currentSensitivity * (useRotationDampening ? DAMPENING_MULTIPLIER : 1);

            Vector2 mouseLook = pointerDelta * localCurrentSensitivity;

            Vector2 currentRot = GetCameraRotation();
            if (currentRot.x > 180) currentRot.x -= 360;
            if (currentRot.y > 180) currentRot.y -= 360;

            float xRotation = Mathf.Clamp(currentRot.x - mouseLook.y, -90, 90);
            float yRotation = currentRot.y + mouseLook.x;

            Quaternion targetLocalRotation = transform.localRotation;
            Quaternion targetViewerRotation = viewerBase.rotation;

            switch (cameraConstrain)
            {
                case CameraConstrain.CONTROL_Y:
                    targetLocalRotation = Quaternion.Euler(xRotation, 0, 0);
                    targetViewerRotation = viewerBase.rotation * Quaternion.Euler(0, mouseLook.x, 0);
                    break;

                case CameraConstrain.CONTROL_BOTH:
                    targetViewerRotation = Quaternion.Euler(xRotation, yRotation, 0);
                    break;

                case CameraConstrain.CONTROL_NONE:
                    xRotation = Mathf.Clamp(xRotation, -45, 45);
                    yRotation = Mathf.Clamp(yRotation, -90, 90);
                    targetLocalRotation = Quaternion.Euler(xRotation, yRotation, 0);
                    break;
            }

            if (useRotationDampening)
            {
                transform.localRotation = SmoothDampQuaternion(transform.localRotation, targetLocalRotation, ref cameraRotationVelocity, smoothTime);
                viewerBase.rotation = SmoothDampQuaternion(viewerBase.rotation, targetViewerRotation, ref viewerRotationVelocity, smoothTime);
            }
            else
            {
                transform.localRotation = targetLocalRotation;
                viewerBase.rotation = targetViewerRotation;
            }
        }

        public void SetCameraRotationDampening(bool enable) => useRotationDampening = enable;

        public void SetCameraConstrain(CameraConstrain state) => cameraConstrain = state;

        private void SetCameraHeight(float height)
        {
            previousCameraHeight = CameraHeightOffset;
            CameraHeightOffset = height;

            transform.localPosition = Vector3.up * CameraHeightOffset;
        }

        private void SetCameraNorth()
        {
            input.AddInputLockConstrain(this);
            transform.DORotate(Vector3.zero, .4f).SetEase(Ease.InOutCubic);
            viewerBase.DORotate(Vector3.zero, .4f).SetEase(Ease.InOutCubic).OnComplete(() => input.RemoveInputLockConstrain(this));
        }

        private void SetCameraFOV(float FOV) => firstPersonViewerCamera.fieldOfView = FOV;

        private Vector3 GetCameraRotation()
        {
            switch (cameraConstrain)
            {
                default:
                    return transform.eulerAngles;
                case CameraConstrain.CONTROL_NONE:
                    return transform.localEulerAngles;
            }
        }

        public Vector3 GetStateRotation()
        {
            switch (cameraConstrain)
            {
                default:
                    return transform.eulerAngles;
                case CameraConstrain.CONTROL_NONE:
                    return transform.parent.eulerAngles;
            }
        }

        public Vector3 GetPreviousCameraHeight() => transform.position + Vector3.up * previousCameraHeight;
        public void SetSensitivity(float sensitivity) => currentSensitivity = sensitivity;
        public float GetSensitivity() => currentSensitivity;
        private void ResetToStart() => transform.rotation = startRotation;

        private Quaternion SmoothDampQuaternion(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
        {
            // account for double-cover
            float dot = Quaternion.Dot(rot, target);
            float multi = dot > 0f ? 1f : -1f;
            target = new Quaternion(target.x * multi, target.y * multi, target.z * multi, target.w * multi);

            // smooth damp each component
            Vector4 result = new Vector4(
                Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
                Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
                Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
                Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
            ).normalized;

            // recompute derivative as tangent
            float dtInv = 1f / Time.deltaTime;
            deriv.x = (result.x - rot.x) * dtInv;
            deriv.y = (result.y - rot.y) * dtInv;
            deriv.z = (result.z - rot.z) * dtInv;
            deriv.w = (result.w - rot.w) * dtInv;

            return new Quaternion(result.x, result.y, result.z, result.w);
        }
    }
}