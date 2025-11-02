using System;
using System.Collections.Generic;
using UnityEngine;

namespace Route24.Core
{
    /// <summary>
    /// Type-safe, centralized event dispatcher for global, decoupled communication.
    /// Uses event structs/classes instead of string keys.
    /// </summary>
    public class EventHub : MonoBehaviour, IService, IInitializable
    {
        public int InitializationPriority => 10;
        
        private static readonly Dictionary<Type, Delegate> _eventTable = new();

        public void Initialize()
        {
            // Do nothing
        }

        /// <summary>
        /// Subscribe to a specific event type.
        /// </summary>
        public void Subscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (_eventTable.TryGetValue(type, out var existingDelegate))
                _eventTable[type] = Delegate.Combine(existingDelegate, listener);
            else
                _eventTable[type] = listener;
        }

        /// <summary>
        /// Unsubscribe from a specific event type.
        /// </summary>
        public void Unsubscribe<T>(Action<T> listener)
        {
            var type = typeof(T);
            if (_eventTable.TryGetValue(type, out var existingDelegate))
            {
                var newDelegate = Delegate.Remove(existingDelegate, listener);
                if (newDelegate == null)
                    _eventTable.Remove(type);
                else
                    _eventTable[type] = newDelegate;
            }
        }

        /// <summary>
        /// Publish an event instance to all listeners of that type.
        /// </summary>
        public void Publish<T>(T eventData)
        {
            var type = typeof(T);
            if (_eventTable.TryGetValue(type, out var callback))
            {
                if (callback is Action<T> action)
                    action.Invoke(eventData);
            }
            else
            {
                Debug.Log($"[EventHub] No listeners for event type: {type.Name}");
            }
        }

        /// <summary>
        /// Clears all registered listeners (used on cleanup or scene reloads).
        /// </summary>
        public void Clear() => _eventTable.Clear();
    }
}
