using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
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

        private ProjectData project;

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

        public LayerProjectData Parent => parent;
        public IReadOnlyCollection<LayerProjectData> Children => children;

        public void Initialize(ProjectData projectData, int siblingIndex)
        {
            project = projectData;
            SetParent(projectData.rootLayer, siblingIndex);
        }

        public void SetParent(LayerProjectData newParent, int siblingIndex)
        {
            Debug.Log("setting parent of: " + Name + " to: " + newParent?.Name);
            
            if (newParent == null)
                newParent = project.rootLayer;

            if (parent != null)
                parent.children.Remove(this);

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;

            parent = newParent;
            Debug.Log("new parent: " + newParent);
            Debug.Log("children: " + children);
            newParent.children.Insert(siblingIndex, this);
        }
    }
}