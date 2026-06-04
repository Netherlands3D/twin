using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class DragManipulator : PointerManipulator
{
    private Vector3 start;
    private bool isTracking;
    private bool isDragging;
    private int pointerId;

    protected float movementDeadzone;
    protected MouseButton dragMouseButton = MouseButton.LeftMouse;
    
    public UnityEvent<Vector2> DragStarted = new(); //parameter is startPosition
    public UnityEvent<Vector2> Dragging = new(); //parameter is delta
    public UnityEvent<Vector2> DragEnded = new(); //parameter is endPosition
    
    private Vector2 previousPosition;
    
    public DragManipulator(float deadzone = 32f)
    {
        this.movementDeadzone = deadzone;
        pointerId = -1;
        activators.Add(new ManipulatorActivationFilter { button = dragMouseButton });
        isTracking = false;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
    }

    private void OnPointerDown(PointerDownEvent e)
    {
        if (!CanStartManipulation(e)) return;
        
        start = e.position;
        previousPosition = e.position;
        pointerId = e.pointerId;
        isDragging = movementDeadzone <= 0;
        isTracking = true;
        
        if (isDragging)
        {
            target.CapturePointer(pointerId);
            OnDragStarted(start);
            DragStarted.Invoke(start);
        }
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (!isTracking) return;

        if (!isDragging)
        {
            var totalMove = (Vector2)e.position - (Vector2)start;
            if (totalMove.magnitude < movementDeadzone) return;

            start = e.position;
            previousPosition = e.position;
            isDragging = true;
            target.CapturePointer(pointerId);
            OnDragStarted(start);
            DragStarted.Invoke(start);
            return;
        }
        
        Vector2 delta = (Vector2)e.position - previousPosition;
        previousPosition = e.position;

        OnDrag(delta);
        Dragging.Invoke(delta);
    }
    
    private void OnPointerUp(PointerUpEvent e)
    {
        if (!isTracking || !CanStopManipulation(e)) return;
        
        bool wasDragging = isDragging;
    
        isTracking = false;
        isDragging = false;
        target.ReleasePointer(pointerId);

        if (!wasDragging) return;
        
        OnDragEnded(e.position);
        DragEnded.Invoke(e.position);
    }
    
    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        if(isTracking || isDragging)
            target.CapturePointer(pointerId); //capture the pointer when dragging off the element, 
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
}