using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ColorSwatch : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
    {
        public Button Button => button;
        public bool IsSelected => isSelected;
        public TMP_InputField InputField => inputField;
        public TMP_Text TextField => textField;
        public string LayerName => layerName;

        [SerializeField] private Image tileColor;
        [SerializeField] private Button button;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_Text textField;
        [SerializeField] private Image selection;
        [SerializeField] private Sprite selectionSprite;
        [SerializeField] private Sprite normalSprite;

        public UnityEvent<PointerEventData> onClickUp = new();
        public UnityEvent<PointerEventData> onClickDown = new();

        private bool isSelected = false;
        private string layerName;

        void Awake()
        {
            SetSelected(false);
            inputField.interactable = false;
            inputField.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            inputField.onEndEdit.AddListener(OnInputFieldChanged);
        }

        void OnDisable()
        {
            inputField.onEndEdit.RemoveListener(OnInputFieldChanged);
        }

        public void SetSelected(bool isSelected)
        {
            this.isSelected = isSelected;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            selection.sprite = isSelected ? selectionSprite : normalSprite;
            selection.type = Image.Type.Sliced;
            selection.pixelsPerUnitMultiplier = 32;
        }

        public void SetLayerName(string name)
        {
            layerName = name;
            textField.text = layerName;
        }

        public void SetInputText(string text)
        {
            inputField.text = text;
        }

        public void SetColor(Color color)
        {
            tileColor.color = color;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onClickUp.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            onClickDown.Invoke(eventData); 
        }

        private void OnInputFieldChanged(string newName)
        {
            layerName = newName;
            textField.text = newName;
            inputField.gameObject.SetActive(false);
            textField.gameObject.SetActive(true);

            StartCoroutine(WaitForNextFrame(() => 
            {
                //needs to happen next frame
                inputField.interactable = false;
            }));            
        }

        private IEnumerator WaitForNextFrame(Action onNextFrame)
        {
            yield return new WaitForEndOfFrame();
            onNextFrame.Invoke();
        }
    }
}
