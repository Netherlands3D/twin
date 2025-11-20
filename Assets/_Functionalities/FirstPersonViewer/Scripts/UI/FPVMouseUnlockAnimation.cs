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
        private FirstPersonViewerInput firstPersonInput;

        private void OnEnable()
        {
            firstPersonInput = ServiceLocator.GetService<FirstPersonViewer>().Input;
            firstPersonInput.OnLockStateChanged += PlayUnlockCircleAnimation;
            unlockCircleImage = GetComponent<Image>();
        }

        private void OnDisable()
        {
            firstPersonInput.OnLockStateChanged -= PlayUnlockCircleAnimation;
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
            yield return new WaitForEndOfFrame();

            for (int i = 0; i < unlockCircleSprites.Length; i++)
            {
                Sprite sprite = unlockCircleSprites[i];
                unlockCircleImage.sprite = sprite;
                unlockCircleImage.rectTransform.sizeDelta = sprite.textureRect.size;
                yield return new WaitForSeconds(0.03f);

            }
            unlockCircleImage.enabled = false;
            playingSequence = false;
        }
    }
}
