using DG.Tweening;
using GG.Extensions;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    public class ViewPositionPickerIcon : MonoBehaviour
    {
        [SerializeField] private Transform arrow;
        [SerializeField] private float intensity = 2.4f;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private Vector2 cursorOffset = new Vector2(0, 30);

        [SerializeField] private GameObject locationSpherePrefab;
        private GameObject locationSphere;
        private OpticalRaycaster raycaster;
        private int snappingCullingMask = 0;

        private void Awake()
        {
            locationSphere = Instantiate(locationSpherePrefab);
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();
            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));

            float cameraDistance = Mathf.Abs(1 - Camera.main.transform.position.y) * .015f;

            locationSphere.transform.localScale = Vector3.one * cameraDistance;
        }

        private void OnDestroy()
        {
            Destroy(locationSphere);
        }

        private void Update()
        {
            Vector2 screenPoint = Pointer.current.position.ReadValue();
            transform.position = screenPoint + cursorOffset;

            Vector2 arrowPosition = arrow.localPosition;
            float yPos = Mathf.Sin(Time.time * speed) * intensity;
            arrowPosition.y = yPos;

            arrow.transform.localPosition = arrowPosition;

            raycaster.GetWorldPointAsync(screenPoint, (point, hit) =>
            {
                if (hit)
                {
                    if (locationSphere == null) return;
                    locationSphere.transform.position = point;
                }
            }, snappingCullingMask);
        }
    }
}
