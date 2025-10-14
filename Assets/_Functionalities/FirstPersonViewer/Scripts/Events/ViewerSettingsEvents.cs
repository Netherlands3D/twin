using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Events
{
    public static class ViewerSettingsEvents<T>
    {
        private static Dictionary<string, System.Action<T>> m_AllEventReactions = new Dictionary<string, System.Action<T>>();

        public static void Invoke(string settingsType, T arg)
        {
            if (!m_AllEventReactions.ContainsKey(settingsType))
            {
                Debug.LogWarning($"Event reactions does not contain: {settingsType}");
                return;
            }

            m_AllEventReactions[settingsType]?.Invoke(arg);
        }

        public static void AddListener(string settingsType, System.Action<T> function)
        {
            if (!m_AllEventReactions.ContainsKey(settingsType))
            {
                m_AllEventReactions.Add(settingsType, null);
            }

            m_AllEventReactions[settingsType] += function;
        }

        public static void RemoveListener(string settingsType, System.Action<T> function)
        {
            if (!m_AllEventReactions.ContainsKey(settingsType))
            {
                Debug.LogWarning($"Event reactions does not contain: {settingsType}");
                return;
            }

            if (m_AllEventReactions[settingsType] == null)
            {
                Debug.LogWarning($"This game event type is null: {settingsType}");
                return;
            }

            m_AllEventReactions[settingsType] -= function;
        }
    }
}
