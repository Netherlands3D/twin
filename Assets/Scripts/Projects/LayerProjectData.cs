using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public class LayerProjectData
    {
        [SerializeField] private string name;
        [SerializeField] private  bool isActive = true;
        [SerializeField] private  LayerProjectData parent;
        [SerializeField] private  List<LayerProjectData> children = new();

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
        
        // public void SetIsActive(bool active)
        // {
        //     isActive = active;
        // }
        //
        // public bool GetIsActive()
        // {
        //     return isActive;
        // }

        public void SetParent(LayerProjectData newParent, int siblingIndex)
        {
            if (newParent == null)
                newParent = rootLayer;
            
            if (parent != null)
                parent.children.Remove(this);

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;
                    
            parent = newParent;
            newParent.children.Insert(siblingIndex,this);
        }

        // public string GetName()
        // {
        //     return name;
        // }
        //
        // public void SetName(string newName)
        // {
        //     name = newName;
        // }

        public void Initialize(LayerProjectData root, int siblingIndex)
        {
            rootLayer = root;
            SetParent(root, siblingIndex);
        }
    }
    
}
