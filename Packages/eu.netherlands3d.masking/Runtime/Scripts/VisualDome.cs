using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Netherlands3D.JavascriptConnection;
using UnityEngine.InputSystem;

namespace Netherlands3D.Masking
{
    public class VisualDome : MonoBehaviour,
    IPointerClickHandler,
    IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler,
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
        [SerializeField] private float scale = 1.0f;


        private Coroutine animationCoroutine;

        [Header("Scale in animation")]
        [SerializeField] private AnimationCurve appearAnimationCurve;
        [SerializeField] private AnimationCurve movedAnimationCurve;
        [SerializeField] private float appearTime = 0.5f;

        private Vector3 offset;

        [Header("Events")]
        public UnityEvent<bool> dragging = new();
        public UnityEvent selected = new();
        public UnityEvent deselected = new();
        public UnityEvent<bool> onHoveringChange = new();

        public Transform ScaleAnchor => scaleAnchor;
        
        [Header("References")]
        [SerializeField] private Transform scaleAnchor;

        private Vector3 targetScale;

        private void Awake()
        {
            mainCamera = Camera.main;
            meshRenderer = this.GetComponent<MeshRenderer>();
            domeMaterial = meshRenderer.material;
        }

        private void Start()
        {
            if (!mainCamera.TryGetComponent(out PhysicsRaycaster raycaster))
            {
                Debug.LogWarning("A PhysicsRaycaster is required  on main Camera in order for the dome to be selectable", this.gameObject);
            }
        }

        private void Update() {
            this.transform.rotation = mainCamera.transform.rotation;
            
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 60);
        }

        public void MoveToScreenPoint()
        {
            UpdateFromPointerPosition();
            ScaleByCameraDistance();
        }

        public void InteruptAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
        }

        public void SetTargetScale(Vector3 targetScale)
        {
            this.targetScale = targetScale;
        }

        public void AnimateIn()
        {
            InteruptAnimation();
            animationCoroutine = StartCoroutine(
                Animate(ScaleByCameraDistance())
            );
        }

        public void AnimateOut(Action onFinish = null)
        {
            InteruptAnimation();
            animationCoroutine = StartCoroutine(
                Animate(Vector3.zero, onFinish)
            );
        }

        private IEnumerator Animate(Vector3 towardsScale, Action onFinish = null)
        {
            var animationTime = 0.0f;
            var startScale = transform.localScale;

            while (animationTime < appearTime)
            {
                animationTime += Time.deltaTime;
                var curveTime = animationTime / appearTime;
                var curveValue = appearAnimationCurve.Evaluate(curveTime);

                targetScale = Vector3.Lerp(
                    startScale,
                    towardsScale,
                    curveValue
                );

                yield return null;
            }
            targetScale = towardsScale;
            onFinish?.Invoke();
            animationCoroutine = null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != dragButton) return;

            InteruptAnimation();

            dragging.Invoke(true);

            DeterminePointerStartOffset(eventData.position);

            // Set the object as being dragged
            isDragging = true;

            //Default to dragging the object    
            meshRenderer.material = highlighMaterial;
            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.GRAB);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != dragButton) return;

            if (isDragging)
            {
                // Update the object's position based on the pointer position
                UpdateFromPointerPosition();
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
            if (eventData.button != dragButton) return;

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
            if(!mainCamera) return Vector3.one;

            var distanceScale = Mathf.Max(1.0f, scale * Vector3.Distance(mainCamera.transform.position, transform.position));
            return Vector3.one * distanceScale;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isDragging) return;

            UpdateFromPointerPosition();
            ScaleByCameraDistance();

            AnimateIn();
        }

        public void UpdateFromPointerPosition()
        {
            transform.position = PointerWorldPosition(Pointer.current.position.ReadValue()) - offset;
        }

        private void DeterminePointerStartOffset(Vector3 pointerPosition)
        {
            offset = PointerWorldPosition(pointerPosition) - this.transform.position;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ChangeHoverState(true);

            if (!isDragging)
            {
                ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.POINTER);
                meshRenderer.material = highlighMaterial;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ChangeHoverState(false);

            ChangePointerStyleHandler.ChangeCursor(ChangePointerStyleHandler.Style.AUTO);

            if (!isDragging)
            {
                meshRenderer.material = defaultMaterial;
            }
        }

        public void ChangeScalingMode(bool scaling)
        {
            meshRenderer.material = scaling ? scaleMaterial : defaultMaterial;
        }

        public void ChangeHoverState(bool hovering)
        {
            this.hovering = hovering;
            onHoveringChange.Invoke(hovering);
        }
    }
}
