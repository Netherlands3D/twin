using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Functionalities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class FunctionalitiesPane : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject watermark;

        [Tooltip("The text that is shown when the selected functionality description is empty")]
        [TextArea(3, 10)]
        [SerializeField] private string placeHolderText;
        [SerializeField] private ScrollRect scrollRectText;
        
        void Start()
        {
            descriptionText.text = placeHolderText;
        }

        public void ShowInformation(Functionality functionality)
        {
            var hasDescription =  !string.IsNullOrEmpty(functionality.Description);

            descriptionText.text = (hasDescription) ? functionality.Description : placeHolderText;
            watermark.SetActive(!hasDescription);

            //Scale text to wrap around its content
            descriptionText.rectTransform.sizeDelta = new Vector2(descriptionText.rectTransform.sizeDelta.x, descriptionText.preferredHeight);
            
            //Reset entire scrollview and momentum back to top
            scrollRectText.velocity = Vector2.zero;
            scrollRectText.normalizedPosition = new Vector2(0, 1);
        }

        private void OnValidate() {
            if(descriptionText != null && placeHolderText != null && descriptionText.text != placeHolderText) {
                descriptionText.text = placeHolderText;
            }
        }
    }
}
