using Netherlands3D.Services;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FPVMouseUnlockAnimation : MonoBehaviour
    {
        private bool playingSequence;

        [Header("Animation")]
        [SerializeField] private Sprite[] unlockCircleSprites;
        private Image unlockCircleImage;
        private Transform unlockCircleImageTransform;
        private FirstPersonViewerInput firstPersonInput;

        private void OnEnable()
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();
            if (fpv != null)
            {
                firstPersonInput = fpv.Input;
                firstPersonInput.OnLockStateChanged += PlayUnlockCircleAnimation;
            }

            unlockCircleImage = GetComponent<Image>();
            unlockCircleImageTransform = GetComponent<Transform>();
        }

        private void OnDisable()
        {
            if(firstPersonInput != null) firstPersonInput.OnLockStateChanged -= PlayUnlockCircleAnimation;
        }

        void Update()
        {
            if (!playingSequence) return;
            if (Pointer.current == null) return;

            unlockCircleImageTransform.position = Pointer.current.position.ReadValue();
        }

        private void PlayUnlockCircleAnimation(CursorLockMode lockMode)
        {
            if (lockMode == CursorLockMode.None) StartCoroutine(UnlockCircleSequence());
        }

        private IEnumerator UnlockCircleSequence()
        {
            playingSequence = true;
            unlockCircleImage.enabled = true;
            yield return new WaitForEndOfFrame();

            for (int i = 0; i < unlockCircleSprites.Length; i++)
            {
                Sprite sprite = unlockCircleSprites[i];
                unlockCircleImage.sprite = sprite;
                unlockCircleImage.rectTransform.sizeDelta = sprite.textureRect.size * 0.66667f;
                yield return new WaitForSeconds(0.03f);
            }
            unlockCircleImage.enabled = false;
            playingSequence = false;
        }
    }
}
