using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    public class ViewPositionPickerIcon : MonoBehaviour
    {
        [SerializeField] private Transform arrow;
        [SerializeField] private float intensity = 2.4f;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private Vector2 cursorOffset = new Vector2(0, 30);

        private void Update()
        {
            Vector2 screenPoint = Pointer.current.position.ReadValue();
            transform.position = screenPoint + cursorOffset;

            Vector2 arrowPosition = arrow.localPosition;
            float yPos = Mathf.Sin(Time.time * speed) * intensity;
            arrowPosition.y = yPos;

            arrow.transform.localPosition = arrowPosition;
        }
    }
}
