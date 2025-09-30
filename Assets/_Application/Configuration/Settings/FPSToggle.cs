using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Configuration.Settings
{
    [RequireComponent(typeof(Toggle))]
    public class FPSToggle : MonoBehaviour
    {
        private Toggle toggle;
        private List<FPSCounter> fpsCounters;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();

            fpsCounters = FindObjectsByType<FPSCounter>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();
            if (fpsCounters != null)
            {
                toggle.isOn = fpsCounters[0].gameObject.activeSelf;
                return;
            }

            Debug.LogWarning("could not find fps counter, disabling toggle");
            toggle.interactable = false;
        }

        public void ToggleFPSCounter(bool isOn)
        {
            if (fpsCounters != null)
            {
                fpsCounters.ForEach(fps =>
                {
                    fps.gameObject.SetActive(isOn);
                });
            }
        }
    }
}