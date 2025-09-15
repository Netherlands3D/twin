using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Functionalities.ObjectInformation;
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
        private int snappingCullingMask = 0;

        private void Start()
        {
            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));
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

                if (IsPointerOverUIObject()) return;

                raycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
                {
                    if (hit)
                    {
                        ObjectSelectorService objectSelectorService = ServiceLocator.GetService<ObjectSelectorService>();
                        SubObjectSelector subObjectSelector = objectSelectorService.SubObjectSelector;

                        string bagID = subObjectSelector.FindSubObjectAtPosition(screenPoint);
                        IMapping mapping = objectSelectorService.FindObjectMapping();
                        objectSelectorService.IsMappingVisible(mapping, bagID);

                        Instantiate(firstPersonViewerPrefab, point, Quaternion.identity);

                        ViewerEvents.OnViewerEntered?.Invoke();
                    }
                }, snappingCullingMask);
            }           
        }

        //Kinda slow
        public static bool IsPointerOverUIObject()
        {
            PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
            eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
            return results.Count > 1; //Idk there seems to be an invisble ui element somewhere.
        }
    }
}
