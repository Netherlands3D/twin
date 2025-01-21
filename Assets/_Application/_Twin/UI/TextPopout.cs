using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.UI
{
    public class TextPopout : MonoBehaviour
    {
        [SerializeField] private TMP_Text textField;
        private RectTransform rectTransform;
        private Camera mainCamera;
        private Coordinate? stuckToWorldPosition = null;

        private void Start()
        {
            mainCamera = Camera.main;
            rectTransform = GetComponent<RectTransform>();
            gameObject.SetActive(false);
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

        private void Update()
        {
            if (stuckToWorldPosition == null) return;

            MoveTo(mainCamera.WorldToScreenPoint(stuckToWorldPosition.Value.ToUnity()));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
