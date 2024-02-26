using System.Collections;
using System.Collections.Generic;
using SLIDDES.UI;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ScaleWithInspector : MonoBehaviour
    {
        [SerializeField] private RectTransform inspector;
        
        void Update()
        {
            var rt = transform as RectTransform;
            rt.SetLeft(inspector.anchoredPosition.x + inspector.sizeDelta.x);
        }
    }
}
