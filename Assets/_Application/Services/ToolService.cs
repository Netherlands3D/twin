using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D
{
    public enum ToolType
    {
        Layer,
        AssetLibrary,
        AssetImport,
        Search,
        SunPosition,
        DownloadTile,
        OpenProject,
        SaveProject,
        Settings,
        Help,
        Dome,
        None
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

        private Dictionary<ToolType, Tool> toolMap;
        private Dictionary<ToolType, Tool> ToolMap => toolMap ??= BuildToolMap();

        private Dictionary<ToolType, Tool> BuildToolMap()
        {
            toolMap = new Dictionary<ToolType, Tool>();
            foreach (var entry in tools)
                toolMap[entry.type] = entry.tool;
            return toolMap;
        }

        public Tool GetTool(ToolType type) => ToolMap[type];
        
        public UnityEvent AnyToolOpened;
        public UnityEvent AnyToolClosed;

        private void OnEnable()
        {
            foreach (var tool in tools)
            {
                tool.tool.onOpen.AddListener(AnyToolOpened.Invoke);
                tool.tool.onClose.AddListener(AnyToolClosed.Invoke); 
            }
        }

        private void OnDisable()
        {
            foreach (var tool in tools)
            {
                tool.tool.onOpen.RemoveListener(AnyToolOpened.Invoke);
                tool.tool.onClose.RemoveListener(AnyToolClosed.Invoke); 
            }
        }

        public void AddOpenListener(ToolType type, UnityAction listener)
        {
            GetTool(type)?.onOpen.AddListener(listener);
        }

        public void RemoveOpenListener(ToolType type, UnityAction listener)
        {
            GetTool(type)?.onOpen.RemoveListener(listener);
        }

        public void CloseAllTools()
        {
            foreach (var toolEntry in tools)
                toolEntry.tool.Close();
        }

        public void CloseAllToolsWithPanel()
        {
            foreach (var tool in GetAllToolsWithPanel())
            {
                tool.Close();
            }
        }

        public List<Tool> GetAllToolsWithPanel()
        {
            List<Tool> toolsWithPanel = new List<Tool>();
            foreach (var tool in tools){
                if (tool.tool.PanelType != null)
                {
                    toolsWithPanel.Add(tool.tool);
                }
            }

            return toolsWithPanel;
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
