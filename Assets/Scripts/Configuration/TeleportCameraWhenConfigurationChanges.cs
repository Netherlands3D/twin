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

            if (EPSG7415.IsValid(newOrigin.ToVector3RD()) == false)
            {
                Debug.LogWarning(
                    $"{newOrigin} is not a valid RD coordinate, camera is not moving to that position"
                );

                yield break;
            };

            mainCamera.transform.position = CoordinateConverter
                .ConvertTo(newOrigin, CoordinateSystem.Unity)
                .ToVector3();
        }
    }
}