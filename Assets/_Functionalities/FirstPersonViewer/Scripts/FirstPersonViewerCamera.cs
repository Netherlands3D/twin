using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.Twin.Cameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D
{
    public class FirstPersonViewerCamera : MonoBehaviour
    {
        private Camera firstPersonViewerCamera;

        [SerializeField] private FloatEvent cameraHeight;
        [SerializeField] private FloatEvent sensitivty;

        private float cameraHeightOffset = 2f;
        private float currentsensitivity = 10f;

        [SerializeField] private Transform viewerBase;
        private float xRotation;

        [SerializeField] private Vector3Event lookInput;

        private Camera mainCam;

        private void Start()
        {
            cameraHeight.AddListenerStarted(SetCameraHeight);

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
            firstPersonViewerCamera.transform.DORotateQuaternion(targetRot, 2f).SetEase(Ease.InOutSine).OnComplete(() => SetupEvents());

            mainCam.GetComponent<FreeCamera>().enabled = false; //TEMP FIX FOR CAMERA MOVEMENT WHILE IN FPV. $$
            mainCam.targetDisplay = 1;
            //mainCam.enabled = false; //Creates a lot of errors
            Camera.SetupCurrent(firstPersonViewerCamera);
        }

        private void SetupEvents()
        {
            xRotation = transform.localEulerAngles.x;
            lookInput.AddListenerStarted(PointerDelta);
        }

        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) ExitViewer();
        }

        public void PointerDelta(Vector3 pointerDelta)
        {
            Vector2 mouseLook = pointerDelta * 10 * Time.deltaTime;

            xRotation = Mathf.Clamp(xRotation - mouseLook.y, -90, 90);

            transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
            viewerBase.Rotate(Vector3.up * mouseLook.x);
        }

        private void ExitViewer()
        {
            mainCam.targetDisplay = 0;
            mainCam.GetComponent<FreeCamera>().enabled = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Destroy(transform.parent.gameObject);
        }

        private void SetCameraHeight(float height) => cameraHeightOffset = height;
    }
}
