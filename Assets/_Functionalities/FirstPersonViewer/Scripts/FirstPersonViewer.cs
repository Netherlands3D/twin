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
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;

        private InputAction moveAction;

        //Raycasting
        private OpticalRaycaster raycaster;
        private int snappingCullingMask = 0;

        //Falling
        private float fallSpeed;
        private readonly float gravity = 9.81f;
        private readonly float maxFallSpeed = 55f;
        private float yPositionTarget;

        [Header("TEMP")]
        [SerializeField] private float walkSpeed;

        [Header("Remove")]
        [SerializeField] private FloatEvent verticalInput;
        [SerializeField] private FloatEvent horizontalInput;


        private void Start()
        {
            yPositionTarget = transform.position.y;

            moveAction = inputActionAsset.FindAction("Move");

            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings"));
        }

        private void Update()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if(moveInput.magnitude > 0)
            {
                MoveCamera(moveInput);
            }


            if (transform.position.y > yPositionTarget)
            {
                fallSpeed = Mathf.Min(fallSpeed + gravity * Time.deltaTime, maxFallSpeed);
                
                transform.position += Vector3.down * fallSpeed * Time.deltaTime; 
            }
            else if (transform.position.y < yPositionTarget)
            {
                transform.position = new Vector3(transform.position.x, yPositionTarget, transform.position.z);
                fallSpeed = 0f;
            }
        }

        private void MoveCamera(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            transform.Translate(direction * walkSpeed * Time.deltaTime, Space.World);
            SnapToFloor();
        }

        private void SnapToFloor()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * 5, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    yPositionTarget = point.y;
                }
            }, snappingCullingMask);
        }
    }
}