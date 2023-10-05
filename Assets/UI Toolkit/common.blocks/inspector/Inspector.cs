using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class Inspector : MonoBehaviour
    {
        private const string INSPECTOR_ID = "#Inspector";

        private VisualElement rootVisualElement;
        private VisualElement inspector;

        private void Start()
        {
            rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            inspector = rootVisualElement.Q<VisualElement>(INSPECTOR_ID);
        }

        public void Open()
        {
            inspector.AddToClassList("inspector--open");
        }

        public void Close()
        {
            inspector.RemoveFromClassList("inspector--open");
        }
    }
}
