using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(Image))]
    public class MatchImageToSelectionState : MonoBehaviour
    {
        [SerializeField] private Selectable targetSelectable;
        private Image image;
        public SpriteState SpriteState; 
        
        private void Awake()
        {
            image = GetComponent<Image>();
            image.sprite = SpriteState?.defaultSprite;
        }

        private void Update()
        {
            if (!targetSelectable.targetGraphic)
            {
                return;
            }

            if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.highlightedSprite)
                image.overrideSprite = SpriteState.highlightedSprite;
            else if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.pressedSprite)
                image.overrideSprite = SpriteState.pressedSprite;
            else if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.selectedSprite)
                image.overrideSprite = SpriteState.selectedSprite;
            else if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.disabledSprite)
                image.overrideSprite = SpriteState.disabledSprite;
            else
                image.overrideSprite = null;
        }
    }
}
