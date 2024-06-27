using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public class LayerProjectData
    {
        [SerializeField, JsonProperty] private string name;
        [SerializeField, JsonProperty] private bool isActive = true;
        [SerializeField, JsonProperty] private LayerProjectData parent;
        [SerializeField, JsonProperty] private List<LayerProjectData> children = new();

        private LayerProjectData rootLayer; //todo make static?

        public string Name
        {
            get => name;
            set => name = value;
        }

        public bool IsActive
        {
            get => isActive;
            set => isActive = value;
        }

        public void Initialize(LayerProjectData root, int siblingIndex)
        {
            rootLayer = root;
            SetParent(root, siblingIndex);
        }

        public void SetParent(LayerProjectData newParent, int siblingIndex)
        {
            if (newParent == null)
                newParent = rootLayer;

            if (parent != null)
                parent.children.Remove(this);

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;

            parent = newParent;
            newParent.children.Insert(siblingIndex, this);
        }

        public void LoadLayer()
        {
            Debug.LogError(children.Count);
            foreach (var child in children)
            {
                Debug.Log("Loading " + child.Name + "\t" + parent +"\t" + children.Count);
                child.LoadLayer();
            }
        }
    }
}