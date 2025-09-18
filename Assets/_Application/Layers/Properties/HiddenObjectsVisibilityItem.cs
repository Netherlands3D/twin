using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsVisibilityItem : MonoBehaviour, ISelectHandler, IDeselectHandler
    {  
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text objectId;

        [SerializeField] private Sprite visible;
        [SerializeField] private Sprite invisible;

        public UnityEvent<bool> ToggleVisibility = new();
        public UnityEvent<string> OnSelectItem = new();
        public UnityEvent<string> OnDeselectItem = new();

        private Image image;

        private void Awake()
        {
            toggle.onValueChanged.AddListener(OnToggle);
            image = toggle.targetGraphic.GetComponent<Image>();
        }

        void OnToggle(bool isOn)
        {
            ToggleVisibility.Invoke(isOn);
            UpdateGraphic();
            //keep behaviour the same as when selecting the item when pressing toggle
            if (!isOn)
                EventSystem.current.SetSelectedGameObject(gameObject);
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

        public void OnSelect(BaseEventData eventData)
        {
            OnSelectItem.Invoke(objectId.text);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            OnDeselectItem.Invoke(objectId.text);
        }
    }
}
