using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Avatar : MonoBehaviour
    {
        [SerializeField] private Image imageField;
        [SerializeField] private Character character;

        public Character Character
        {
            get => character;
            set
            {
                character = value;
                UpdatePhoto();
            }
        }

        private void OnValidate()
        {
            UpdatePhoto();
        }

        private void UpdatePhoto()
        {
            if (imageField && character)
            {
                imageField.sprite = character.avatar;
            }
        }
    }
}
