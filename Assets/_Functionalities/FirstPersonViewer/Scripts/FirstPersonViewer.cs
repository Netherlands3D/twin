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
        private InputAction sprintAction;

        //Movement
        private float movementMultiplier = 2f; //Should be replaced by a ScriptableObject

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

        private void OnEnable()
        {
            inputActionAsset.Enable();
        }

        private void OnDisable()
        {
            inputActionAsset.Disable();
        }

        private void Start()
        {
            yPositionTarget = transform.position.y;

            moveAction = inputActionAsset.FindAction("Move");
            sprintAction = inputActionAsset.FindAction("Sprint");

            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));
        }

        private void Update()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if(moveInput.magnitude > 0)
            {
                MovePlayer(moveInput);
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

        private void MovePlayer(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float movementSpeed = walkSpeed * (sprintAction.IsPressed() ? movementMultiplier : 1);

            transform.Translate(direction * movementSpeed * Time.deltaTime, Space.World);
            SnapToFloor();
        }

        private void SnapToFloor()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    yPositionTarget = point.y;
                }
            }, snappingCullingMask);
        }
    }
}