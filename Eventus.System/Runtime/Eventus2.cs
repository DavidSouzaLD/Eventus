using System;
using System.Collections.Generic;
using UnityEngine;
using Eventus.Runtime.Core;

namespace Eventus.Runtime
{
    public static class Eventus2
    {
        private static readonly Dictionary<Channel, object> _dataHub = new();
        private static readonly Dictionary<Channel, IEventBinding> _eventBindings = new();

        #region Blackboard

        public static void Write<T>(Channel dataKey, T value)
        {
            _dataHub[dataKey] = value;
        }

        public static T Read<T>(Channel dataKey)
        {
            if (_dataHub.TryGetValue(dataKey, out var value))
                if (value is T typedValue)
                    return typedValue;

            Debug.LogWarning($"Data for key '{dataKey}' not found or with incorrect type. Returning default value.");
            return default;
        }

        #endregion

        #region Events

        public static void Publish(Channel type)
        {
            if (!_eventBindings.TryGetValue(type, out var binding)) return;
            if (binding is EventBinding simpleBinding) simpleBinding.Invoke();
        }

        public static void Publish<T>(Channel type, T data)
        {
            if (!_eventBindings.TryGetValue(type, out var binding)) return;
            if (binding is EventBinding<T> typedBinding) typedBinding.Invoke(data);
        }

        public static void Subscribe(Channel type, Action listener)
        {
            if (!_eventBindings.TryGetValue(type, out var binding))
            {
                binding = new EventBinding();
                _eventBindings[type] = binding;
            }

            if (binding is EventBinding simpleBinding) simpleBinding.AddListener(listener);
        }

        public static void Subscribe<T>(Channel type, Action<T> listener)
        {
            if (!_eventBindings.TryGetValue(type, out var binding))
            {
                binding = new EventBinding<T>();
                _eventBindings[type] = binding;
            }

            if (binding is EventBinding<T> typedBinding) typedBinding.AddListener(listener);
        }

        public static void Unsubscribe(Channel type, Action listener)
        {
            if (!_eventBindings.TryGetValue(type, out var binding)) return;
            if (binding is EventBinding simpleBinding) simpleBinding.RemoveListener(listener);
        }

        public static void Unsubscribe<T>(Channel type, Action<T> listener)
        {
            if (!_eventBindings.TryGetValue(type, out var binding)) return;
            if (binding is EventBinding<T> typedBinding) typedBinding.RemoveListener(listener);
        }

        internal interface IEventBinding
        {
        }

        internal class EventBinding : IEventBinding
        {
            private event Action OnEvent = delegate { };

            public void AddListener(Action listener)
            {
                OnEvent += listener;
            }

            public void RemoveListener(Action listener)
            {
                OnEvent -= listener;
            }

            public void Invoke()
            {
                OnEvent.Invoke();
            }
        }

        internal class EventBinding<T> : IEventBinding
        {
            private event Action<T> OnEvent = delegate { };

            public void AddListener(Action<T> listener)
            {
                OnEvent += listener;
            }

            public void RemoveListener(Action<T> listener)
            {
                OnEvent -= listener;
            }

            public void Invoke(T data)
            {
                OnEvent.Invoke(data);
            }
        }

        #endregion
    }
}