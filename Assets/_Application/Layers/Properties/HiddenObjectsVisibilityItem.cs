using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsVisibilityItem : MonoBehaviour, IPointerDownHandler, ISelectable 
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text objectId;

        [SerializeField] private Sprite visible;
        [SerializeField] private Sprite invisible;

        public UnityEvent<bool> ToggleVisibility = new();
        public UnityEvent<string> OnSelectItem = new();

        public bool IsSelected => selected;
        public string ObjectId => objectId.text;
        public Image Image => image;

        private bool selected = false;
        private Image image;
        private HiddenObjectsSelectableButton button;

        private void Awake()
        {
            toggle.onValueChanged.AddListener(OnToggle);
            image = toggle.targetGraphic.GetComponent<Image>();
            button = gameObject.GetComponent<HiddenObjectsSelectableButton>();

        }

        void OnToggle(bool isOn)
        {
            ToggleVisibility.Invoke(isOn);
            UpdateGraphic();
        }

        public void SetToggleState(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
            UpdateGraphic();
        }

        private void UpdateGraphic()
        {
            image.sprite = toggle.isOn ? visible : invisible;
        }

        void OnDestroy()
        {
            toggle.onValueChanged.RemoveListener(OnToggle);
        }

        public void SetObjectId(string id)
        {
            objectId.text = id;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnSelectItem.Invoke(objectId.text);
        }

        public void SetSelected(bool isSelected)
        {
            selected = isSelected;
            button.ForceVisualSelection(isSelected);

            //debug
            image.color = isSelected ? Color.green : Color.red;
        }
    }
}
