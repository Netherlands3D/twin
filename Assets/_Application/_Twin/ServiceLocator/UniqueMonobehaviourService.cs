using System;
using UnityEngine;

namespace Netherlands3D
{
    public class UniqueMonobehaviourService : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour service;

        private void Awake()
        {
            ServiceLocator.RegisterService(service);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnRegisterService(service);
        }
    }
}
