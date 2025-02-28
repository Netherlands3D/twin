using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    public class MatchSpriteToSelectionState : MonoBehaviour
    {
        [SerializeField] private Selectable targetSelectable;
        private Image image;
        [SerializeField] private Sprite highlightedSprite;
        [SerializeField] private Sprite pressedSprite;
        [SerializeField] private Sprite selectedSprite;
        [SerializeField] private Sprite disabledSprite;

        private void Awake()
        {
            image = GetComponent<Image>();
        }

        private void Update()
        {
            if (!targetSelectable.targetGraphic)
            {
                return;
            }

            if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.highlightedSprite)
                image.overrideSprite = highlightedSprite;
            else if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.pressedSprite)
                image.overrideSprite = pressedSprite;
            else if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.selectedSprite)
                image.overrideSprite = selectedSprite;
            else if (targetSelectable.image.overrideSprite == targetSelectable.spriteState.disabledSprite)
                image.overrideSprite = disabledSprite;
            else
                image.overrideSprite = null;
        }
    }
}