using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Configuration.Settings
{
    [RequireComponent(typeof(Toggle))]
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

            Debug.LogWarning("could not find fps counter, disabling toggle");
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