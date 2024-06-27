using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Projects
{
    [Serializable]
    public class LayerProjectData
    {
        public string Name;
        public bool isActive;// { get; set; }
        public LayerProjectData parent;// { get; private set; }
        public List<LayerProjectData> children = new();

        // public void SetIsActive(bool active)
        // {
        //     isActive = active;
        //     //event.invoke
        // }
        //
        // public bool GetIsActive()
        // {
        //     return isActive;
        // }

        public void SetParent(LayerProjectData newParent, int siblingIndex)
        {

            if (parent != null)
                parent.children.Remove(this);

            if (siblingIndex < 0)
                siblingIndex = newParent.children.Count;
                    
            parent = newParent;
            newParent.children.Insert(siblingIndex,this);
        }
    }
    
}
