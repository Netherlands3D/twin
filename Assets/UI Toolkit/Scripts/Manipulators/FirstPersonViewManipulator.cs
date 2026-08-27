using Netherlands3D.FirstPersonViewer;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class FirstPersonViewManipulator : DragManipulator
{
    private readonly VisualElement draggedElement;
    
    private Vector2 totalDrag;
    private Vector2 layoutPositionAtDragStart;
    

    private readonly LayerMask layers = LayerMask.GetMask(
        "Default",
        "Terrain",
        "Buildings"
    );

    public FirstPersonViewManipulator(VisualElement draggedElement, float deadzone) : base(deadzone)
    {
        this.draggedElement = draggedElement;
    }

    protected override void OnDragStarted(Vector2 startPosition)
    {
        base.OnDragStarted(startPosition);
        totalDrag = Vector2.zero;
        layoutPositionAtDragStart = new Vector2(target.layout.x, target.layout.y);
    }

    protected override void OnDrag(Vector2 delta)
    {
        base.OnDrag(delta);
        totalDrag += delta;
        draggedElement.style.top = layoutPositionAtDragStart.y + totalDrag.y;
        draggedElement.style.left = layoutPositionAtDragStart.x + totalDrag.x;
    }

    protected override void OnDragEnded(Vector2 endPosition)
    {
        base.OnDragEnded(endPosition);

        var mousePos = Mouse.current.position.ReadValue();
        var picked = App.UIRoot.Root.panel.Pick(App.UIRoot.GetPanelClickPosition());
        var isPointerOverUI = App.UIRoot.IsPointerOverUI(out picked);
        isPointerOverUI = isPointerOverUI && picked != draggedElement;
        if (!isPointerOverUI)
        {
            App.UIRoot.EnableFPVUI();
            EnterFPVMode();
        }

        ResetTarget();
    }

    private void ResetTarget()
    {
        draggedElement.style.top = 0;
        draggedElement.style.left = 0;
        totalDrag = Vector2.zero;
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
}