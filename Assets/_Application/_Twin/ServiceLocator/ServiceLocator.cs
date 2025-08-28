using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<string, MonoBehaviour> namedServices = new();
        private static readonly Dictionary<Type, MonoBehaviour> registeredServices = new();

        public static void RegisterService(MonoBehaviour service)
        {
            Debug.Log("Registering service: " + service.GetType(), service);
            registeredServices.Add(service.GetType(), service);
        }

        public static void RegisterService(string serviceName, MonoBehaviour service)
        {
            Debug.Log("Registering named service: " + serviceName, service);
            namedServices.Add(serviceName, service);
        }

        public static void UnregisterService(MonoBehaviour service)
        {
            Debug.Log("Unregistering service: " + service.GetType(), service);
            registeredServices.Remove(service.GetType());
        }
     
        public static void UnregisterService(string serviceName)
        {
            Debug.Log("Unregistering named service: " + serviceName);
            namedServices.Remove(serviceName);
        }
     
        public static T GetService<T>() where T : MonoBehaviour
        {
            registeredServices.TryGetValue(typeof(T), out var service);
            
            return service as T;
        }

        public static T GetService<T>(string serviceName) where T : MonoBehaviour
        {
            namedServices.TryGetValue(serviceName, out var service);
            
            return service as T;
        }
        public static MonoBehaviour GetService(string serviceName)
        {
            if (!namedServices.TryGetValue(serviceName, out var service))
            {
                return null;
            }

            return service;
        }
    }
}
