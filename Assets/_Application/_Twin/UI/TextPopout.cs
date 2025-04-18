using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI
{
    public class TextPopout : MonoBehaviour
    {
        [SerializeField] private TMP_InputField textField;

        private RectTransform rectTransform;
        private Camera mainCamera;
        private Coordinate? stuckToWorldPosition = null;

        public UnityEvent<string> OnEndEdit;
        public UnityEvent TextFieldSelected;
        public UnityEvent TextFieldDeselected;
        
        public bool ReadOnly
        {
            get => textField.readOnly;
            set => textField.readOnly = value;
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            textField.onSubmit.AddListener(OnSubmitText);
            textField.onEndEdit.AddListener(OnEndEdit.Invoke);
            textField.onSelect.AddListener(OnTextFieldSelect); 
            textField.onDeselect.AddListener(OnTextFieldDeselect);
        }

        private void OnTextFieldSelect(string text)
        {
            TextFieldSelected.Invoke();
        }
        
        private void OnTextFieldDeselect(string text)
        {
            TextFieldDeselected.Invoke();
        }

        private void OnDisable()
        {
            textField.onSubmit.RemoveListener(OnSubmitText);
            textField.onEndEdit.RemoveListener(OnEndEdit.Invoke);
            textField.onSelect.RemoveListener(OnTextFieldSelect); 
            textField.onDeselect.RemoveListener(OnTextFieldDeselect);
        }

        private void OnSubmitText(string text)
        {
            if (NewLineModifierKeyIsPressed())
            {
                textField.Select();
                textField.text += "\n";
                // Ensure the input field remains focused
                EventSystem.current.SetSelectedGameObject(textField.gameObject, null);
                textField.ActivateInputField();
                textField.caretPosition = textField.text.Length;
            }
        }

        public void Show(string text, Vector3 atScreenPosition)
        {
            textField.text = text;
            MoveTo(atScreenPosition);
            StickTo(null);

            gameObject.SetActive(true);
        }

        public void Show(string text, Coordinate atWorldPosition, bool stickToWorldPosition = false)
        {
            Show(text, mainCamera.WorldToScreenPoint(atWorldPosition.ToUnity()));

            if (stickToWorldPosition) StickTo(atWorldPosition);
            else StickTo(null);
        }

        public void MoveTo(Vector3 atScreenPosition)
        {
            rectTransform.position = atScreenPosition;
        }

        public void MoveTo(Coordinate atWorldPosition, bool stickToWorldPosition = false)
        {
            MoveTo(mainCamera.WorldToScreenPoint(atWorldPosition.ToUnity()));
            StickTo(stickToWorldPosition ? atWorldPosition : null);
        }

        public void StickTo(Coordinate? atWorldPosition)
        {
            stuckToWorldPosition = atWorldPosition;
        }

        private void LateUpdate()
        {
            if (stuckToWorldPosition == null) return;

            MoveTo(mainCamera.WorldToScreenPoint(stuckToWorldPosition.Value.ToUnity()));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetTextWithoutNotify(string newText)
        {
            textField.SetTextWithoutNotify(newText);
        }

        public static bool NewLineModifierKeyIsPressed()
        {
            return Keyboard.current.shiftKey.isPressed;
        }
    }
}