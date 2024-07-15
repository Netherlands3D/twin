using System;
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
        private Sequence animationSequence;

        [FormerlySerializedAs("durationOfRepositioning")]
        [SerializeField] private float animationDuration = 0.3f;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraTransform = cameraComponent.transform;
            freeCamera = GetComponent<FreeCamera>();

            // Initialize orthographicSize for calculations
            cameraComponent.orthographicSize = transform.position.y;
            
            // Initialize cached value with a preset
            previousPitchWhenSwitchingToAndFromOrtho = cameraTransform.eulerAngles.x;
        }

        private void Update()
        {
            if (cameraComponent.orthographic)
            {
                // By dividing it by the field of view divided by 100, the size matches close to what you would see
                // in perspective mode
                cameraComponent.orthographicSize = transform.position.y * (cameraComponent.fieldOfView / 100);
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
            
            animationSequence = DOTween.Sequence(cameraTransform);
            animationSequence.SetEase(intoOrtho ? Ease.InCubic : Ease.OutCubic);
            
            animationSequence.AppendCallback(() => {
                cameraComponent.orthographic = !intoOrtho;
            });

            // Pull camera and rotate to point downwards as if we are smoothing towards ortho
            animationSequence.Insert(0f, cameraTransform.DOMoveX(newPosition.x, animationDuration));
            animationSequence.Insert(0f, cameraTransform.DOMoveZ(newPosition.z, animationDuration));
            animationSequence.Insert(0f, cameraTransform.DORotate(newRotation.eulerAngles, animationDuration));
            
            animationSequence.AppendCallback(() => {
                cameraComponent.orthographic = intoOrtho;
            });

            animationSequence.Play();
        }
    }
}