using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.UI;
using UnityEngine;

namespace Netherlands3D.Twin.Cameras
{
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] private WorldPositionFollower crosshairPrefab;
        private WorldPositionFollower crosshair;

        private void OnEnable()
        {
            crosshair?.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            crosshair?.gameObject.SetActive(false);
        }

        private void Start()
        {
            crosshair = Instantiate(crosshairPrefab, CanvasID.GetCanvasByType(CanvasType.World).transform);
        }

        private void LateUpdate()
        {
            crosshair.StickTo(new Coordinate(transform.position));
        }
    }
}