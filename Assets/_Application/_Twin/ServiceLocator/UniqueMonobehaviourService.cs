using System;
using UnityEngine;

namespace Netherlands3D.Services
{
    public class UniqueMonobehaviourService : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour service;

        private void Awake()
        {
            if (service == null)
            {
                Debug.LogError($"the service on {gameObject.name} is null.");
                Destroy(this);
                return;
            }
            ServiceLocator.RegisterService(service);
        }

        private void OnDestroy()
        {
            if (service == null)
            {
                Debug.LogError($"the service on {gameObject.name} is null.");
                return;
            }
            ServiceLocator.UnRegisterService(service);
        }
    }
}
