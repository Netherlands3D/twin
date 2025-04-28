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
        [SerializeField] private float disappearDistance = 2000f;
        [SerializeField] private float doubleClickThreshold = 0.5f;
        private float lastClickTime = -0.5f;
        private float originalSelectionColorAlpha;

        private RectTransform rectTransform;
        private Camera mainCamera;
        private Coordinate? stuckToWorldPosition = null;

        public UnityEvent<string> OnEndEdit;
        public UnityEvent TextFieldSelected;
        public UnityEvent TextFieldDeselected;
        public UnityEvent TextFieldDoubleClicked;

        public bool ReadOnly
        {
            get => textField.readOnly;
            set => textField.readOnly = value;
        }

        // Unfortunately we cannot use the textfield.interactable property, since this also changes the selection state, which we don't want.
        // Instead we will set the selection alpha color to 0 to make it seem like no text is selected.
        public bool SelectableText
        {
            get => originalSelectionColorAlpha != textField.selectionColor.a;
            set
            {
                var color = textField.selectionColor;
                color.a = value ? originalSelectionColorAlpha : 0;
                textField.selectionColor = color;
            }
        }

        private void Awake()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            gameObject.SetActive(false);
            originalSelectionColorAlpha = textField.selectionColor.a;
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

        public void OnTextFieldClick(BaseEventData data) //event called through a event trigger in the inspector when the child input field is clicked
        {
            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickThreshold)
            {
                TextFieldDoubleClicked.Invoke();
            }

            lastClickTime = Time.time;
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
                var caretPosition = textField.caretPosition;
                var firstHalf = textField.text.Substring(0, caretPosition);
                var secondHalf = textField.text.Substring(caretPosition);
                textField.text = firstHalf + "\n" + secondHalf;
                // Ensure the input field remains focused
                EventSystem.current.SetSelectedGameObject(textField.gameObject, null);
                textField.ActivateInputField();
                textField.caretPosition = caretPosition + 1;
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
            // Canvas renders UI elements with z values between -1000 and +1000
            // this range is affected by the canvas scale, but the atScreenPosition z is also scaled so no further correction is needed

            var scaledZ = atScreenPosition.z / disappearDistance * 1000;
            atScreenPosition.z = scaledZ;
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