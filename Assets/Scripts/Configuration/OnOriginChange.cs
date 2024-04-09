using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration
{
    public class OnOriginChange : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private UnityEvent OnOriginChanged = new UnityEvent();

        private void OnEnable()
        {
            configuration.OnOriginChanged.AddListener(OnNewOrigin);
        }

        private void OnDisable()
        {
            configuration.OnOriginChanged.RemoveListener(OnNewOrigin);
        }

        private void OnNewOrigin(Coordinate origin)
        {
            OnOriginChanged.Invoke();
        }
    }
}
