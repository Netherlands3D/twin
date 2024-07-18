using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Twin
{
    [Serializable]
    public class PrefabEntry
    {
        public string id;
        public GameObject prefab;
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Twin/PrefabLibrary", fileName = "PrefabLibrary", order = 0)]
    public class PrefabLibrary : ScriptableObject
    {
        // [SerializeField] private Dictionary<int, GameObject> prefabs;
        public List<PrefabEntry> prefabEntries;
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            MatchPrefabIds();
        }

        private void MatchPrefabIds()
        {
            //check if prefab id's still match meta ids, or set it to these ids
            foreach (var prefabEntry in prefabEntries)
            {
                if (prefabEntry.prefab == null)
                {
                    continue;
                }

                var pathToPrefabAsset = AssetDatabase.GUIDToAssetPath(prefabEntry.id);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pathToPrefabAsset);

                if (prefab == prefabEntry.prefab)
                {
                    Debug.Log(prefabEntry.prefab.name + " has a correct id");
                }
                else
                {
                    var pathToPrefab = AssetDatabase.GetAssetPath(prefabEntry.prefab);
                    var metaID = AssetDatabase.GUIDFromAssetPath(pathToPrefab);
                    prefabEntry.id = metaID.ToString();
                    Debug.Log(prefabEntry.prefab.name + " has incorrect id, setting id to: " + prefabEntry.id);
                }
            }
        }
#endif

        public GameObject GetPrefabById(string id)
        {
            foreach (var entry in prefabEntries)
            {
                if (entry.id == id)
                {
                    return entry.prefab;
                }
            }

            return null;
        }
    }
}