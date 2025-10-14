using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Manipulators
{
    public class DragToWorldManipulator : PointerManipulator
    {
        private readonly VisualElement overlay;
        private readonly Func<VisualElement, VisualElement> ghostFactory;
        private readonly Action<VisualElement, Vector2> onDrop;

        private bool dragging;
        private int pointerId;
        private Vector2 startPos;
        private VisualElement ghost;

        public DragToWorldManipulator(
            VisualElement overlay, 
            Func<VisualElement, VisualElement> ghostFactory, 
            Action<VisualElement, Vector2> onDrop
        ) {
            this.overlay = overlay;
            this.ghostFactory = ghostFactory;
            this.onDrop = onDrop;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        private void RegisterCallbacksOnOverlay()
        {
            overlay.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            overlay.RegisterCallback<PointerUpEvent>(OnPointerUp);
            overlay.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        private void UnregisterCallbacksOnOverlay()
        {
            overlay.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            overlay.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            overlay.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;
            if (dragging) return;

            RegisterCallbacksOnOverlay();
            
            pointerId = evt.pointerId;
            startPos = evt.position;

            overlay.CapturePointer(pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!overlay.HasPointerCapture(evt.pointerId)) return;

            if (!dragging)
            {
                if (((Vector2)evt.position - startPos).sqrMagnitude < 36f) return; // 6px threshold
                BeginDrag(evt.position);
            }

            UpdateGhost(evt.position);
            evt.StopPropagation();

            // Optional: live raycast for hover feedback
            // var screen = RuntimePanelUtils.PanelToScreen(target.panel, evt.position);
            // var ray = worldCamera.ScreenPointToRay(screen);
            // if (Physics.Raycast(ray, out var hit))
            // {
                // snap/preview at hit.point
            // }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!overlay.HasPointerCapture(evt.pointerId)) return;
            EndDrag(evt.position);
            overlay.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!overlay.HasPointerCapture(evt.pointerId)) return;
            CancelDrag();
            overlay.ReleasePointer(evt.pointerId);
        }

        private void BeginDrag(Vector2 pos)
        {
            dragging = true;

            // activate overlay if you want (not strictly required with capture)
            overlay.pickingMode = PickingMode.Position;

            ghost = ghostFactory(target);
            ghost.pickingMode = PickingMode.Ignore;
            ghost.style.position = Position.Absolute;
            ghost.style.opacity = 0.9f;
            ghost.style.width = target.resolvedStyle.width;
            ghost.style.height = target.resolvedStyle.height;

            overlay.Add(ghost);
            UpdateGhost(pos);
        }

        private void UpdateGhost(Vector2 pos)
        {
            if (ghost == null) return;
            ghost.style.left = pos.x - ghost.resolvedStyle.width * 0.5f;
            ghost.style.top = pos.y - ghost.resolvedStyle.height * 0.5f;
        }

        private void EndDrag(Vector2 pos)
        {
            if (!dragging) return;

            onDrop(target, pos);

            // var screen = RuntimePanelUtils.PanelToScreen(target.panel, pos);
            // var ray = worldCamera.ScreenPointToRay(screen);
            //
            // if (Physics.Raycast(ray, out var hit))
            // {
            //     // do drop with payload + hit.point
            //     Debug.Log($"Dropped {payload} on {hit.collider.name} at {hit.point}");
            // }
            // else
            // {
            //     // no world hit
            // }

            Cleanup();
        }

        private void CancelDrag()
        {
            if (!dragging) return;
            Cleanup();
        }

        private void Cleanup()
        {
            UnregisterCallbacksOnOverlay();
            dragging = false;
            ghost?.RemoveFromHierarchy();
            ghost = null;
            overlay.pickingMode = PickingMode.Ignore; // return to click-through
        }
    }
}