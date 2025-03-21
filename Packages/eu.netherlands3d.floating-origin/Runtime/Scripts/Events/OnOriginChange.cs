using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class OnOriginChange : MonoBehaviour
    {
        [SerializeField] private UnityEvent OnOriginChanged = new();
        private Origin origin;

        private void OnEnable()
        {
            if (origin == null)
            {
                origin = FindObjectOfType<Origin>();
            }

            origin.onPostShift.AddListener(OnNewOrigin);
        }

        private void OnDisable()
        {
            origin.onPostShift.RemoveListener(OnNewOrigin);
        }

        private void OnNewOrigin(Coordinate from, Coordinate to)
        {
            OnOriginChanged.Invoke();
        }
    }
}
