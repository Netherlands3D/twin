using System;
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
        public Image Icon;

        // Start is called before the first frame update
        void Start()
        {

            if (this.GetComponent<Button>().interactable == true)
            {
                if (ButtonText != null)
                {
                    ButtonText.overrideColorTags = true;
                    ButtonText.color = BaseTextColor;
                }
                if (Icon != null)
                {
                    Icon.color = BaseTextColor;
                }
            }
            else
            {
                if (ButtonText != null)
                {
                    ButtonText.overrideColorTags = true;
                    ButtonText.color = DisabledTextColor;
                }
                if (Icon != null)
                {
                    Icon.color = DisabledTextColor;
                }
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
                Icon.color = HighlightedTextColor;
            }
        }

        public void PointerUp()
        {
            if(this.GetComponent<Button>().interactable == true)
            {
                if (ButtonText != null)
                {
                    ButtonText.overrideColorTags = true;
                    ButtonText.color = BaseTextColor;
                }
                if (Icon != null)
                {
                    Icon.color = BaseTextColor;
                }
            }
            else
            {
                if (ButtonText != null)
                {
                    ButtonText.overrideColorTags = true;
                    ButtonText.color = DisabledTextColor;
                }
                if (Icon != null)
                {
                    Icon.color = DisabledTextColor;
                }
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

        public void Disable_Button()
        {
            if (ButtonText != null)
            {
                ButtonText.overrideColorTags = true;
                ButtonText.color = DisabledTextColor;
            }
            if (Icon != null)
            {
                Icon.color = DisabledTextColor;
            }
        }

        public void Enable_Button()
        {
            PointerUp();
        }
    }
}
