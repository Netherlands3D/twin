using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Masking
{
    public class DomeVisualisation : MonoBehaviour, 
    IPointerClickHandler,
    IPointerUpHandler, IPointerEnterHandler,IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
    {   
        [SerializeField] private PointerEventData.InputButton dragButton = PointerEventData.InputButton.Left;
        [SerializeField] private Color highlightColor = Color.yellow;
        [SerializeField] private Color defaultColor = Color.white;
        private Material domeMaterial;

        private Camera mainCamera;
        
        private bool hovering = false;
        private bool isDragging = false;
        private bool hoveringEdge = false;
        [SerializeField] private float scale = 1.0f;


        private Coroutine animationCoroutine;

        [Header("Scale in animation")]
        [SerializeField] private AnimationCurve appearAnimationCurve;
        [SerializeField] private AnimationCurve movedAnimationCurve;
        [SerializeField] private float appearTime = 0.5f;
        SphereCollider sphereCollider;

        private Vector3 offset;

        [Header("Events")]
        public UnityEvent<bool> dragging = new();
        public UnityEvent selected = new();
        public UnityEvent deselected = new();

        public bool AllowInteraction
        {
            get => sphereCollider.enabled;
            set
            {
                sphereCollider.enabled = value;
            }
        }

        private void Awake() {
            sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.enabled = false;
            mainCamera = Camera.main;
        }

        private void Start()
        {
            if(!mainCamera.TryGetComponent<PhysicsRaycaster>(out PhysicsRaycaster raycaster))
            {
                Debug.LogWarning("A PhysicsRaycaster is required  on main Camera in order for the dome to be selectable", this.gameObject);
            }

            domeMaterial = this.GetComponent<MeshRenderer>().material;
        }

        public void MoveToScreenPoint(Vector2 screenPoint)
        {
            transform.position = PointerWorldPosition(screenPoint);
            ScaleByCameraDistance();
        }
        
        public void AnimateIn()
        {
            if(animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(Animate());
        }

        
        private IEnumerator Animate()
        {
            var animationTime = 0.0f;

            var targetScale = hovering ? this.transform.localScale : ScaleByCameraDistance();
            var animationCurve =  hovering ? movedAnimationCurve : appearAnimationCurve;

            while(animationTime < appearTime){
                animationTime += Time.deltaTime;
                var curveTime = animationTime / appearTime;
                var curveValue = animationCurve.Evaluate(curveTime);

                this.transform.localScale = targetScale * curveValue;

                yield return null;
            }

            this.transform.localScale = targetScale;
            animationCoroutine = null;
        }

        private void Update()
        {
            //Check if we are hovering edge
            if(hovering && Pointer.current != null)
            {
                Vector2 pointerPosition = Pointer.current.position.ReadValue();
                var objectPosition = mainCamera.WorldToScreenPoint(this.transform.position);
            }        
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if(eventData.button != dragButton) return;

            DeterminePointerOffset(eventData.position);

            // Set the object as being dragged
            isDragging = true;
            //Highlight 
            domeMaterial.color = highlightColor;

            dragging.Invoke(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(eventData.button != dragButton) return;

            if (isDragging)
            {
                // If we are dragging the edge of the dome; scale instead of drag.
                if(hoveringEdge)
                {
                    //Scale
                    return;
                }

                // Update the object's position based on the pointer position
                transform.position = PointerWorldPosition(eventData.position) - offset;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;

            dragging.Invoke(false);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(eventData.button != dragButton) return;

            // Update the object's position based on the pointer position
            transform.position = PointerWorldPosition(eventData.position) - offset;

            // Reset the dragging flag
            isDragging = false;
        }

        private Vector3 PointerWorldPosition(Vector2 position)
        {
            // Calculate the mouse position in world space
            Ray ray = mainCamera.ScreenPointToRay(position);
            Plane plane = new Plane(Vector3.up, transform.parent.position);
            plane.Raycast(ray, out float distance);
            Vector3 pointerWorldPosition = ray.GetPoint(distance);

            return pointerWorldPosition;
        }

        public Vector3 ScaleByCameraDistance()
        {
            var distanceScale = Mathf.Max(1.0f, scale * Vector3.Distance(mainCamera.transform.position, transform.position));
            return Vector3.one * distanceScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            transform.position = PointerWorldPosition(eventData.position) - offset;
            ScaleByCameraDistance();

            AnimateIn();

            domeMaterial.color = highlightColor;
        }

        private void DeterminePointerOffset(Vector3 pointerPosition)
        {
            offset = PointerWorldPosition(pointerPosition) - this.transform.position;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            domeMaterial.color = highlightColor;
            hovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            domeMaterial.color = defaultColor;
            hovering = false;
        }
    }
}
