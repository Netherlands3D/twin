using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration
{
    public class OnOriginChange : MonoBehaviour
    {
        [SerializeField] private UnityEvent OnOriginChanged = new();
        private Origin origin;

        private void Start()
        {
            origin = FindObjectOfType<Origin>();
        }

        private void OnEnable()
        {
            origin.onShiftOriginTo.AddListener(OnNewOrigin);
        }

        private void OnDisable()
        {
            origin.onShiftOriginTo.RemoveListener(OnNewOrigin);
        }

        private void OnNewOrigin(Coordinate from, Coordinate to)
        {
            OnOriginChanged.Invoke();
        }
    }
}
