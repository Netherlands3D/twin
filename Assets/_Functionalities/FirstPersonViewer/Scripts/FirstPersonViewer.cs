using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewer : MonoBehaviour
    {
        [SerializeField] private FloatEvent verticalInput;
        [SerializeField] private FloatEvent horizontalInput;

        private OpticalRaycaster raycaster;
        private int snappingCullingMask = 0;

        [Header("TEMP")]
        [SerializeField] private float walkSpeed;
        private float yPositionTarget;

        private bool isFalling;
        private Vector3 targetPosition;

        private void Start()
        {
            verticalInput.AddListenerStarted(MoveForwardBackwards);
            horizontalInput.AddListenerStarted(MoveHorizontally);

            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings"));
        }

        private void MoveForwardBackwards(float amount)
        {
            transform.Translate(transform.forward * amount * walkSpeed * Time.deltaTime, Space.World);

            SnapToFloor();
        }

        private void MoveHorizontally(float amount)
        {
            transform.Translate(transform.right * amount * walkSpeed * Time.deltaTime, Space.World);
        }     

        private void SnapToFloor()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * 5, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    targetPosition = transform.position;
                    targetPosition.y = point.y;

                    transform.position = targetPosition;
                }
            }, snappingCullingMask);
        }
    }
}