using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class MovementVisualController : MonoBehaviour
    {
        [SerializeField] private Transform steerTransform;
        [SerializeField] private float steeringSpeed = 5;
        [SerializeField] private float maxSteerAngle = 90f;

        public void SetSteeringWheelRotation(float angle)
        {
            angle = Mathf.Clamp(angle, -maxSteerAngle, maxSteerAngle);

            Vector3 current = steerTransform.localEulerAngles;
            if (current.y > 180f) current.y -= 360f;

            float newY = Mathf.Lerp(current.y, angle, steeringSpeed * Time.deltaTime);

            steerTransform.localEulerAngles = new Vector3(current.x, newY, current.z);

        }
    }
}
