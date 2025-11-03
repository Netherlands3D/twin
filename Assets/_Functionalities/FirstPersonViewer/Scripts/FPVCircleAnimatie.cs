using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer
{
    public class FPVCircleAnimatie : MonoBehaviour
    {
        private bool playingSequence;

        [Header("Animation")]
        [SerializeField] private Sprite[] unlockCircleSprites;
        [SerializeField] private Image unlockCircleImage;

        private void OnEnable()
        {
            FirstPersonViewerInput.OnLockStateChanged += PlayUnlockCircleAnimation;
            unlockCircleImage = GetComponent<Image>();
        }

        private void OnDisable()
        {
            FirstPersonViewerInput.OnLockStateChanged -= PlayUnlockCircleAnimation;
        }

        void Update()
        {
            if (playingSequence)
            {
                unlockCircleImage.transform.position = Mouse.current.position.ReadValue();
            }
        }

        private void PlayUnlockCircleAnimation(CursorLockMode lockMode)
        {
            if (lockMode == CursorLockMode.None) StartCoroutine(UnlockCircleSequence());
        }

        private IEnumerator UnlockCircleSequence()
        {
            playingSequence = true;
            unlockCircleImage.enabled = true;
            foreach (Sprite sprite in unlockCircleSprites)
            {
                unlockCircleImage.sprite = sprite;
                unlockCircleImage.rectTransform.sizeDelta = sprite.textureRect.size;
                yield return new WaitForSeconds(0.03f);
            }
            unlockCircleImage.enabled = false;
            playingSequence = false;
        }
    }
}
