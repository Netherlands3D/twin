using System.Collections;
using Netherlands3D.Coordinates;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class TeleportCameraWhenConfigurationChanges : MonoBehaviour
    {
        [Tooltip("The camera that should be teleported, uses Camera.main when left empty")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Configuration configuration;
        private Coroutine movingCoRoutine;

        [SerializeField] private Vector3 defaultCameraOffset = new();

        private void Awake()
        {
            mainCamera = mainCamera == null ? Camera.main : mainCamera;
        }

        private void OnEnable()
        {
            MoveOrigin(configuration.Origin);
            configuration.OnOriginChanged.AddListener(MoveOrigin);
        }

        private void OnDisable()
        {
            configuration.OnOriginChanged.RemoveListener(MoveOrigin);
        }

        private void MoveOrigin(Coordinate newOrigin)
        {
            if (movingCoRoutine != null)
            {
                StopCoroutine(movingCoRoutine);
            }

            movingCoRoutine = StartCoroutine(DebounceMovingOfOrigin(newOrigin));
        }

        private IEnumerator DebounceMovingOfOrigin(Coordinate newOrigin)
        {
            yield return new WaitForSeconds(.3f);
            movingCoRoutine = null;

            var newCameraPosition = CoordinateConverter
                .ConvertTo(newOrigin, CoordinateSystem.Unity)
                .ToVector3();

            mainCamera.transform.position = newCameraPosition + defaultCameraOffset;
        }
    }
}