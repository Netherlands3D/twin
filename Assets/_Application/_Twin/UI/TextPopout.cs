using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Rendering;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

namespace Netherlands3D.Twin.UI
{
    public class TextPopout : MonoBehaviour
    {
        [SerializeField] private TMP_InputField textField;
        [SerializeField] private float disappearDistance = 2000f;
        [SerializeField] private float doubleClickThreshold = 0.5f;
        [SerializeField] private RectTransform pointTransform;

        [Header("Image")]
        [SerializeField] private GameObject imageContainer;
        [SerializeField] private RawImage imagePreview;
        [SerializeField] private TMP_Text imageCaptionText;
        [SerializeField] private int maxImagePreviewDimension = 512;

        private float lastClickTime = -0.5f;
        private float originalSelectionColorAlpha;

        private RectTransform rectTransform;
        private Camera mainCamera;
        private Coordinate? stuckToWorldPosition = null;
        private Coroutine imageLoadRoutine;
        private Texture2D loadedTexture;
        private Vector2 initialImagePreviewSize;

        public UnityEvent<string> OnEndEdit;
        public UnityEvent TextFieldSelected;
        public UnityEvent TextFieldDeselected;
        public UnityEvent TextFieldDoubleClicked;
        public UnityEvent TextFieldInputConfirmed;

        private float localDistanceToPoint = 20;

        public enum SnappingSide { Left, Right, Above }
        private SnappingSide snappingSide = SnappingSide.Above;

        public TMP_InputField TextField => textField;

        public bool ReadOnly
        {
            get => textField.readOnly;
            set => textField.readOnly = value;
        }

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

            if (imagePreview != null)
                initialImagePreviewSize = imagePreview.rectTransform.sizeDelta;

            ClearImage();
        }

        private void OnEnable()
        {
            textField.onSubmit.AddListener(OnSubmitText);
            textField.onEndEdit.AddListener(OnEndEdit.Invoke);
            textField.onSelect.AddListener(OnTextFieldSelect);
            textField.onDeselect.AddListener(OnTextFieldDeselect);
        }

        private void OnDisable()
        {
            textField.onSubmit.RemoveListener(OnSubmitText);
            textField.onEndEdit.RemoveListener(OnEndEdit.Invoke);
            textField.onSelect.RemoveListener(OnTextFieldSelect);
            textField.onDeselect.RemoveListener(OnTextFieldDeselect);
        }

        public void SetSnappingSide(SnappingSide snap)
        {
            snappingSide = snap;
        }

        private void OnTextFieldSelect(string text)
        {
            TextFieldSelected.Invoke();
        }

        private void OnTextFieldDeselect(string text)
        {
            TextFieldDeselected.Invoke();
        }

        public void OnTextFieldClick(BaseEventData data)
        {
            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickThreshold)
            {
                TextFieldDoubleClicked.Invoke();
            }

            lastClickTime = Time.time;
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
                EventSystem.current.SetSelectedGameObject(textField.gameObject, null);
                textField.ActivateInputField();
                textField.caretPosition = caretPosition + 1;
                return;
            }

            TextFieldInputConfirmed.Invoke();
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
            var scaledZ = atScreenPosition.z / disappearDistance * 1000;
            atScreenPosition.z = scaledZ;
            rectTransform.position = atScreenPosition;
            pointTransform.pivot = new Vector2(0.5f, 0.5f);
            pointTransform.position = atScreenPosition;

            switch (snappingSide)
            {
                case SnappingSide.Left:
                    rectTransform.pivot = new Vector2(-localDistanceToPoint / rectTransform.rect.width, 0.5f);
                    break;
                case SnappingSide.Right:
                    rectTransform.pivot = new Vector2(1 + localDistanceToPoint / rectTransform.rect.width, 0.5f);
                    break;
                case SnappingSide.Above:
                    rectTransform.pivot = new Vector2(0.5f, -localDistanceToPoint / rectTransform.rect.height);
                    break;
            }
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

        public void SetImageCaption(string caption)
        {
            if (imageCaptionText != null)
                imageCaptionText.text = caption ?? "";
        }

        public void SetImageTexture(Texture texture)
        {
            if (imageLoadRoutine != null)
            {
                StopCoroutine(imageLoadRoutine);
                imageLoadRoutine = null;
            }

            loadedTexture = null;

            ApplyImageTexture(texture);
        }

        private void ApplyImageTexture(Texture texture)
        {
            if (imagePreview != null)
            {
                imagePreview.texture = texture;
                TextureThumbnailUtility.FitWidthToTextureAspect(imagePreview.rectTransform, texture);
            }

            if (imageContainer != null)
                imageContainer.SetActive(texture != null);
        }

        public void SetImageFromPath(string path)
        {
            if (imageLoadRoutine != null)
            {
                StopCoroutine(imageLoadRoutine);
                imageLoadRoutine = null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                ClearImage();
                return;
            }

            imageLoadRoutine = StartCoroutine(LoadImage(path));
        }

        public void ClearImage()
        {
            if (imageLoadRoutine != null)
            {
                StopCoroutine(imageLoadRoutine);
                imageLoadRoutine = null;
            }

            loadedTexture = null;

            if (imagePreview != null)
            {
                imagePreview.texture = null;
                ResetImagePreviewSize();
            }

            if (imageContainer != null)
                imageContainer.SetActive(false);

            if (imageCaptionText != null)
                imageCaptionText.text = "";
        }

        private IEnumerator LoadImage(string path)
        {
            if (TextureThumbnailUtility.TryGetCachedThumbnail(path, out var cachedThumbnail))
            {
                ApplyImageTexture(cachedThumbnail);
                imageLoadRoutine = null;
                yield break;
            }

            using var request = UnityWebRequestTexture.GetTexture(path, true);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Failed to load image: " + request.error);
                ClearImage();
                yield break;
            }

            Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
            loadedTexture = TextureThumbnailUtility.CreateThumbnail(downloadedTexture, maxImagePreviewDimension, "Annotation Popout Thumbnail");
            if (loadedTexture != downloadedTexture)
                Destroy(downloadedTexture);

            TextureThumbnailUtility.CacheThumbnail(path, loadedTexture);
            ApplyImageTexture(loadedTexture);
            imageLoadRoutine = null;
        }

        private void ResetImagePreviewSize()
        {
            if (imagePreview == null || initialImagePreviewSize == Vector2.zero) return;

            imagePreview.rectTransform.sizeDelta = initialImagePreviewSize;
        }

        public static bool NewLineModifierKeyIsPressed()
        {
            return Keyboard.current.shiftKey.isPressed;
        }
    }
}
