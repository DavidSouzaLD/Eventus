using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eventus.Core
{
    public static class Evn2
    {
        private static readonly Dictionary<Type, object> DataHub = new();
        private static readonly Dictionary<Type, IEventBinding> EventBindings = new();

        #region Blackboard

        public static void Write<TChannel, TValue>(TValue value) where TChannel : Channel
        {
            DataHub[typeof(TChannel)] = value;
        }

        public static TValue Read<TChannel, TValue>() where TChannel : Channel
        {
            if (DataHub.TryGetValue(typeof(TChannel), out var value))
                if (value is TValue typedValue)
                    return typedValue;

            Debug.LogWarning($"Data for key '{typeof(TChannel).Name}' not found or with incorrect type. Returning default value.");
            return default;
        }

        #endregion

        #region Events

        public static void Publish<TChannel>() where TChannel : Channel
        {
            if (!EventBindings.TryGetValue(typeof(TChannel), out var binding)) return;
            if (binding is EventBinding simpleBinding) simpleBinding.Invoke();
        }

        public static void Publish<TChannel, TData>(TData data) where TChannel : Channel
        {
            if (!EventBindings.TryGetValue(typeof(TChannel), out var binding)) return;
            if (binding is EventBinding<TData> typedBinding) typedBinding.Invoke(data);
        }

        public static void Subscribe<TChannel>(Action listener) where TChannel : Channel
        {
            var key = typeof(TChannel);

            if (!EventBindings.TryGetValue(key, out var binding))
            {
                binding = new EventBinding();
                EventBindings[key] = binding;
            }

            if (binding is EventBinding simpleBinding) simpleBinding.AddListener(listener);
        }

        public static void Subscribe<TChannel, TData>(Action<TData> listener) where TChannel : Channel
        {
            var key = typeof(TChannel);

            if (!EventBindings.TryGetValue(key, out var binding))
            {
                binding = new EventBinding<TData>();
                EventBindings[key] = binding;
            }

            if (binding is EventBinding<TData> typedBinding) typedBinding.AddListener(listener);
        }

        public static void Unsubscribe<TChannel>(Action listener) where TChannel : Channel
        {
            if (!EventBindings.TryGetValue(typeof(TChannel), out var binding)) return;
            if (binding is EventBinding simpleBinding) simpleBinding.RemoveListener(listener);
        }

        public static void Unsubscribe<TChannel, TData>(Action<TData> listener) where TChannel : Channel
        {
            if (!EventBindings.TryGetValue(typeof(TChannel), out var binding)) return;
            if (binding is EventBinding<TData> typedBinding) typedBinding.RemoveListener(listener);
        }

        #endregion

        #region Bindings

        private interface IEventBinding { }

        private class EventBinding : IEventBinding
        {
            private event Action OnEvent = delegate { };

            public void AddListener(Action listener) => OnEvent += listener;
            public void RemoveListener(Action listener) => OnEvent -= listener;
            public void Invoke() => OnEvent.Invoke();
        }

        private class EventBinding<T> : IEventBinding
        {
            private event Action<T> OnEvent = delegate { };

            public void AddListener(Action<T> listener) => OnEvent += listener;
            public void RemoveListener(Action<T> listener) => OnEvent -= listener;
            public void Invoke(T data) => OnEvent.Invoke(data);
        }

        #endregion
    }
}