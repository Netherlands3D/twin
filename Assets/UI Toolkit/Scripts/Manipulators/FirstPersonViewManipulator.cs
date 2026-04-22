using Netherlands3D.FirstPersonViewer;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class FirstPersonViewManipulator : PointerManipulator
{
    private Vector3 start;
    private bool active;
    private int pointerId;
    private Vector2 startSize;

    private readonly LayerMask layers = LayerMask.GetMask(
        "Default", 
        "Terrain", 
        "Buildings"
    );

    private readonly float movementDeadzone = 32f;

    public FirstPersonViewManipulator()
    {
        pointerId = -1;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        active = false;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected void OnPointerDown(PointerDownEvent e)
    {
        if (active)
        {
            e.StopImmediatePropagation();
            return;
        }

        if (!CanStartManipulation(e)) return;

        StartDragging(e);
        e.StopPropagation();
    }

    protected void OnPointerMove(PointerMoveEvent e)
    {
        if (GuardPointerIsDragging()) return;

        Vector2 diff = e.localPosition - start;

        target.style.top = target.layout.y + diff.y;
        target.style.left = target.layout.x + diff.x;

        e.StopPropagation();
    }

    protected void OnPointerUp(PointerUpEvent e)
    {
        if (GuardPointerIsDragging() || !CanStopManipulation(e)) return;
        
        OnDrop();
        StopDragging();
        e.StopPropagation();
    }

    private void StartDragging(PointerDownEvent e)
    {
        start = e.localPosition;
        pointerId = e.pointerId;

        active = true;
        target.CapturePointer(pointerId);
    }

    private void StopDragging()
    {
        target.style.top = 0;
        target.style.left = 0;

        active = false;
        target.ReleaseMouse();
    }

    private void OnDrop()
    {
        if (GuardPointerIsMovedOutsideOfDeadZone()) return;
        if (Interface.PointerIsOverUI()) return;
        
        OpticalRaycaster raycaster = ServiceLocator.GetService<OpticalRaycaster>();

        Vector2 screenPoint = Pointer.current.position.ReadValue();

        raycaster.GetWorldPointAsync(screenPoint, OnRaycastHit, layers);
    }

    private bool GuardPointerIsDragging() => !active || !target.HasPointerCapture(pointerId);
    private bool GuardPointerIsMovedOutsideOfDeadZone() => Mathf.Abs(target.style.top.value.value) < movementDeadzone || Mathf.Abs(target.style.left.value.value) < movementDeadzone;

    private void OnRaycastHit(Vector3 point, bool hit)
    {
        if (!hit) return;

        FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();

        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0;
        forward.Normalize();

        fpv.SetPositionAndRotation(point, Quaternion.LookRotation(forward, Vector3.up));
        fpv.EnterViewer(null, null);
    }
}