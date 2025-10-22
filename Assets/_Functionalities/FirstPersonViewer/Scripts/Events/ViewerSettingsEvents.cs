using Netherlands3D.FirstPersonViewer.ViewModus;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Events
{
    public static class ViewerSettingsEvents<T>
    {
        private static Dictionary<MovementLabel, Action<T>> allEventReactions = new Dictionary<MovementLabel, Action<T>>();

        public static void Invoke(MovementLabel settingsType, T arg)
        {
            if (!allEventReactions.ContainsKey(settingsType))
            {
                Debug.LogWarning($"Event reactions does not contain: {settingsType}");
                return;
            }

            allEventReactions[settingsType]?.Invoke(arg);
        }

        public static void AddListener(MovementLabel settingsType, Action<T> function)
        {
            if (!allEventReactions.ContainsKey(settingsType))
            {
                allEventReactions.Add(settingsType, null);
            }

            allEventReactions[settingsType] += function;
        }

        public static void RemoveListener(MovementLabel settingsType, Action<T> function)
        {
            if (!allEventReactions.ContainsKey(settingsType))
            {
                Debug.LogWarning($"Event reactions does not contain: {settingsType}");
                return;
            }

            if (allEventReactions[settingsType] == null)
            {
                Debug.LogWarning($"This game event type is null: {settingsType}");
                return;
            }

            allEventReactions[settingsType] -= function;
        }
    }
}
