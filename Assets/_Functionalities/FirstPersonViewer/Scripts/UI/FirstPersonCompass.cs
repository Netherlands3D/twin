using UnityEngine;
using DG.Tweening;
using GG.Extensions;
using UnityEngine.UI;
using Netherlands3D.FirstPersonViewer.Events;


namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FirstPersonCompass : MonoBehaviour
    {
        [SerializeField] private Image arrowImage;
        [SerializeField] private Color NorthColor;
        private const float northAngleMargin = 1.0f;
        [SerializeField] private Color arrowColor;

        private void OnEnable()
        {
            ViewerEvents.OnCameraRotation += CompassUpdate;
        }

        private void OnDisable()
        {
            ViewerEvents.OnCameraRotation -= CompassUpdate;
        }

        private void CompassUpdate(Vector3 direction)
        {
            float angle = Vector3.SignedAngle(direction, Vector3.forward, Vector3.up);
            arrowImage.transform.SetRotationZ(-angle);
            arrowImage.color = Mathf.Abs(angle) < northAngleMargin ? NorthColor : arrowColor;
        }

        public void OnCompassClick()
        {
            ViewerEvents.OnSetCameraNorth?.Invoke();
        }
    }
}
