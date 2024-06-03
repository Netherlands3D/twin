using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


namespace Netherlands3D.Twin
{
    public class UIButtonLogic : MonoBehaviour
    {

        public Color BaseTextColor = Color.blue;
        public Color HighlightedTextColor = Color.white;
        public Color DisabledTextColor = Color.grey;
        public TextMeshProUGUI ButtonText;
        public GameObject Icon;

        // Start is called before the first frame update
        void Start()
        {
            if (ButtonText != null)
            {
                ButtonText.overrideColorTags = true;
                ButtonText.color = BaseTextColor;
            }
            if(Icon != null)
            {
                Icon.GetComponent<Image>().color = BaseTextColor;
            }
        }

        public void PointerDown()
        {
            if (ButtonText != null)
            {
                ButtonText.overrideColorTags = true;
                ButtonText.color = HighlightedTextColor;
            }
            if (Icon != null)
            {
                Icon.GetComponent<Image>().color = HighlightedTextColor;
            }
        }

        public void PointerUp()
        {
            if (ButtonText != null)
            {
                ButtonText.overrideColorTags = true;
                ButtonText.color = BaseTextColor;
            }
            if (Icon != null)
            {
                Icon.GetComponent<Image>().color = BaseTextColor;
            }
            DeselectClickedButton(this.gameObject);
            
        }

        private void DeselectClickedButton(GameObject button)
        {
            if (EventSystem.current.currentSelectedGameObject == button)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void ButtonState(bool Enabled )
        {
            if(Enabled == true)
            {
                this.GetComponent<Button>().interactable = true;
                {
                    ButtonText.overrideColorTags = true;
                    ButtonText.color = BaseTextColor;
                }
                if (Icon != null)
                {
                    Icon.GetComponent<Image>().color = BaseTextColor;
                }
            }

            if(Enabled == false)
            {
                this.GetComponent<Button>().interactable = false;
                {
                    ButtonText.overrideColorTags = true;
                    ButtonText.color = DisabledTextColor;
                }
                if (Icon != null)
                {
                    Icon.GetComponent<Image>().color = DisabledTextColor;
                }
            }
        }

    }
}
