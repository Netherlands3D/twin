using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.PackageStagingArea.eu.netherlands3d.cameras.Runtime.Scripts.Cameras
{
    public class OrthographicSwitcher : MonoBehaviour
    {
        private Camera cameraComponent;
        private FreeCamera freeCamera;
        private Transform cameraTransform;
        private float previousPitchWhenSwitchingToAndFromOrtho = 60f;
        private float previousFovWhenSwitchingToAndFromOrtho = 60f;
        private Sequence animationSequence;

        [Header("Animation tweaks")]
        [SerializeField] private float repositioningDuration = 0.3f;
        [Range(0f, 60f)] [SerializeField] private float simulatedOrthoFov = 2f;
        [Tooltip("The animation dynamically heightens the Simulated Orthogonal FOV if the predicted height of the camera exceeds this number")]
        [SerializeField] private float maxCameraHeightWhenSimulating = 8000f;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraTransform = cameraComponent.transform;
            freeCamera = GetComponent<FreeCamera>();

            // Initialize orthographicSize for calculations
            cameraComponent.orthographicSize = transform.position.y;
            
            // Initialize cached value with a preset
            previousPitchWhenSwitchingToAndFromOrtho = cameraTransform.eulerAngles.x;
            previousFovWhenSwitchingToAndFromOrtho = cameraComponent.fieldOfView;
        }

        private void Update()
        {
            if (cameraComponent.orthographic)
            {
                // By dividing it by the field of view divided by 100, the size matches close to what you would see
                // in perspective mode
                cameraComponent.orthographicSize = transform.position.y * (previousFovWhenSwitchingToAndFromOrtho / 100f); 
            }
        }

        /// <summary>
        /// Switch camera to ortographic mode and limit its controls
        /// </summary>
        /// <param name="enableOrthographic">Ortographic mode enabled</param>
        public void EnableOrthographic(bool enableOrthographic)
        {
            // Switch back to perspective
            if (!enableOrthographic)
            {
                SwitchToPerspectiveMode();
                return;
            }

            SwitchToOrthographicMode();
        }

        private void SwitchToOrthographicMode()
        {
            if (cameraComponent.orthographic) return;

            // Remember the current pitch so that we have the correct angle to switch back to
            // when re-entering perspective
            previousPitchWhenSwitchingToAndFromOrtho = cameraTransform.eulerAngles.x;

            // Pull forward camera to make sure that what is in center of the screen stays there
            var cameraLookWorldPosition = freeCamera.GetWorldPoint(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0)
            );
            cameraLookWorldPosition.y = transform.position.y;

            // Look downwards
            var flattenedForward = cameraTransform.forward;
            flattenedForward.y = 0;
            var rotateTo = Quaternion.LookRotation(Vector3.down, flattenedForward);

            StartAnimation(cameraLookWorldPosition, rotateTo, true);
        }

        private void SwitchToPerspectiveMode()
        {
            if (!cameraComponent.orthographic) return;

            var currentCameraRotation = cameraTransform.rotation;
            var rotateTo = Quaternion.Euler(
                previousPitchWhenSwitchingToAndFromOrtho, 
                currentCameraRotation.eulerAngles.y, 
                currentCameraRotation.eulerAngles.z
            );
            
            var moveTo = CalculateCameraPositionWhenSwitchingToPerspective(rotateTo, currentCameraRotation);

            StartAnimation(moveTo, rotateTo, false);
        }

        #region Animation

        private void StartAnimation(Vector3 newPosition, Quaternion rotateTo, bool shouldSwitchToOrthogonal)
        {
            // If the animation is playing, quickly complete it and then start a new one
            if (animationSequence != null && animationSequence.IsPlaying())
            {
                animationSequence.Complete(true);
            }

            // Only cache the previous FOV when entering orthogonal mode, otherwise the camera is not restored to
            // the same FOV when switching back
            if (shouldSwitchToOrthogonal)
            {
                previousFovWhenSwitchingToAndFromOrtho = cameraComponent.fieldOfView;
            }

            // Find the tile handlers that are currently active, to disable them at the start of the animation
            // and reactivate them after the animation to prevent glitching from LOD switches
            var activeTileHandlers = FindObjectsByType<CartesianTiles.TileHandler>(
                FindObjectsInactive.Exclude, 
                FindObjectsSortMode.None
            );

            animationSequence = CreateAnimationSequence(
                new Vector3(newPosition.x, cameraTransform.position.y, newPosition.z), 
                rotateTo, 
                shouldSwitchToOrthogonal, 
                activeTileHandlers
            );
            animationSequence.Play();
        }

        private Sequence CreateAnimationSequence(
            Vector3 moveTo,
            Quaternion rotateTo,
            bool shouldSwitchToOrthogonal,
            CartesianTiles.TileHandler[] activeTileHandlers
        ) {
            var sequence = DOTween.Sequence(cameraTransform);
            sequence.SetEase(Ease.Linear);
            sequence.AppendCallback(CreateAnimationInitializer(shouldSwitchToOrthogonal, activeTileHandlers));

            if (shouldSwitchToOrthogonal)
            {
                AppendRepositioningOfCamera(sequence, rotateTo, moveTo);
                JoinTransitionToAndFromOrthogonal(sequence, CalculateSmallestFovForAnimation(moveTo.y, maxCameraHeightWhenSimulating), moveTo.y);
            }
            else
            {
                // Going back, we do not animate the field of view as that feels more snappy
                sequence.AppendCallback(() => cameraComponent.fieldOfView = previousFovWhenSwitchingToAndFromOrtho);
                AppendRepositioningOfCamera(sequence, rotateTo, moveTo);
            }

            sequence.AppendCallback(CreateAnimationFinalizer(shouldSwitchToOrthogonal, moveTo.y, activeTileHandlers));
            
            // Ensure animation sequence is nulled after completing to clean up
            sequence.OnComplete(() => animationSequence = null);

            return sequence;
        }

        private TweenCallback CreateAnimationInitializer(
            bool shouldSwitchToOrthogonal, 
            CartesianTiles.TileHandler[] activeTileHandlers
        ) {
            // Yes, we indeed return a callback here
            return () =>
            {
                LockBeforePerspectiveSwitch(activeTileHandlers);
                cameraComponent.orthographic = !shouldSwitchToOrthogonal;
            };
        }

        private TweenCallback CreateAnimationFinalizer(
            bool shouldSwitchToOrthogonal, 
            float simulatedHeight, 
            CartesianTiles.TileHandler[] activeTileHandlers
        ) {
            // Yes, we indeed return a callback here
            return () =>
            {
                // Force height to be the simulated height exactly to prevent rounding errors in the animation
                cameraTransform.position = new Vector3(
                    cameraTransform.position.x, 
                    simulatedHeight, 
                    cameraTransform.position.z
                );
                
                // Perform the actual switch
                cameraComponent.orthographic = shouldSwitchToOrthogonal;
                UnlockAfterPerspectiveSwitch(activeTileHandlers);
            };
        }

        private void AppendRepositioningOfCamera(Sequence sequence, Quaternion rotateTo, Vector3 moveTo)
        {
            // Pull camera and rotate to point downwards as if we are smoothing towards ortho
            sequence.Append(cameraTransform.DOMoveX(moveTo.x, repositioningDuration));
            sequence.Join(cameraTransform.DOMoveZ(moveTo.z, repositioningDuration));
            sequence.Join(cameraTransform.DORotate(rotateTo.eulerAngles, repositioningDuration));
        }

        private void JoinTransitionToAndFromOrthogonal(Sequence sequence, float smallestFov, float toHeight)
        {
            sequence.Join(
                cameraComponent
                    .DOFieldOfView(smallestFov, repositioningDuration)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() =>
                        {
                            // Someone smarter than me might actually figure out how to write
                            // an easing function out of this to use DOMoveY with a custom easing,
                            // but I am just not that smart :)
                            var actualHeight = toHeight * ((previousFovWhenSwitchingToAndFromOrtho / cameraComponent.fieldOfView) * 1.1f);
                            cameraTransform.position = new Vector3(
                                cameraTransform.position.x, 
                                actualHeight,
                                cameraTransform.position.z
                            );
                        }
                    )
            );
        }

        #endregion

        #region Calculations

        private Vector3 CalculateCameraPositionWhenSwitchingToPerspective(
            Quaternion rotateTo, 
            Quaternion currentCameraRotation
        ) {
            // Temporarily rotate it to calculate the position of the camera using transform.forward 
            cameraTransform.rotation = rotateTo;

            // Pull back camera to make sure that what is in center of the screen stays there
            var currentPosition = cameraTransform.position;
            var distance = currentPosition.y / Mathf.Sin(previousPitchWhenSwitchingToAndFromOrtho * Mathf.Deg2Rad);
            var moveTo = new Vector3(currentPosition.x, 0, currentPosition.z) - cameraTransform.forward * distance;

            // Restore rotation to make for a smooth animation
            cameraTransform.rotation = currentCameraRotation;
            
            return moveTo;
        }

        /// <summary>
        /// For the FOV animation to work best, we need to get as near to FOV 0 as possible. But the height of the
        /// camera is restrained to maxHeight to prevent issues with fog, levels of detail and the camera clipping
        /// range. This method takes the `simulatedOrthoFov` setting and calculates whether we can use that, or
        /// the smallest possible FOV to approach this number.
        /// </summary>
        /// <returns></returns>
        private float CalculateSmallestFovForAnimation(float simulatedHeight, float maxHeight)
        {
            var idealEndFov = simulatedOrthoFov;
            var smallestFov = Mathf.Clamp(
                idealEndFov, 
                simulatedHeight * previousFovWhenSwitchingToAndFromOrtho / maxHeight, 
                previousFovWhenSwitchingToAndFromOrtho
            );
#if UNITY_EDITOR
            if (!Mathf.Approximately(idealEndFov, smallestFov))
            {
                Debug.LogWarning(
                    $"When switching to orthogonal: Ideal FOV of {idealEndFov} would result in the camera simulation to go too high, clamped at: {smallestFov}");
            }
#endif
            return smallestFov;
        }

        #endregion

        #region LockingAndUnlockingTheView

        private void LockBeforePerspectiveSwitch(IEnumerable<CartesianTiles.TileHandler> activeTileHandlers)
        {
            freeCamera.LockDragging(true);
            foreach (var handler in activeTileHandlers)
            {
                handler.enabled = false;
            }
        }

        private void UnlockAfterPerspectiveSwitch(IEnumerable<CartesianTiles.TileHandler> activeTileHandlers)
        {
            foreach (var handler in activeTileHandlers)
            {
                handler.enabled = true;
            }

            freeCamera.LockDragging(false);
        }

        #endregion
    }
}