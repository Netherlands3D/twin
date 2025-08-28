using UnityEngine;

namespace Netherlands3D.Services
{
    public class NamedMonoBehaviourService : MonoBehaviour
    {
        [SerializeField] private string serviceName;
        [Tooltip("Service to register with the service locator, if none will register this service injector")]
        [SerializeField] private MonoBehaviour service;

        private void Awake()
        {
            if (!service)
            {
                service = this;
            }

            ServiceLocator.RegisterService(serviceName, service);
        }

        private void OnDestroy()
        {
            ServiceLocator.UnregisterService(serviceName);
        }
    }
}
