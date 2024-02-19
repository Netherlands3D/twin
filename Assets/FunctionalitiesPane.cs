using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class FunctionalitiesPane : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject watermark;

        [Tooltip("The text that is shown when the selected functionality description is empty")]
        [TextArea(3, 10)]
        [SerializeField] private string placeHolderText;
        void Start()
        {
            descriptionText.text = placeHolderText;
        }

        private void OnValidate() {
            if(descriptionText != null && placeHolderText != null && descriptionText.text != placeHolderText) {
                descriptionText.text = placeHolderText;
            }
        }
    }
}
