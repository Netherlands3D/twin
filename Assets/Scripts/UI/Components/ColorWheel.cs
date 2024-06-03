using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Netherlands3D.Twin
{
    public class ColorWheel : MonoBehaviour
    {

        public Color32 CurrentSelectedColor;
        public TextMeshProUGUI CurrentColorCode;
        public string Colorspace = "HEX";
        public RawImage ColorWheelPic;


        public void SelectNewColor()
        {
            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            int pos1 = (int)mousePosition.x;
            int pos2 = (int)mousePosition.y;
            CurrentColorCode.text = CurrentSelectedColor.ToString();
        }

   
    }
}
