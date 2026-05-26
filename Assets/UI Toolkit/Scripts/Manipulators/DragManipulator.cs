using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class DragManipulator : PointerManipulator
{
    private Vector3 start;
    private bool active;
    private int pointerId;
    private Vector2 startSize;

    protected float movementDeadzone;
    protected MouseButton dragMouseButton = MouseButton.LeftMouse;
    
    public UnityEvent<Vector2> DragStarted = new(); //parameter is startPosition
    public UnityEvent<Vector2> Dragging = new(); //parameter is delta
    public UnityEvent<Vector2> DragEnded = new(); //parameter is endPosition
    
    private Vector2 previousPosition;
    private bool deadzonePassed;
    
    public DragManipulator(float deadzone = 32f)
    {
        this.movementDeadzone = deadzone;
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

    private void StartDragging(PointerDownEvent e)
    {
        start = e.localPosition;
        previousPosition = e.localPosition;
        pointerId = e.pointerId;
        deadzonePassed = movementDeadzone <= 0;
            
        active = true;
        target.CapturePointer(pointerId);
        
        if (deadzonePassed)
        {
            OnDragStarted(start);
            DragStarted.Invoke(start);
        }
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (GuardPointerIsDragging()) return;

        if (!deadzonePassed)
        {
            var totalMove = (Vector2)e.localPosition - (Vector2)start;
            if (totalMove.magnitude < movementDeadzone) return;

            deadzonePassed = true;
            previousPosition = e.localPosition;
            OnDragStarted(start);
            DragStarted.Invoke(start);
            return;
        }
        
        Vector2 delta = (Vector2)e.localPosition - previousPosition; // frame delta, not start delta
        previousPosition = e.localPosition;

        OnDrag(delta);
        Dragging.Invoke(delta);

        e.StopPropagation();
    }
    
    private void OnPointerUp(PointerUpEvent e)
    {
        if (GuardPointerIsDragging() || !CanStopManipulation(e)) return;
        
        bool wasDragging = deadzonePassed;
    
        active = false;
        deadzonePassed = false;
        target.ReleaseMouse();
        e.StopPropagation();

        if (!wasDragging) return;
        
        OnDragEnded(e.localPosition);
        DragEnded.Invoke(e.localPosition);
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