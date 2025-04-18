using System;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Sun
{
    public class ToggleSpriteSwap : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image imageToSwap;
        [SerializeField] private Sprite startSprite;
        [SerializeField] private Sprite swappedSprite;

        // Start is called before the first frame update
        void Start()
        {
            if (toggle == null) toggle = GetComponent<Toggle>();

            if (toggle == null)
            {
                throw new NullReferenceException("Toggle hasn't been set in the inspector!");
            }

            if (imageToSwap == null)
            {
                imageToSwap = toggle.graphic as Image;
            }

            // The graphic needs to be removed, because toggle will disable the SpriteRenderer otherwise, and we want to
            // toggle
            if (toggle.graphic == imageToSwap)
            {
                toggle.graphic = null;
            }

            toggle.onValueChanged.AddListener(SwapSprites);
            SwapSprites(toggle.isOn);
        }

        public void SwapSprites(bool isActive)
        {
            imageToSwap.sprite = isActive ? startSprite : swappedSprite;
        }
    }
}
