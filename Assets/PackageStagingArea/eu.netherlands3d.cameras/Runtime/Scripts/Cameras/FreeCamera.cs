using System;
using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.Twin;
using Netherlands3D.Twin.PackageStagingArea.eu.netherlands3d.cameras.Runtime.Scripts.Cameras;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 *  Copyright (C) X Gemeente
 *                X Amsterdam
 *                X Economic Services Departments
 *
 *  Licensed under the EUPL, Version 1.2 or later (the "License");
 *  You may not use this work except in compliance with the License.
 *  You may obtain a copy of the License at:
 *
 *    https://github.com/Amsterdam/Netherlands3D/blob/main/LICENSE.txt
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" basis,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 *  implied. See the License for the specific language governing
 *  permissions and limitations under the License.
 */
public class FreeCamera : MonoBehaviour
{
    [Header("Options")] [Tooltip("Move forward on world plane instead of camera forward")] [SerializeField]
    private bool moveForwardOnPlane = true;

    [Tooltip("Value threshold from 0 to 1 to switch to dragging coplanar")] [SerializeField]
    private float dragOnPlaneThreshold = 0.5f;

    [SerializeField] private bool dragToMoveCamera = true;
    [SerializeField] private bool multiplySpeedBasedOnHeight = true;

    [Header("Speeds")] [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float upAndDownSpeed = 10.0f;
    [SerializeField] private float dragSpeed = 1.0f;
    [SerializeField] private float easing = 1.0f;
    [SerializeField] private float zoomSpeed = 1.0f;
    private float dynamicMoveSpeed = 1.0f;
    private float dynamicDragSpeed = 1.0f;
    private float dynamicZoomSpeed = 1.0f;

    [SerializeField] private float minimumSpeed = 5.0f;
    [SerializeField] private float maximumSpeed = 1000.0f;
    [SerializeField] private float minimumDragSpeed = 5.0f;
    [SerializeField] private float maximumDragSpeed = 1000.0f;
    [SerializeField] private float dragRotateSpeed = 1.0f;
    [SerializeField] private float rotateAroundPointSpeed = 1.0f;

    [Header("Gamepad")] [SerializeField] private float gamepadRotateSpeed = 1.0f;
    [SerializeField] private float gamepadMoveSpeed = 1.0f;

    [Header("Limits")] [SerializeField] private float maxPointerDistance = 10000;
    [SerializeField] private float maxCameraHeight = 1500;
    [SerializeField] private float minCameraHeight = -500;
    [SerializeField] private bool useRotationLimits = true;

    [Header("Listen to input events")] [SerializeField]
    private FloatEvent horizontalInput;

    [SerializeField] private FloatEvent verticalInput;
    [SerializeField] private FloatEvent upDownInput;
    [SerializeField] private Vector3Event lookInput;
    [SerializeField] private Vector3Event flyInput;
    [SerializeField] private Vector3Event rotateInput;
    [SerializeField] private FloatEvent zoomToPointerInput;
    [SerializeField] private Vector3Event pointerPosition;
    [SerializeField] private BoolEvent dragModifier;
    [SerializeField] private BoolEvent rotateModifier;
    [SerializeField] private BoolEvent firstPersonModifier;

    [Header("Other setting events")] [SerializeField]
    public BoolEvent blockCameraDrag;

    [SerializeField] public BoolEvent ortographicEnabled;
    [SerializeField] public GameObjectEvent focusOnObject;
    [SerializeField] private float focusAngle = 45.0f;
    [SerializeField] private float focusDistanceMultiplier = 2.0f;

    private Vector3 currentPointerPosition;
    private Vector3 zoomTarget;
    private Camera cameraComponent;
    private Plane worldPlane;

    private Vector3 dragStart;
    private Vector3 dragVelocity;
    private Vector3 currentPointerDelta;

    private bool dragging = false;
    private bool lockDraggingInput = false;
    private bool rotate = false;
    private bool rotatingAroundPoint = false;
    private bool firstPersonRotate = false;

    private Vector3 dragStartPointerPosition;
    private Quaternion previousRotation;
    private Vector3 previousPosition;
    public OrthographicSwitcher orthographicSwitcher;

    [SerializeField] private OpticalRaycaster opticalRaycaster;
    private Vector3 intendedMoveVector;
    private Quaternion intendedRotation;
    private Color opticalRaycastColor;
    
    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        orthographicSwitcher = orthographicSwitcher ? orthographicSwitcher : GetComponent<OrthographicSwitcher>();

        worldPlane = new Plane(Vector3.up, Vector3.zero);

        horizontalInput.AddListenerStarted(MoveHorizontally);
        verticalInput.AddListenerStarted(MoveForwardBackwards);
        upDownInput.AddListenerStarted(MoveUpDown);
        lookInput.AddListenerStarted(PointerDelta);
        flyInput.AddListenerStarted(FreeFly);
        rotateInput.AddListenerStarted(RotateAroundOwnAxis);

        zoomToPointerInput.AddListenerStarted(ZoomToPointer);
        pointerPosition.AddListenerStarted(SetPointerPosition);

        dragModifier.AddListenerStarted(Drag);
        rotateModifier.AddListenerStarted(Rotate);
        firstPersonModifier.AddListenerStarted(RotateFirstPerson);

        if (blockCameraDrag) blockCameraDrag.AddListenerStarted(LockDragging);
        if (ortographicEnabled) ortographicEnabled.AddListenerStarted(EnableOrtographic);
        if (focusOnObject) focusOnObject.AddListenerStarted(FocusOnObject);
    }

