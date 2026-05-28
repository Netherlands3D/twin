using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Tools;
using UnityEngine;

namespace Netherlands3D
{
    public enum ToolType
    {
        Layer,
        AssetImport,
        AssetLibrary,
        Search,
        SunPosition,
        DownloadTile,
        OpenProject,
        SaveProject,
        Settings,
        Help
    }
    
    public class ToolService : MonoBehaviour
    {
        [Serializable]
        public struct ToolEntry
        {
            public ToolType type;
            public Tool tool;
        }
        
        public Tool GetTool(ToolType type) => toolMap[type];
        
        [SerializeField] private List<ToolEntry> tools;

        private Dictionary<ToolType, Tool> toolMap;

        private void Awake()
        {
            toolMap = new Dictionary<ToolType, Tool>();
            foreach (var entry in tools)
                toolMap[entry.type] = entry.tool;
        }
    }
    
    [Serializable]
    public class ToolDictionary : SerializableDictionary<ToolType, Tool> { }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new();
        [SerializeField] private List<TValue> values = new();

        private Dictionary<TKey, TValue> dictionary = new();

        //hooks into unity pipeline when about to write to disk
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        //hooks into unity pipeline when data is read from disk
        public void OnAfterDeserialize()
        {
            dictionary.Clear();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
                dictionary[keys[i]] = values[i];
        }
    }
}
