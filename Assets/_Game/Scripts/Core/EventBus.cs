using System;
using System.Collections.Generic;

namespace DeadDawn.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> subscribers = new Dictionary<Type, List<Delegate>>();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!subscribers.ContainsKey(type))
            {
                subscribers[type] = new List<Delegate>();
            }
            subscribers[type].Add(handler);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (subscribers.TryGetValue(type, out var list))
            {
                list.Remove(handler);
            }
        }

        public static void Publish<T>(T eventArgs)
        {
            var type = typeof(T);
            if (subscribers.TryGetValue(type, out var list))
            {
                var snapshot = list.ToArray();
                for (int i = 0; i < snapshot.Length; i++)
                {
                    ((Action<T>)snapshot[i])?.Invoke(eventArgs);
                }
            }
        }

        public static void Clear()
        {
            subscribers.Clear();
        }
    }
}
