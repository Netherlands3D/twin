using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.HierarchyInspector
{
    public class LayerUICredentialsNeededListener : MonoBehaviour
    {
        public LayerUI layerUI { get; set; }
        
        [SerializeField] private GameObject visibilityToggle;
        [SerializeField] private GameObject warningIcon;
        [SerializeField] private GameObject colorButton;
        [SerializeField] private GameObject errorColorButton;
        [SerializeField] private GameObject propertiesToggle;
        [SerializeField] private GameObject errorProperties;

        [SerializeField] private TMP_Text layerNameText;
        private Color originalTextColor;

        [SerializeField] private Image layerTypeImage;
        [SerializeField] private Image errorLayerTypeImage;
        
        [SerializeField] private Color errorColor = new Color(0.8f, 0.8431372549019608f, 0.8941176470588236f);
        
        private void Awake()
        {
            if(!layerUI)
                layerUI = GetComponent<LayerUI>();
        }

        private void Start()
        {
            originalTextColor = layerNameText.color;
            SetUI(layerUI.Layer.HasValidCredentials); //set initial state
            layerUI.Layer.HasValidCredentialsChanged.AddListener(SetUI);
        }

        private void SetUI(bool hasValidCredentials)
        {
            visibilityToggle.SetActive(hasValidCredentials);
            warningIcon.SetActive(!hasValidCredentials);
            colorButton.SetActive(hasValidCredentials);
            errorColorButton.SetActive(!hasValidCredentials);
            propertiesToggle.SetActive(hasValidCredentials);
            errorProperties.SetActive(!hasValidCredentials);
            
            layerNameText.color = hasValidCredentials ? originalTextColor : errorColor;

            layerTypeImage.gameObject.SetActive(hasValidCredentials);
            errorLayerTypeImage.gameObject.SetActive(!hasValidCredentials);
        }

        private void OnDestroy()
        {
            layerUI.Layer.HasValidCredentialsChanged.RemoveListener(SetUI);
        }
    }
}