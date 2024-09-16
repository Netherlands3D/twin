using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GG.Extensions;
using Netherlands3D.Events;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Netherlands3D.Twin.PackageStagingArea.eu.netherlands3d.cameras.Runtime.Scripts.Cameras
{
    public class Compass : MonoBehaviour
    {
        public RectTransform arrowTransform;
        public Color NorthColor;
        
        private Camera cameraComponent;
        private Transform cameraTransform;
        private bool cancelingAnimation = false;
        private Image arrowImage;
        private Color arrowColor;

        private const float directionMargin = 0.1f;
        private const float northAngleMargin = 1.0f;

        [Header("Animation tweaks")]
        [SerializeField] private float snapSpeed = 10f;

        [Header("Input")][SerializeField] private InputActionAsset inputActionAsset;
        private InputAction rotateStartAction;
        private InputAction rotateAction;
        private InputActionMap cameraActionMap;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
            cameraTransform = cameraComponent.transform;

            arrowImage = arrowTransform.GetComponent<Image>();
            arrowColor = arrowImage.color;

            //when user rotates, cancel the animation if active
            cameraActionMap = inputActionAsset.FindActionMap("Camera");
            rotateStartAction = cameraActionMap.FindAction("RotateModifier");
            rotateAction = cameraActionMap.FindAction("Look");
            rotateStartAction.started += CancelAnimation;
            rotateAction.started += UpdateArrow;
        }

        private void UpdateArrow(InputAction.CallbackContext context)
        {
            arrowTransform.SetRotationZ(cameraTransform.transform.eulerAngles.y);
            float angle;
            if (cameraComponent.orthographic)
                angle = Vector3.SignedAngle(cameraTransform.up, Vector3.forward, Vector3.up);
            else
                angle = Mathf.Rad2Deg * Mathf.Atan2(cameraTransform.forward.z, cameraTransform.forward.x) - 90;

            if (Mathf.Abs(angle) < northAngleMargin)
                arrowImage.color = NorthColor;
            else
                arrowImage.color = arrowColor;
        }

        public void SwitchToNorth()
        {
            cancelingAnimation = false;

            // Find the tile handlers that are currently active, to disable them at the start of the animation
            // and reactivate them after the animation to prevent glitching from LOD switches
            var activeTileHandlers = FindObjectsByType<CartesianTiles.TileHandler>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            //set target rotation
            Quaternion rotateTo = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            rotateTo.eulerAngles = new Vector3(cameraTransform.eulerAngles.x, rotateTo.eulerAngles.y, cameraTransform.eulerAngles.z);

            StartCoroutine(CameraAnimationLoop(rotateTo, cameraTransform,
                () => { LockTileHandlers(activeTileHandlers); },
                () => 
                { 
                    cameraTransform.transform.rotation = Quaternion.Slerp(cameraTransform.transform.rotation, rotateTo, Time.deltaTime * snapSpeed);
                    UpdateArrow(new InputAction.CallbackContext());
                },
                () => { UnlockTileHandlers(activeTileHandlers); }));
        }

        private IEnumerator CameraAnimationLoop(Quaternion to, Transform cam, Action onStart, Action onUpdate, Action onEnd)
        {
            WaitForUpdate wfu = new WaitForUpdate();
            onStart.Invoke();
            while(Quaternion.Angle(cam.rotation, to) > directionMargin && !cancelingAnimation)
            {
                onUpdate.Invoke();
                yield return wfu;
            }
            onEnd.Invoke();
            cancelingAnimation = false;
        }

        public void CancelAnimation(InputAction.CallbackContext context)
        {
            cancelingAnimation = true;
        }

        private void LockTileHandlers(IEnumerable<CartesianTiles.TileHandler> activeTileHandlers)
        {
            foreach (var handler in activeTileHandlers)
            {
                handler.enabled = false;
            }
        }

        private void UnlockTileHandlers(IEnumerable<CartesianTiles.TileHandler> activeTileHandlers)
        {
            foreach (var handler in activeTileHandlers)
            {
                handler.enabled = true;
            }
        }

        private void OnDestroy()
        {
            rotateStartAction.started -= CancelAnimation;
            rotateAction.started -= UpdateArrow;
        }
    }
}