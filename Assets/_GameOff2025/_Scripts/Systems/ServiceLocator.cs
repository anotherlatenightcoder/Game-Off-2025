using System;
using System.Collections.Generic;
using UnityEngine;

namespace Route24.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Registers a service in the dictionary,
        /// replaces if already existing
        /// </summary>
        /// <param name="service"></param>
        /// <typeparam name="T"></typeparam>
        public static void Register<T>(T service)
        {
            Register(typeof(T), service);
        }

        public static void Register(Type type, object service)
        {
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} already registered, overwriting.");
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
            }
        }

        /// <summary>
        /// Checks if the dictionary contains a type of service,
        /// if so, returns it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Get<T>()
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
            {
                return (T)service;
            }
            
            Debug.LogError($"[ServiceLocator] No service of type {type.Name} registered.");
            return default;
        }

        /// <summary>
        /// Unregisters a service by type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Unregister<T>()
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
        }

        /// <summary>
        /// Clears all registered services
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }
    }   
}
