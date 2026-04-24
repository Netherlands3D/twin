using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class DragManipulator : PointerManipulator
{
    private Vector3 start;
    private bool active;
    private int pointerId;
    private Vector2 startSize;

    protected float movementDeadzone = 32f;
    protected MouseButton dragMouseButton = MouseButton.LeftMouse;
    
    public UnityEvent<Vector2> DragStarted = new(); //parameter is startPosition
    public UnityEvent<Vector2> Dragging = new(); //parameter is delta
    public UnityEvent<Vector2> DragEnded = new(); //parameter is endPosition
    
    public DragManipulator()
    {
        pointerId = -1;
        activators.Add(new ManipulatorActivationFilter { button = dragMouseButton });
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

    private void OnPointerDown(PointerDownEvent e)
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

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (GuardPointerIsDragging()) return;

        Vector2 diff = e.localPosition - start;

        OnDrag(diff);
        Dragging.Invoke(diff);

        e.StopPropagation();
    }
    private void OnPointerUp(PointerUpEvent e)
    {
        if (GuardPointerIsDragging() || !CanStopManipulation(e)) return;
        
        OnDragEnded(e.localPosition);
        
        active = false;
        target.ReleaseMouse();
        
        e.StopPropagation();
        DragEnded.Invoke(e.localPosition);
    }
    
    private void StartDragging(PointerDownEvent e)
    {
        start = e.localPosition;
        pointerId = e.pointerId;

        active = true;
        target.CapturePointer(pointerId);
        
        OnDragStarted(start);
        DragStarted.Invoke(start);
    }

    protected virtual void OnDragStarted(Vector2 startPosition)
    {
    }
    
    protected virtual void OnDrag(Vector2 delta)
    {
    }
    
    protected virtual void OnDragEnded(Vector2 endPosition)
    {
    }

    protected bool GuardPointerIsDragging() => !active || !target.HasPointerCapture(pointerId);
}