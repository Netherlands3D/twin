using Netherlands3D.FirstPersonViewer;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class FirstPersonViewManipulator : DragManipulator
{
    private readonly Icon sourceIcon;

    private Icon dragPreview;
    private VisualElement dragLayer;
    
    private Vector2 totalDrag;
    private Vector2 layoutPositionAtDragStart;
    

    private readonly LayerMask layers = LayerMask.GetMask(
        "Default",
        "Terrain",
        "Buildings"
    );

    public FirstPersonViewManipulator(Icon icon, float deadzone) : base(deadzone, TrickleDown.TrickleDown)
    {
        this.sourceIcon = icon;
    }

    protected override void OnDragStarted(Vector2 startPosition)
    {
        base.OnDragStarted(startPosition);
        totalDrag = Vector2.zero;
        dragLayer = sourceIcon.panel.visualTree;
        layoutPositionAtDragStart = dragLayer.WorldToLocal(sourceIcon.worldBound.position);

        dragPreview = new Icon
        {
            Image = sourceIcon.Image,
            pickingMode = PickingMode.Ignore
        };
        
        dragPreview.style.unityBackgroundImageTintColor =
            sourceIcon.resolvedStyle.unityBackgroundImageTintColor;
        
        dragPreview.style.position = Position.Absolute;
        dragPreview.style.left = layoutPositionAtDragStart.x;
        dragPreview.style.top = layoutPositionAtDragStart.y;
        dragPreview.style.width = sourceIcon.resolvedStyle.width;
        dragPreview.style.height = sourceIcon.resolvedStyle.height;

        dragLayer.Add(dragPreview);
        dragPreview.BringToFront();
        
        sourceIcon.style.visibility = Visibility.Hidden;
    }

    protected override void OnDrag(Vector2 delta)
    {
        base.OnDrag(delta);
        totalDrag += delta;
        dragPreview.style.top = layoutPositionAtDragStart.y + totalDrag.y;
        dragPreview.style.left = layoutPositionAtDragStart.x + totalDrag.x;
    }

    protected override void OnDragEnded(Vector2 endPosition)
    {
        base.OnDragEnded(endPosition);

        var mousePos = Mouse.current.position.ReadValue();
        var picked = App.UIRoot.Root.panel.Pick(App.UIRoot.GetPanelClickPosition());
        var isPointerOverUI = App.UIRoot.IsPointerOverUI(out picked);
        isPointerOverUI = isPointerOverUI && picked != dragPreview;
        if (!isPointerOverUI)
        {
            App.UIRoot.EnableFPVUI();
            EnterFPVMode();
        }

        ResetTarget();
    }

    private void ResetTarget()
    {
        dragPreview?.RemoveFromHierarchy();
        dragPreview = null;
        dragLayer = null;

        sourceIcon.style.visibility = StyleKeyword.Null;
        totalDrag = Vector2.zero;
    }

    private void EnterFPVMode()
    {
        OpticalRaycaster raycaster = ServiceLocator.GetService<OpticalRaycaster>();
        Vector2 screenPoint = Pointer.current.position.ReadValue();
        var ray = App.Cameras.ActiveCamera.ScreenPointToRay(screenPoint);
        var isHit = raycaster.Raycast(ray.origin, ray.direction, out var hitPosition, layers);
        OnRaycastHit(hitPosition, isHit);
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