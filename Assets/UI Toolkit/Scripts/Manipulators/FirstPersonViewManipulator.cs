using Netherlands3D.FirstPersonViewer;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class FirstPersonViewManipulator : DragManipulator
{
    private readonly LayerMask layers = LayerMask.GetMask(
        "Default",
        "Terrain",
        "Buildings"
    );

    protected override void OnDrag(Vector2 delta)
    {
        base.OnDrag(delta);
        target.style.top = target.layout.y + delta.y;
        target.style.left = target.layout.x + delta.x;
    }

    protected override void OnDragEnded(Vector2 endPosition)
    {
        base.OnDragEnded(endPosition);
        
        if (!GuardPointerIsMovedOutsideOfDeadZone() && 
            !Interface.PointerIsOverUI())
        {
            EnterFPVMode();
        }

        ResetTarget();
    }

    private void ResetTarget()
    {
        target.style.top = 0;
        target.style.left = 0;
    }

    private void EnterFPVMode()
    {
        OpticalRaycaster raycaster = ServiceLocator.GetService<OpticalRaycaster>();
        Vector2 screenPoint = Pointer.current.position.ReadValue();
        raycaster.GetWorldPointAsync(screenPoint, OnRaycastHit, layers);
    }

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

    protected bool GuardPointerIsMovedOutsideOfDeadZone()
    {
        return Mathf.Abs(target.style.top.value.value) < movementDeadzone || Mathf.Abs(target.style.left.value.value) < movementDeadzone;
    }
}