    /// <summary>
    /// Switch camera to ortographic mode and limit its controls
    /// </summary>
    /// <param name="enableOrtographic">Ortographic mode enabled</param>
    public void EnableOrtographic(bool enableOrtographic)
    {
        if (!orthographicSwitcher)
        {
            cameraComponent.orthographic = enableOrtographic;
            return;
        }

        orthographicSwitcher.EnableOrthographic(enableOrtographic);
    }

    /// <summary>
    /// Focus camera on gameobject using origin.
    /// Move camera backwards to contain renderer bounds.
    /// </summary>
    /// <param name="focusObject"></param>
    public void FocusOnObject(GameObject focusObject)
    {
        this.transform.position = focusObject.transform.position;
        this.transform.eulerAngles = new Vector3((cameraComponent.orthographic) ? 90 : focusAngle, 0, 0);

        var meshRenderer = focusObject.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer)
        {
            this.transform.position = meshRenderer.bounds.center;
            PrepareCameraForMove(Vector3.back, meshRenderer.bounds.size.magnitude * focusDistanceMultiplier, Space.Self);
            // this.transform.Translate(Vector3.back * meshRenderer.bounds.size.magnitude * focusDistanceMultiplier, Space.Self);
        }
        else
        {
            PrepareCameraForMove(Vector3.back, focusDistanceMultiplier, Space.Self);
            // this.transform.Translate(Vector3.back * focusDistanceMultiplier, Space.Self);
        }
    }

    /// <summary>
    /// Set dragging input to locked/unlocked. This ignores pointer drag input while still allowing other movement inputs.
    /// If another feature used mouse pointer but want to stop the camera from dragging while click+dragging, set this to locked.
    /// </summary>
    /// <param name="locked">Lock pointer drag events</param>
    public void LockDragging(bool locked)
    {
        lockDraggingInput = locked;
    }

    /// <summary>
    /// Use pointerdelta to rotate around point or first person rotate when modifiers are enabled
    /// </summary>
    /// <param name="pointerDelta">Pointer delta input</param>
    public void PointerDelta(Vector3 pointerDelta)
    {
        currentPointerDelta = pointerDelta;

        if (rotate)
        {
            if (!rotatingAroundPoint)
            {
                dragStart = GetWorldPoint();
            }

            rotatingAroundPoint = true;
            RotateAroundPoint(pointerDelta);
        }
        else if (dragging && firstPersonRotate)
        {
            pointerDelta.x = -pointerDelta.x;
            DragRotateAroundOwnAxis(pointerDelta);
        }
    }

    /// <summary>
    /// Drag rotate camera via pointer delta
    /// </summary>
    /// <param name="value">Pointer/stick delta input</param>
    public void DragRotateAroundOwnAxis(Vector3 value)
    {
        StopEasing();
        CalculateSpeed();

        StorePreviousTransform();

        var eulerRotation = quaternion.Euler(0, value.x * dragRotateSpeed, 0);
        // intendedRotation *= WorldToLocalRotation(eulerRotation);
        this.transform.Rotate(0, value.x * dragRotateSpeed, 0, Space.World);
        if (!cameraComponent.orthographic)
        {
            // intendedRotation *= quaternion.Euler(value.y * dragRotateSpeed, 0, 0);
            this.transform.Rotate(value.y * dragRotateSpeed, 0, 0, Space.Self);
            RevertIfOverAxis();
        }
    }

    /// <summary>
    /// Rotate around own axes (first person style turning of camera)
    /// </summary>
    /// <param name="value">Pointer/stick delta input</param>
    public void RotateAroundOwnAxis(Vector3 value)
    {
        StopEasing();
        CalculateSpeed();

        StorePreviousTransform();

        var eulerRot = quaternion.Euler(0, value.x * gamepadRotateSpeed * Time.deltaTime, 0);
        // intendedRotation *= WorldToLocalRotation(eulerRot);
        this.transform.Rotate(0, value.x * gamepadRotateSpeed * Time.deltaTime, 0, Space.World);
        if (!cameraComponent.orthographic)
        {
            // intendedRotation *= quaternion.Euler(value.y * gamepadRotateSpeed * Time.deltaTime, 0, 0);
            this.transform.Rotate(value.y * gamepadRotateSpeed * Time.deltaTime, 0, 0, Space.Self);
            RevertIfOverAxis();
        }
    }

    private quaternion WorldToLocalRotation(quaternion worldSpaceRotation)
    {
        return Quaternion.Inverse(transform.rotation) * worldSpaceRotation * transform.rotation;
    }
    
    /// <summary>
    /// Stores previous transform position to reset to after moves that cross the bounds
    /// </summary>
    private void StorePreviousTransform()
    {
        previousRotation = this.transform.rotation;
        previousPosition = this.transform.position;
    }

    /// <summary>
    /// Rotate camera around a fixed point 
    /// </summary>
    /// <param name="pointerDelta">Pointer delta (based on deltaTime)</param>
    public void RotateAroundPoint(Vector3 pointerDelta)
    {
        StopEasing();

        StorePreviousTransform();

        transform.RotateAround(dragStart, Vector3.up, pointerDelta.x * rotateAroundPointSpeed);
        if (!cameraComponent.orthographic)
        {
            transform.RotateAround(dragStart, this.transform.right, -pointerDelta.y * rotateAroundPointSpeed);
            RevertIfOverAxis();
        }
    } 
    
    public void RotateAround(Vector3 point, Vector3 axis, float angle)
    {
        // transform.RotateAround(point, axis, angle);
        
        // Vector3 position = transform.position;
        // Vector3 vector3 = Quaternion.AngleAxis(angle, axis) * (position - point);
        // Vector3 newPosition = point + vector3;
        //
        // intendedMoveVector += newPosition - transform.position;
        //
        Vector3 worldPos = transform.position;
        Quaternion rot = Quaternion.AngleAxis(angle, axis);
        Vector3 dif = worldPos - point;
        dif = rot * dif;
        worldPos = point + dif;
        var newPosition = worldPos;
        intendedMoveVector += newPosition - transform.position;
        intendedRotation *= rot;
        // transform.Rotate(axis, angle * Mathf.Deg2Rad);
    }

    /// <summary>
    /// If we use rotation limits, restore previous position/rotation if we passed straight up or down.
    /// This avoids getting upside down.
    /// </summary>
    public void RevertIfOverAxis()
    {
        if (!useRotationLimits) return;

        var overAxis = Vector3.Dot(Vector3.up, this.transform.up);
        if (overAxis < 0)
        {
            this.transform.SetPositionAndRotation(previousPosition, previousRotation);
        }
    }

    /// <summary>
    /// Fly camera airplane style using stick style input
    /// </summary>
    /// <param name="value">Joystick or gamepad style input</param>
    public void FreeFly(Vector3 value)
    {
        StopEasing();
        CalculateSpeed();

        var dir = new Vector3(value.x, 0, value.y);
        PrepareCameraForMove(dir, gamepadMoveSpeed, Space.Self);
        // this.transform.Translate(value.x * gamepadMoveSpeed * Time.deltaTime, 0, value.y * gamepadMoveSpeed * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Activate rotation modifier
    /// </summary>
    /// <param name="rotate">Rotating active</param>
    public void Rotate(bool rotate)
    {
        this.rotate = rotate;
        if (!rotate) rotatingAroundPoint = false;
    }

    /// <summary>
    /// Rotate first person style 
    /// </summary>
    /// <param name="rotateFirstPerson"></param>
    public void RotateFirstPerson(bool rotateFirstPerson)
    {
        this.firstPersonRotate = rotateFirstPerson;
    }

    void Update()
    {
        EaseDragTarget();
    }

    private void LateUpdate()
    {
        opticalRaycastColor = opticalRaycaster.GetColorFromPosition(transform.position, intendedMoveVector.normalized, intendedMoveVector.magnitude);
        // print(intendedMoveVector + "\t" + intendedMoveVector.magnitude + "\t" + opticalRaycastColor);
        LimitIntendedMoveVectorAndMoveCamera();
        transform.rotation *= intendedRotation;
        intendedRotation = Quaternion.identity;
    }

    private void LimitIntendedMoveVectorAndMoveCamera()
    {
        if (!lockDraggingInput)
        {
            intendedMoveVector = Clamp(intendedMoveVector);
        }

        if (opticalRaycastColor.a == 0) // alpha is zero, so no obstruction was found
        {
            transform.position += intendedMoveVector;
            intendedMoveVector = Vector3.zero; //move was applied
            return;
        }

        if (intendedMoveVector.magnitude < cameraComponent.nearClipPlane)
        {
            intendedMoveVector = Vector3.zero;
            return;
        }

        var worldPoint = new Vector3(opticalRaycastColor.r, opticalRaycastColor.g, opticalRaycastColor.b);
        var dir = intendedMoveVector.normalized;
        
        transform.position = worldPoint + -dir * cameraComponent.nearClipPlane; //if an obstruction was found, use the collision point, minus a little bit to avoid ending up exactly on the mesh
        intendedMoveVector = Vector3.zero;
    }

    /// <summary>
    /// Eases out camera motion using drag velocity.
    /// This allows you to drag and throw the camera.
    /// </summary>
    private void EaseDragTarget()
    {
        dragVelocity = new Vector3(Mathf.Lerp(dragVelocity.x, 0, Time.deltaTime * easing), 0, Mathf.Lerp(dragVelocity.z, 0, Time.deltaTime * easing));
        if (!dragging && dragVelocity.magnitude > 0)
        {
            PrepareCameraForMove(-dragVelocity, dragSpeed, Space.World);
            // this.transform.Translate(-dragVelocity * Time.deltaTime * dragSpeed,Space.World);
        }
    }

    /// <summary>
    /// Clears camera drag velocity so it stops directly
    /// </summary>
    private void StopEasing()
    {
        dragVelocity = Vector3.zero;
    }

    /// <summary>
    /// Dragging camera and modifier logic
    /// </summary>
    /// <param name="isDragging"></param>
    public void Drag(bool isDragging)
    {
        if (!dragToMoveCamera) return;
        if (lockDraggingInput)
        {
            dragVelocity = Vector3.zero;
            return;
        }

        if (!dragging && isDragging)
        {
            dragStart = this.transform.position;
            dragStartPointerPosition = currentPointerPosition;
        }
        else if (dragging && !rotatingAroundPoint && currentPointerDelta.magnitude > 0 && !firstPersonRotate)
        {
            CalculateSpeed();
            var screenMove = currentPointerDelta / Screen.height;

            StorePreviousTransform();
            var lookingDown = Vector3.Dot(Vector3.down, transform.forward);
            if (lookingDown >= dragOnPlaneThreshold)
            {
                var flattenedForward = this.transform.up;
                flattenedForward.y = 0;
                PrepareCameraForMove(flattenedForward.normalized, -screenMove.y * dynamicDragSpeed, Space.World);
                // this.transform.Translate(flattenedForward.normalized * -screenMove.y * dynamicDragSpeed, Space.World);
                screenMove.y = 0;
            }

            PrepareCameraForMove(-screenMove, dynamicDragSpeed, Space.Self);
            // this.transform.Translate(-screenMove * dynamicDragSpeed, Space.Self);

            dragVelocity = (previousPosition - this.transform.position) / Time.deltaTime;
        }

        dragging = isDragging;
    }

    /// <summary>
    /// Move towards/from zoompoint
    /// </summary>
    /// <param name="amount">Zoom delta where 1 is towards, and -1 is backing up from zoompoint</param>
    public void ZoomToPointer(float amount)
    {
        rotatingAroundPoint = false;

        CalculateSpeed();
        zoomTarget = GetWorldPoint();
        var direction = zoomTarget - this.transform.position;

        //Make sure we always have a direction. Even when the zoompoint is on the camera.
        if (Vector3.Distance(zoomTarget, this.transform.position) < 0.01f)
            direction = this.transform.forward;

        var targetIsBehind = Vector3.Dot(this.transform.forward, direction) < 0;
        if (targetIsBehind) direction = -direction;

        PrepareCameraForMove(direction.normalized, amount * dynamicZoomSpeed, Space.World);
        // this.transform.Translate(direction.normalized * dynamicZoomSpeed * amount, Space.World);
    }

    /// <summary>
    /// Returns a position on the world 0 plane
    /// </summary>
    /// <param name="screenPoint">Optional screen position. Defaults to pointer input position.</param>
    /// <returns>World position</returns>
    public Vector3 GetWorldPoint(Vector3 screenPoint = default)
    {
        if (screenPoint == default)
        {
            screenPoint = currentPointerPosition;
        }

        var screenRay = cameraComponent.ScreenPointToRay(screenPoint);
        worldPlane.Raycast(screenRay, out float distance);
        var samplePoint = screenRay.GetPoint(Mathf.Min(maxPointerDistance, distance));

        return samplePoint;
    }

    /// <summary>
    /// Sets the pointer screen position
    /// </summary>
    /// <param name="pointerPosition">Screen coordinates</param>
    public void SetPointerPosition(Vector3 pointerPosition)
    {
        currentPointerPosition = pointerPosition;
    }

    /// <summary>
    /// Moves camera left/right on local axis
    /// </summary>
    /// <param name="amount"></param>
    public void MoveHorizontally(float amount)
    {
        StopEasing();

        CalculateSpeed();
        PrepareCameraForMove(Vector3.right, amount * dynamicMoveSpeed, Space.World);
        // this.transform.Translate(Vector3.right * amount * dynamicMoveSpeed * Time.deltaTime, Space.Self);
    }

    /// <summary>
    /// Move camera forward/backwards on own forward, or foward over world plane based on moveForwardOnPlane setting
    /// </summary>
    /// <param name="amount"></param>
    public void MoveForwardBackwards(float amount)
    {
        StopEasing();

        CalculateSpeed();
        var forwardDirection = this.transform.forward;
        if (moveForwardOnPlane)
        {
            forwardDirection.y = 0;
        }

        PrepareCameraForMove(forwardDirection.normalized, amount * dynamicMoveSpeed, Space.World);
        // this.transform.Translate(forwardDirection.normalized * amount * dynamicMoveSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Move camera position up or down in world space
    /// </summary>
    /// <param name="amount">The amount in meters per second</param>
    public void MoveUpDown(float amount)
    {
        StopEasing();
        PrepareCameraForMove(Vector3.up, amount * upAndDownSpeed, Space.World);
        // this.transform.Translate(Vector3.up * amount * upAndDownSpeed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// Calculates the dynamic speed variables based on camera height
    /// </summary>
    private void CalculateSpeed()
    {
        dynamicMoveSpeed = (multiplySpeedBasedOnHeight) ? moveSpeed * Mathf.Abs(this.transform.position.y) : moveSpeed;
        dynamicDragSpeed = (multiplySpeedBasedOnHeight) ? dragSpeed * Mathf.Abs(this.transform.position.y) : dragSpeed;
        dynamicZoomSpeed = (multiplySpeedBasedOnHeight) ? zoomSpeed * Mathf.Abs(this.transform.position.y) : zoomSpeed;

        //Clamp speeds
        dynamicMoveSpeed = Mathf.Clamp(dynamicMoveSpeed, minimumSpeed, maximumSpeed);
        dynamicDragSpeed = Mathf.Clamp(dynamicDragSpeed, minimumDragSpeed, maximumDragSpeed);
        dynamicZoomSpeed = Mathf.Clamp(dynamicZoomSpeed, minimumSpeed, maximumSpeed);
    }

    private void PrepareCameraForMove(Vector3 direction, float speed, Space relativeTo)
    {
        var moveVector = direction * speed * Time.deltaTime;
        if (relativeTo == Space.World)
            intendedMoveVector += moveVector;
        else
            intendedMoveVector += transform.TransformDirection(moveVector);
        
        print("intended move vector changed: " + moveVector);
    }

    /// <summary>
    /// Clamp camera to limits
    /// </summary>
    private Vector3 Clamp(Vector3 position)
    {
        if (position.y > maxCameraHeight)
        {
            position = new Vector3(position.x, maxCameraHeight, position.z);
        }
        else if (position.y < minCameraHeight)
        {
            position = new Vector3(position.x, minCameraHeight, position.z);
        }

        return position;
    }

    private void OnDrawGizmos()
    {
        if (dragging || rotatingAroundPoint)
        {
            Gizmos.DrawSphere(dragStart, 1.0f);
        }
        else
        {
            Gizmos.DrawSphere(zoomTarget, 1.0f);
        }
    }
}