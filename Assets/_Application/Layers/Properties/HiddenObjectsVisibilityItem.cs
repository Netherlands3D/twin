using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsVisibilityItem : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text bagId;

        [SerializeField] private Sprite visible;
        [SerializeField] private Sprite invisible;

        public UnityEvent<bool> ToggleVisibility = new();
        public LayerFeature LayerFeature => feature;

        private Image image;
        private LayerFeature feature;

        private void Awake()
        {
            toggle.onValueChanged.AddListener(OnToggle);
            image = toggle.targetGraphic.GetComponent<Image>();
        }

        void OnToggle(bool isOn)
        {   
            ToggleVisibility.Invoke(isOn);
            UpdateGraphic();
        }

        public void SetToggleState(bool isOn)
        {      
            toggle.isOn = isOn;
        }

        private void UpdateGraphic()
        {
            image.sprite = toggle.isOn ? visible : invisible;
        }

        void OnDestroy()
        {
            toggle.onValueChanged.RemoveListener(OnToggle);
        }

        public void SetLayerFeature(LayerFeature feature)
        {
            this.feature = feature;
        }

        public void SetBagId(string id)
        {
            bagId.text = id;
        }
    }
}
