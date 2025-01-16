using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Sun
{
    public class ToggleSpeedButton : MonoBehaviour
    {
        [SerializeField] int activeAtValue;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void SetInteractable(int dropdownValue)
        {
            button.interactable = dropdownValue == activeAtValue;
        }
    }
}
