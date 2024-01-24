using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class MatchSpriteToSelectionState : MonoBehaviour
    {
        [SerializeField] private Selectable targetSelectable;
        private Image image;
        private Sprite normalSprite;
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
            if (!targetSelectable.image)
                return;

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