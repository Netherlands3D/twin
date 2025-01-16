using TMPro;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectInformation
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
