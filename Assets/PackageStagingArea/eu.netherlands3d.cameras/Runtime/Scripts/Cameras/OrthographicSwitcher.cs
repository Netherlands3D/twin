using System;
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
        [SerializeField] private float perspectiveShiftDuration = 1f;
        [Range(0f, 60f)] [SerializeField] private float simulatedOrthoFov = 2f;
        [Tooltip("The animation dynamically heightens the Simulated Orthogonal FOV if the predicted height of the camera exceeds this number")]
        [SerializeField] private float maxCameraHeightWhenSimulating = 8000f;

        [Header("Debug options - use with care")] 
        [SerializeField] private bool preventSwitchToOrthographicSetting = false;

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
            var newRotation = Quaternion.LookRotation(Vector3.down, flattenedForward);

            StartAnimation(cameraLookWorldPosition, newRotation, true);
        }

        private void SwitchToPerspectiveMode()
        {
            if (!cameraComponent.orthographic) return;

            var currentCameraRotation = cameraTransform.rotation;
            
            // Look back in the original direction. 
            var newRotation = Quaternion.Euler(
                previousPitchWhenSwitchingToAndFromOrtho, 
                cameraTransform.rotation.eulerAngles.y, 
                cameraTransform.rotation.eulerAngles.z
            );
            
            // Temporarily rotate it to calculate the position of the camera using transform.forward 
            cameraTransform.rotation = newRotation;

            // Pull back camera to make sure that what is in center of the screen stays there
            var currentPosition = cameraTransform.position;
            var distance = currentPosition.y / Mathf.Sin(previousPitchWhenSwitchingToAndFromOrtho * Mathf.Deg2Rad);
            var newPosition = new Vector3(currentPosition.x, 0, currentPosition.z) - cameraTransform.forward * distance;

            // Restore rotation to make for a smooth animation
            cameraTransform.rotation = currentCameraRotation;
            
            StartAnimation(newPosition, newRotation, false);
        }

        private void StartAnimation(Vector3 newPosition, Quaternion newRotation, bool intoOrtho)
        {
            // If the animation is playing, quickly complete it and then start a new one
            if (animationSequence != null && animationSequence.IsPlaying())
            {
                animationSequence.Complete(true);
            }

            if (intoOrtho)
            {
                previousFovWhenSwitchingToAndFromOrtho = cameraComponent.fieldOfView;
            }

            var simulatedHeight = cameraTransform.position.y;
            var moveTo = new Vector3(newPosition.x, simulatedHeight, newPosition.z);

            var idealEndFov = intoOrtho ? simulatedOrthoFov : previousFovWhenSwitchingToAndFromOrtho;

            var endFov = Mathf.Clamp(
                idealEndFov, 
                simulatedHeight * previousFovWhenSwitchingToAndFromOrtho / maxCameraHeightWhenSimulating, 
                previousFovWhenSwitchingToAndFromOrtho
            );
            
#if UNITY_EDITOR
            if (!Mathf.Approximately(idealEndFov, endFov))
            {
                Debug.LogWarning(
                    $"When switching to orthogonal: Ideal FOV of {idealEndFov} would result in the camera simulation to go too high, clamped at: {endFov}");
            }
#else
            // Just to be sure: prevent this setting from being on in a production environment
            preventSwitchToOrthographicSetting = false;
#endif

            animationSequence = DOTween.Sequence(cameraTransform);
            animationSequence.SetEase(Ease.Linear);

            animationSequence.AppendCallback(() => {
                if (preventSwitchToOrthographicSetting == false)
                {
                    cameraComponent.orthographic = !intoOrtho;
                }
            });

            if (intoOrtho)
            {
                AnimateRepositioningOfCamera(newRotation, moveTo);
                AnimateTransitionToAndFromOrthogonal(endFov, simulatedHeight);
            }
            else
            {
                cameraComponent.fieldOfView = previousFovWhenSwitchingToAndFromOrtho;
                AnimateRepositioningOfCamera(newRotation, moveTo);
            }

            animationSequence.AppendCallback(() =>
            {
                cameraTransform.position = new Vector3(cameraTransform.position.x, simulatedHeight, cameraTransform.position.z);
                if (preventSwitchToOrthographicSetting == false)
                {
                    cameraComponent.orthographic = intoOrtho;
                }
            });

            animationSequence.Play();
        }

        private void AnimateRepositioningOfCamera(Quaternion newRotation, Vector3 moveTo)
        {
            // Pull camera and rotate to point downwards as if we are smoothing towards ortho
            animationSequence.Append(cameraTransform.DOMove(moveTo, repositioningDuration));
            animationSequence.Join(cameraTransform.DORotate(newRotation.eulerAngles, repositioningDuration));
        }

        private void AnimateTransitionToAndFromOrthogonal(float endFov, float simulatedHeight)
        {
            animationSequence.Append(
                cameraComponent
                    .DOFieldOfView(endFov, perspectiveShiftDuration)
                    .SetEase(Ease.Linear)
                    .OnUpdate(() =>
                        {
                            // Someone smarter than me might actually figure out how to write
                            // an easing function out of this to use DOMoveY with a custom easing,
                            // but I am just not that smart :)
                            var actualHeight = simulatedHeight * ((previousFovWhenSwitchingToAndFromOrtho / cameraComponent.fieldOfView) * 1.1f);
                            cameraTransform.position = new Vector3(
                                cameraTransform.position.x, 
                                actualHeight,
                                cameraTransform.position.z
                            );
                        }
                    )
            );
        }
    }
}