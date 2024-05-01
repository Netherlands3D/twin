using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Functionalities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    /// <summary>
    /// A button with a nested checkbox to enable/disable functionaly
    /// </summary>
    public class FunctionalitySelection : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text caption;
       
        [FormerlySerializedAs("feature")]
        private Functionality functionality;

        [SerializeField] private Toggle toggle;
        [SerializeField] private Button button;

        public Toggle.ToggleEvent OnToggle => toggle.onValueChanged;
        public Button.ButtonClickedEvent OnClick => button.onClick;
       
        public void Init(Functionality functionality)
        {
            this.functionality = functionality;
            
            // Two-way binding, if the functionality changes state with the SetupWizard open, it should
            // influence whether it is toggled. This is used, for example, by the validation mechanism.
            functionality.OnDisable.AddListener(() => toggle.SetIsOnWithoutNotify(false));
            functionality.OnEnable.AddListener(() => toggle.SetIsOnWithoutNotify(true));
                
            title.text = functionality.Title;
            caption.text = functionality.Caption;
            toggle.isOn = this.functionality.IsEnabled;
        }
    }
}
