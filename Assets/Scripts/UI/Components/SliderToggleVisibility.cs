using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{

    public class SliderToggleVisibility : MonoBehaviour
    {
        public Slider slider;                           // Reference to the UI Slider  
        public List<GameObject> DeactivateAt100;         // List of GameObjects to activate when slider is not at 100  
        public List<GameObject> ActivateAt100;         // List of GameObjects to activate when slider is at 100

        void Start()
        {
            // Set the initial state based on the slider value  
            UpdateObjectVisibility();

            // Add a listener to call UpdateObjectVisibility whenever the slider value changes  
            slider.onValueChanged.AddListener(delegate { UpdateObjectVisibility(); });
        }

        private void UpdateObjectVisibility()
        {
            // Check the slider's value  
            if (slider.value >= 100)
            {
                // If the slider is at 100, deactivate all active objects and activate hidden ones  
                SetGameObjectsActive(ActivateAt100, true);
                SetGameObjectsActive(DeactivateAt100, false);
            }
            else
            {
                // If the slider is not at 100, activate all active objects and deactivate hidden ones  
                SetGameObjectsActive(DeactivateAt100, true);
                SetGameObjectsActive(ActivateAt100, false);
            }
        }

        private void SetGameObjectsActive(List<GameObject> gameObjects, bool isActive)
        {
            foreach (GameObject obj in gameObjects)
            {
                if (obj != null) // Check if the object is not null  
                {
                    obj.SetActive(isActive);
                }
            }
        }
    }
}
