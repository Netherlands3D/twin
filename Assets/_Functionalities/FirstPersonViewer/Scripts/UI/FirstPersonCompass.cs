using UnityEngine;
using GG.Extensions;
using UnityEngine.UI;
using Netherlands3D.Services;


namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FirstPersonCompass : MonoBehaviour
    {
        [SerializeField] private Transform objectTransform;

        [SerializeField] private Image arrowImage;
        [SerializeField] private Color NorthColor;
        private const float northAngleMargin = 1.0f;
        [SerializeField] private Color arrowColor;

        private void Update()
        {
            //TEMP IF Check
            if (objectTransform != null)
            {
                CompassUpdate(objectTransform.forward);
            }
        }

        private void CompassUpdate(Vector3 direction)
        {
            float angle = Vector3.SignedAngle(direction, Vector3.forward, Vector3.up);
            arrowImage.transform.SetRotationZ(-angle);
            arrowImage.color = Mathf.Abs(angle) < northAngleMargin ? NorthColor : arrowColor;
        }

        public void OnCompassClick()
        {
            ServiceLocator.GetService<FirstPersonViewer>().OnSetCameraNorth.Invoke();
        }
    }
}
