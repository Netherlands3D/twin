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
            Debug.Log("Registering service: " + service.GetType(), service);
            registeredServices.Add(service.GetType(), service);
        }

        public static void UnregisterService(MonoBehaviour service)
        {
            Debug.Log("Unregistering service: " + service.GetType(), service);
            registeredServices.Remove(service.GetType());
        }

        public static T GetService<T>() where T : MonoBehaviour
        {
            registeredServices.TryGetValue(typeof(T), out var service);
            
            return service as T;
        }
    }
}
