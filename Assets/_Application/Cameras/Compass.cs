using System.Collections.Generic;
using DG.Tweening;
using Netherlands3D.CartesianTiles;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Cameras
{
    public class Compass : MonoBehaviour
    {
        [Header("Input")][SerializeField] private InputActionAsset inputActionAsset;
        private InputAction rotateStartAction;
        private InputAction rotateAction;
        private InputActionMap cameraActionMap;
        private Sequence animationSequence;
        private Camera cameraComponent;
        private FreeCamera freeCamera;
        private Transform cameraTransform;
        private const float animationDuration = 1.0f;        

        public UnityEvent<float> ChangeDirection = new();
        
        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            freeCamera = cameraComponent.GetComponent<FreeCamera>();
            cameraTransform = cameraComponent.transform;            

            //when user rotates, cancel the animation if active
            cameraActionMap = inputActionAsset.FindActionMap("Camera");
            rotateStartAction = cameraActionMap.FindAction("RotateModifier");
            rotateAction = cameraActionMap.FindAction("Look");
            rotateStartAction.started += CancelAnimation;
            rotateAction.started += OnUpdateArrow;
        }

        private void OnUpdateArrow(InputAction.CallbackContext obj)
        {
            ChangeDirection.Invoke(cameraTransform.eulerAngles.y);
        }

        public void SwitchToNorth()
        {
            //set target rotation
            Quaternion rotateTo = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            rotateTo.eulerAngles = new Vector3(cameraTransform.eulerAngles.x, rotateTo.eulerAngles.y, cameraTransform.eulerAngles.z);
            StartAnimation(rotateTo);
        }

        private void StartAnimation(Quaternion rotateTo)
        {
            // If the animation is playing, quickly complete it and then start a new one
            if (animationSequence != null && animationSequence.IsPlaying())
            {
                animationSequence.Complete(true);
            }

            // Find the tile handlers that are currently active, to disable them at the start of the animation
            // and reactivate them after the animation to prevent glitching from LOD switches
            var activeTileHandlers = FindObjectsByType<TileHandler>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            animationSequence = CreateAnimationSequence(                
                rotateTo,
                activeTileHandlers
            );
            animationSequence.Play();
        }

        private Sequence CreateAnimationSequence(Quaternion rotateTo, TileHandler[] activeTileHandlers)
        {
            Sequence sequence = DOTween.Sequence(cameraTransform);
            sequence.SetEase(Ease.InOutCubic);
            sequence.AppendCallback(() =>
            {
                freeCamera.LockDragging(true);
                SetTileHandlersEnabled(activeTileHandlers, false);
            });
            sequence.Join(cameraTransform.DORotate(rotateTo.eulerAngles, animationDuration));
            sequence.AppendCallback(() =>
            {
                SetTileHandlersEnabled(activeTileHandlers, true);
                freeCamera.LockDragging(false);
            });

            // Ensure animation sequence is nulled after completing to clean up
            sequence.OnComplete(() => animationSequence = null);

            return sequence;
        }   

        public void CancelAnimation(InputAction.CallbackContext context)
        {
            //in case of user cancelation we want to keep the orientation before canceling
            //otherwise the camera will snap facing north giving an instant transition
            Vector3 angles = cameraTransform.eulerAngles;
            animationSequence.Complete(true);
            cameraTransform.eulerAngles = angles;
        }

        private void SetTileHandlersEnabled(IEnumerable<TileHandler> activeTileHandlers, bool enabled)
        {            
            foreach (var handler in activeTileHandlers)
            {
                handler.enabled = enabled;
            }
        }        

        private void OnDestroy()
        {
            rotateStartAction.started -= CancelAnimation;
            rotateAction.started -= OnUpdateArrow;
        }
    }
}