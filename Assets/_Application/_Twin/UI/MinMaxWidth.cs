using UnityEngine;

namespace Netherlands3D.Twin.UI
{
    public class MinMaxWidth : MonoBehaviour
    {
        [SerializeField] float minWidth = 800.0f;
        [SerializeField] float maxWidth = 1130.0f;
        RectTransform rectTransform;
        private void Awake() {
            rectTransform = GetComponent<RectTransform>();
        }
        void Update()
        {
            var canvasScaledWidth = Screen.width/rectTransform.lossyScale.x;
            rectTransform.sizeDelta = new Vector2(Mathf.Clamp(canvasScaledWidth, minWidth, maxWidth), rectTransform.sizeDelta.y);
        }

        private void OnValidate() {
            //If minWidth is larger than maxWidth, increase maxWidth
            if(minWidth > maxWidth)
                maxWidth = minWidth;
        }
    }
}
