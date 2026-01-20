using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    public class ViewPositionPicker : MonoBehaviour, IPointerUpHandler
    {
        private ViewPositionPickerIcon picker;
        [SerializeField] private ViewPositionPickerIcon pickerPrefab;
        [SerializeField] private LayerMask snappingCullingMask = 0;

        public void PointerDown()
        {
            Vector2 screenPoint = Pointer.current.position.ReadValue();
            picker = Instantiate(pickerPrefab, screenPoint, Quaternion.identity, transform.root);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Destroy(picker.gameObject);

            OpticalRaycaster raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            Vector2 screenPoint = Pointer.current.position.ReadValue();

            if (Interface.PointerIsOverUI()) return;

            raycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
            {
                if (hit)
                {
                    //$$ TODO Fix being able to walk on invicible buildings.
                    //Commentent code not working or changing anythings based on the visibilty of the building.

                    //ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
                    //SubObjectSelector subObjectSelector = objectSelectorService.SubObjectSelector;

                    //string bagID = subObjectSelector.FindSubObjectAtPosition(screenPoint);
                    //IMapping mapping = objectSelectorService.FindObjectMapping();
                    //if (objectSelectorService.IsMappingVisible(mapping, bagID))
                    //{
                    FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
                    fpv.transform.position = point;
                    fpv.OnViewerEntered.Invoke();

                    //}
                }
            }, snappingCullingMask);
        }
    }
}
