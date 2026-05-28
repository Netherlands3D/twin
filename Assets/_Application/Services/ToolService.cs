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
        
        [SerializeField] private List<ToolEntry> tools;

        private Dictionary<ToolType, Tool> _toolMap;

        private void Awake()
        {
            _toolMap = new Dictionary<ToolType, Tool>();
            foreach (var entry in tools)
                _toolMap[entry.type] = entry.tool;
        }

        public Tool GetTool(ToolType type) => _toolMap[type];

        public bool TryGetTool(ToolType type, out Tool tool) => _toolMap.TryGetValue(type, out tool);
    }
    
    [Serializable]
    public class ToolDictionary : SerializableDictionary<ToolType, Tool> { }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys = new();
        [SerializeField] private List<TValue> values = new();

        private Dictionary<TKey, TValue> _dict = new();

        public TValue this[TKey key] => _dict[key];

        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        //hooks into unity pipeline when about to write to disk
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var kvp in _dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        //hooks into unity pipeline when data is read from disk
        public void OnAfterDeserialize()
        {
            _dict.Clear();
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
                _dict[keys[i]] = values[i];
        }
    }
}
