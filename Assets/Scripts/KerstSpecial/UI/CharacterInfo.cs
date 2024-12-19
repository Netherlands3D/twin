using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class CharacterInfo : MonoBehaviour
    {
        [SerializeField] private Character character;
        public TMP_Text name;
        public Image photo;

        public Character Character
        {
            get => character;
            set {
                character = value;
                UpdateDetails();
            }
        }

        void Start()
        {
            UpdateDetails();
        }

        private void UpdateDetails()
        {
            photo.sprite = Character.avatar;
            name.text = Character.name;
        }
    }
}
