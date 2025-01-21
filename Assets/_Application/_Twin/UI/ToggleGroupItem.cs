using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    public class ToggleGroupItem : MonoBehaviour
    {
        private Toggle toggle;
        public Toggle Toggle { get => toggle; }

        [Header("Icon colors")]
        [SerializeField] private Color enabledIconColor = Color.white;
        [SerializeField] private Color disabledIconColor = Color.grey;

        [SerializeField] private Image icon;

        public bool IsInteractable { get => toggle.interactable; }

        void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        public void SetInteractable(bool interactable)
        {
            toggle.interactable = interactable;
            icon.color = interactable ? enabledIconColor : disabledIconColor;
        }
    }
}
