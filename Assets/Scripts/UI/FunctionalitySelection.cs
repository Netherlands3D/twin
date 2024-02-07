using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Functionalities;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Toggle))]
    public class FunctionalitySelection : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text caption;
       
        [FormerlySerializedAs("feature")]
        private Functionality functionality;
        private Toggle toggle;
        public Toggle Toggle { get => toggle; private set => toggle = value; }

        private Button button;
        public Button Button { get => button; private set => button = value; }
       
        public void Init(Functionality functionality)
        {
            this.functionality = functionality;

            title.text = functionality.Title;
            caption.text = functionality.Caption;

            Toggle = GetComponent<Toggle>();
            Toggle.isOn = this.functionality.IsEnabled;

            Button = GetComponentInChildren<Button>();
        }
    }
}
