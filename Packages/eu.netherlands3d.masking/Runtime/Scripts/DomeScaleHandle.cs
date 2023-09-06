using Netherlands3D.Masking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class DomeScaleHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private DomeVisualisation domeVisualisation;
        [SerializeField] private Vector3 iconOffset = Vector3.zero;
        [SerializeField] private float scaleMultiplier = 2.0f;

        private Vector3 startScale = Vector3.one;

        private RectTransform rectTransform;

        private Camera mainCamera;

        private Vector3 pointerStartDragPosition;
        private Vector3 pointerObjectStartPosition;

        private float startDistance;

        public UnityEvent<bool> onHoveringChange = new();
        public UnityEvent<bool> onDragChange = new();

        private bool hovering = false;
        private bool dragging = false;

        public bool IsHovering { get => hovering; }

        private void Awake() {
            mainCamera = Camera.main;
            onHoveringChange.Invoke(false);
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update() {
            var worldPositionDome = mainCamera.WorldToViewportPoint(domeVisualisation.transform.position);
            worldPositionDome.z = 0;

            var domeScaleInScreen = domeVisualisation.transform.localScale.x / Vector3.Distance(domeVisualisation.transform.position,mainCamera.transform.position);

            rectTransform.anchorMin = worldPositionDome + iconOffset * domeScaleInScreen;
			rectTransform.anchorMax = worldPositionDome + iconOffset * domeScaleInScreen;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            onHoveringChange.Invoke(IsHovering);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            onHoveringChange.Invoke(IsHovering);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            pointerObjectStartPosition = mainCamera.WorldToViewportPoint(domeVisualisation.transform.position);
            pointerObjectStartPosition.z = 0; //Remove depth

            startDistance = Vector3.Distance(pointerStartDragPosition, pointerObjectStartPosition);

            startScale = domeVisualisation.transform.localScale;
            dragging = true;
            onDragChange.Invoke(dragging);
        }

        public void OnDrag(PointerEventData eventData)
        {
            var pointerViewportPoint = mainCamera.ScreenToViewportPoint(eventData.position);
            var distancePointerMoved = Vector3.Distance(pointerViewportPoint, pointerObjectStartPosition) / startDistance;

            //Scale
            domeVisualisation.transform.localScale = startScale * scaleMultiplier * distancePointerMoved;  
        }
         
        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log("Ended scaling drag");
            dragging = false;
            onDragChange.Invoke(dragging);
        }
    }
}
