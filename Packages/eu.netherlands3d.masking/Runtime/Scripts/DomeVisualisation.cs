using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Netherlands3D.JavascriptConnection;

namespace Netherlands3D.Masking
{
    public class DomeVisualisation : MonoBehaviour, 
    IPointerClickHandler,
    IPointerUpHandler, IPointerEnterHandler,IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
    {   
        [SerializeField] private PointerEventData.InputButton dragButton = PointerEventData.InputButton.Left;
        [SerializeField] private Material highlighMaterial;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material scaleMaterial;
        private Material domeMaterial;
        private MeshRenderer meshRenderer;
        private Camera mainCamera;
        
        private bool hovering = false;
        private bool isDragging = false;
        private bool hoveringEdge = false;
        [SerializeField] private float hoveringEdgeThreshold = 0.1f;
        [SerializeField] private float scale = 1.0f;


        private Coroutine animationCoroutine;

        [Header("Scale in animation")]
        [SerializeField] private AnimationCurve appearAnimationCurve;
        [SerializeField] private AnimationCurve movedAnimationCurve;
        [SerializeField] private float appearTime = 0.5f;
        SphereCollider sphereCollider;

        private Vector3 offset;
        private Vector3 startScale;
        private Vector3 pointerStartDragPosition;
        private Vector3 pointerObjectStartPosition;
        private float startDistance;

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
            mainCamera = Camera.main;

            sphereCollider = GetComponent<SphereCollider>();
            meshRenderer = this.GetComponent<MeshRenderer>();
            domeMaterial = meshRenderer.material;

            sphereCollider.enabled = false;
        }

        private void Start()
        {
            if(!mainCamera.TryGetComponent<PhysicsRaycaster>(out PhysicsRaycaster raycaster))
            {
                Debug.LogWarning("A PhysicsRaycaster is required  on main Camera in order for the dome to be selectable", this.gameObject);
            }

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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if(eventData.button != dragButton) return;

            dragging.Invoke(true);

            DeterminePointerStartOffsets(eventData.position);      

            // Set the object as being dragged
            isDragging = true;
            startScale = this.transform.localScale;

            //Check if we are dragging the edge ( so we can scale instead of drag )
            hoveringEdge = startDistance > hoveringEdgeThreshold;

            if(hoveringEdge)
            {
                //Highlight 
                meshRenderer.material = scaleMaterial;
                ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.ERESIZE);
                return;
            }

            //Default to dragging the object    
            meshRenderer.material = highlighMaterial;
            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.GRAB);            
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(eventData.button != dragButton) return;

            if (isDragging)
            {
                // If we are dragging the edge of the dome; scale instead of drag.
                if(hoveringEdge)
                {
                    var pointerViewportPoint = mainCamera.ScreenToViewportPoint(eventData.position);
                    var distancePointerMoved = Vector3.Distance(pointerViewportPoint, pointerObjectStartPosition) / startDistance;

                    //Scale
                    this.transform.localScale = startScale * distancePointerMoved;
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

            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.POINTER);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if(eventData.button != dragButton) return;

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
            if(!isDragging) return;

            transform.position = PointerWorldPosition(eventData.position) - offset;
            ScaleByCameraDistance();

            AnimateIn();
        }

        private void DeterminePointerStartOffsets(Vector3 pointerPosition)
        {
            pointerStartDragPosition = mainCamera.ScreenToViewportPoint(pointerPosition);
            pointerObjectStartPosition = mainCamera.WorldToViewportPoint(transform.position);
            pointerObjectStartPosition.z = 0; //Remove depth

            startDistance = Vector3.Distance(pointerStartDragPosition, pointerObjectStartPosition);

            offset = PointerWorldPosition(pointerPosition) - this.transform.position;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;

            if(!isDragging){
                ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.POINTER);
                meshRenderer.material = highlighMaterial;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;

            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.AUTO);

            if(!isDragging)
            {
                meshRenderer.material = defaultMaterial;
            }
        }
    }
}
