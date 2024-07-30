using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin
{
    [Serializable]
    public class PrefabGroup
    {
        public string groupName;
        public List<ReferencedLayer> prefabs;
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Twin/PrefabLibrary", fileName = "PrefabLibrary", order = 0)]
    public class PrefabLibrary : ScriptableObject
    {
        [JsonIgnore, SerializeField] private ReferencedLayer fallbackPrefab;
        [JsonIgnore] public List<PrefabGroup> prefabGroups;

        public ReferencedLayer GetPrefabById(string id)
        {
            foreach (var group in prefabGroups)
            {
                foreach (var prefab in group.prefabs)
                {
                    if (prefab.PrefabIdentifier == id)
                    {
                        return prefab;
                    }
                }
            }

            return fallbackPrefab;
        }
    }
}