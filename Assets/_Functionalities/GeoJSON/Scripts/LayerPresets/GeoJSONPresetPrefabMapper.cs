using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "GeoJSONPresetPrefabMapper", menuName = "ScriptableObjects/GeoJSONPresets/GeoJSONPresetPrefabMapper", order = 1)]
    public class GeoJSONPresetPrefabMapper : ScriptableObject
    {
        [Serializable]
        public class Mapping
        {
            public string Type;
            public string PrefabIdentifier;
        }

        [SerializeField]
        private List<Mapping> mappings = new List<Mapping>();

        public bool TryGetPrefabIdentifier(string type, out string prefabIdentifier)
        {
            foreach (var mapping in mappings)
            {
                if (string.Equals(mapping.Type, type, StringComparison.OrdinalIgnoreCase))
                {
                    prefabIdentifier = mapping.PrefabIdentifier;
                    return true;
                }
            }

            prefabIdentifier = null;
            return false;
        }
    }
}