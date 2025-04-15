using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.UI.ColorPicker;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class LayerColorPropertySection : PropertySectionWithLayerGameObject
    {
        private LayerGameObject layer;
        [SerializeField] private RectTransform content;

        private void Start()
        {
            StartCoroutine(WaitFrame());
        }

        private IEnumerator WaitFrame()
        {
            yield return new WaitForEndOfFrame(); 
            LayoutElement layout = GetComponent<LayoutElement>();
            layout.minHeight = content.rect.height;
        }

        public override LayerGameObject LayerGameObject
        {
            get => layer;
            set
            {
                layer = value;
                //colorPicker.SetColorWithoutNotify(layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.GetFillColor() ?? defaultColor);
            }
        }

        private void OnEnable()
        {
            //colorPicker.colorChanged.AddListener(OnPickedColor);
        }

        private void OnDisable()
        {
            //colorPicker.colorChanged.RemoveListener(OnPickedColor);
        }

        public void OnPickedColor(Color color)
        {
            layer.LayerData.DefaultStyle.AnyFeature.Symbolizer.SetFillColor(color);
            layer.ApplyStyling();
        }
    }
}