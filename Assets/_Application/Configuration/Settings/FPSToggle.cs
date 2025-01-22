using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Configuration.Settings
{
    public class FPSToggle : MonoBehaviour
    {
        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();

            FPSCounter counter = FindObjectOfType<FPSCounter>(true);
            if (counter != null)
            {
                toggle.isOn = counter.gameObject.activeSelf;
                return;
            }

            Debug.Log("could not find ui scaler or camera scaler, disabling toggle");
            toggle.interactable = false;
        }

        public void ToggleFPSCounter(bool isOn)
        {
            FPSCounter counter = FindObjectOfType<FPSCounter>(true);
            if (counter != null)
            {
                if(isOn != counter.gameObject.activeSelf) 
                    counter.gameObject.SetActive(isOn);
            }
        }
    }
}