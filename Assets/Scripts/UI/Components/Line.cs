using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class Line : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        public void Set(string value)
        {
            text.text = value;
        }
    }
}
