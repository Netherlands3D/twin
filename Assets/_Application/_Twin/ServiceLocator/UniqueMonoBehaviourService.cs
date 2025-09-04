using System;
using UnityEngine;

namespace Netherlands3D.Services
{
    public class UniqueMonoBehaviourService : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour service;

        private void Awake()
        {
            ServiceLocator.RegisterService(service);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService(service);
        }
    }
}
