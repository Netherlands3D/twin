using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ZoneTrigger : MonoBehaviour
    {
        public delegate void ZoneHandler(Collider col, ZoneTrigger zone);
        public event ZoneHandler OnEnter;
        public event ZoneHandler OnExit;

        private void OnTriggerEnter(Collider other)
        {
            OnEnter?.Invoke(other, this);
        }

        private void OnTriggerExit(Collider other)
        {
            OnExit?.Invoke(other, this);
        }
    }
}
