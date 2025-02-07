using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Netherlands3D.Twin.ExtensionMethods;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.OGC3DTiles
{
    public class Tile3DNodeTogglePropertySection : MonoBehaviour
    {
        [SerializeField] private RectTransform togglePrefab;
        [SerializeField] private RectTransform togglePanel;
        private Tile3DLayerGameObject controller;
        private Dictionary<string, HashSet<GameObject>> nodeDictionary;
        private List<Toggle> toggles = new();

        public Tile3DLayerGameObject Controller
        {
            get { return controller; }
            set
            {
                
                controller = value;
                nodeDictionary = GetNodes(controller.transform);
                SpawnToggles(nodeDictionary.Keys);
            }
        }
        
        private void RefreshDictionary()
        {
            nodeDictionary = GetNodes(controller.transform);
        }

        private void SpawnToggles(IEnumerable<string> nodeNames)
        {
            foreach (var nodeName in nodeNames)
            {
                var nodeToggle = Instantiate(togglePrefab.gameObject, togglePanel);
                nodeToggle.name = nodeName;
                nodeToggle.GetComponentInChildren<TextMeshProUGUI>().text = nodeName;
                var toggle = nodeToggle.GetComponentInChildren<Toggle>();
                toggles.Add(toggle);
                toggle.onValueChanged.AddListener(isOn => ToggleNodes(nodeName, isOn));
            }

            var h = Mathf.CeilToInt((float)nodeNames.Count() / 2f);
            togglePanel.SetHeight(togglePrefab.sizeDelta.y * h);
            togglePanel.sizeDelta = new Vector2(togglePanel.sizeDelta.x, togglePrefab.sizeDelta.y * h);
        }


        private void ToggleNodes(string nodeName, bool isOn)
        {
            RefreshDictionary();
            if (nodeDictionary.TryGetValue(nodeName, out var nodes))
            {
                foreach (var node in nodes)
                {
                    node.SetActive(isOn);
                }
            }
        }

        private static Dictionary<string, HashSet<GameObject>> GetNodes(Transform transform)
        {
            Dictionary<string, HashSet<GameObject>> nodeDict = new Dictionary<string, HashSet<GameObject>>();
            Regex regex = new Regex("^Node-(\\d+)$");
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(transform);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();
                foreach (Transform child in current)
                {
                    Match match = regex.Match(child.name);
                    if (match.Success)
                    {
                        string key = match.Value;
                        if (!nodeDict.ContainsKey(key))
                        {
                            nodeDict[key] = new HashSet<GameObject>();
                        }

                        nodeDict[key].Add(child.gameObject);
                    }

                    queue.Enqueue(child);
                }
            }

            return nodeDict;
        }


        private void OnDestroy()
        {
            // if (legendToggle != null && controller != null)
            //     legendToggle.onValueChanged.RemoveListener(controller.SetLegendActive);
        }
    }
}