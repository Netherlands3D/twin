using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, MonoBehaviour> registeredServices = new();

        public static void RegisterService(MonoBehaviour service)
        {
            if (service == null)
            {
                Debug.LogError("Service is null");
                return;
            }
            
            Debug.Log("Registering service: " + service.GetType(), service);
            registeredServices.Add(service.GetType(), service);
        }

        public static void UnRegisterService(MonoBehaviour service)
        {
            if (service == null)
            {
                Debug.LogError("Service is null");
                return;
            }
            
            Debug.Log("Unregistering service: " + service.GetType(), service);
            registeredServices.Remove(service.GetType());
        }
     
        public static T GetService<T>() where T : MonoBehaviour
        {
            MonoBehaviour service;
            registeredServices.TryGetValue(typeof(T), out service);
            return service as T;
        }

    }
}
