using Netherlands3D.Twin.ExtensionMethods;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ScaleWithInspector : MonoBehaviour
    {
        [SerializeField] private RectTransform inspector;
        private RectTransform rt;
        private float offsetLeft;

        private void Awake()
        {
            rt = transform as RectTransform;
            offsetLeft = rt.offsetMin.x;
        }

        private void Update()
        {
            var inspectorLeft = Mathf.Clamp(inspector.anchoredPosition.x + inspector.sizeDelta.x, 0, float.MaxValue);
            rt.SetLeft(offsetLeft + inspectorLeft);
        }
    }
}
