using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    public class ViewPositionPicker : MonoBehaviour, IPointerUpHandler
    {
        private bool isPointerDown;

        private ViewPositionPickerIcon picker;
        [SerializeField] private ViewPositionPickerIcon pickerPrefab;
        [SerializeField] private GameObject firstPersonViewerPrefab;
        [SerializeField] private LayerMask snappingCullingMask = 0;

        public void PointerDown()
        {
            isPointerDown = true;
            picker = Instantiate(pickerPrefab, transform.root);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isPointerDown)
            {
                Destroy(picker.gameObject);

                OpticalRaycaster raycaster = ServiceLocator.GetService<OpticalRaycaster>();

                Vector2 screenPoint = Pointer.current.position.ReadValue();

                if (Interface.PointerIsOverUI()) return;

                raycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
                {
                    if (hit)
                    {
                        //$$ TO-DO
                        //Commentent code not working or changing anything based on the visibilty of the building.

                        //ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
                        //SubObjectSelector subObjectSelector = objectSelectorService.SubObjectSelector;

                        //string bagID = subObjectSelector.FindSubObjectAtPosition(screenPoint);
                        //IMapping mapping = objectSelectorService.FindObjectMapping();
                        //if (objectSelectorService.IsMappingVisible(mapping, bagID))
                        //{
                            Instantiate(firstPersonViewerPrefab, point, Quaternion.identity);

                            ViewerEvents.OnViewerEntered?.Invoke();
                        //}
                    }
                }, snappingCullingMask);
            }           
        }
    }
}
