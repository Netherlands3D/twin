using Netherlands3D.Twin.Samplers;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Netherlands3D.Twin.FloatingOrigin;

namespace Netherlands3D.FirstPersonViewer
{
    public class ViewPositionPicker : MonoBehaviour, IPointerUpHandler
    {
        private bool isPointerDown;

        private ViewPositionPickerIcon picker;
        [SerializeField] private ViewPositionPickerIcon pickerPrefab;
        [SerializeField] private GameObject firstPersonViewerPrefab;
        private int snappingCullingMask = 0;

        private void Start()
        {
            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings"));
        }

        public void PointerDown()
        {
            isPointerDown = true;
            picker = Instantiate(pickerPrefab, transform.root); // <-- $$ Change Root to propper location. 
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isPointerDown)
            {
                Destroy(picker.gameObject);

                OpticalRaycaster raycaster = ServiceLocator.GetService<OpticalRaycaster>();

                Vector2 screenPoint = Pointer.current.position.ReadValue();
                raycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
                {
                    if (hit) Instantiate(firstPersonViewerPrefab, point, Quaternion.identity);
                }, snappingCullingMask);
            }           
        }
    }
}
