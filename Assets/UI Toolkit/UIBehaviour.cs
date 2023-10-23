using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    [Serializable]
    public class UIElementInHierarchy
    {
        public VisualElement visualElement;
        public Transform transform;

        public UIElementInHierarchy(VisualElement visualElement, Transform transform, Transform parent = null)
        {
            this.visualElement = visualElement;
            this.transform = transform;
            transform.SetParent(parent);
        }
    }
    
    public class UIBehaviour: MonoBehaviour
    {
        public static List<UIElementInHierarchy> ElementsInHierarchy = new List<UIElementInHierarchy>();
        
        public static void CreateElement(VisualTreeAsset elementTemplate, UIElementInHierarchy parent, MonoBehaviour behaviour = null)
        {
            var element = elementTemplate.Instantiate();
            parent.visualElement.Add(element);
            var component = new GameObject().AddComponent(behaviour.GetType());
            var newElement = new UIElementInHierarchy(element, component.gameObject.transform, parent.transform);
            ElementsInHierarchy.Add(newElement);
        }
        
        
    }
}