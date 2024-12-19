using System;
using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin
{
    public class CharacterSelectionScreen : MonoBehaviour
    {
        public List<Character> characters = new();
        [SerializeField] private Avatar selectedCharacter;
        [SerializeField] private CharacterInfo characterInfo;
        public Avatar avatarPrefab;
        public GridLayoutGroup selectionArea;
        [SerializeField] private Transform SelectionRing;
        [SerializeField] private SelectedCharacterController selectedCharacterController;

        public Avatar SelectedCharacter
        {
            get => selectedCharacter;
            set => SelectCharacter(value);
        }

        private void UpdateSelectedCharacter()
        {
            var hasActiveCharacter = selectedCharacter != null;
            SelectionRing.gameObject.SetActive(hasActiveCharacter);
            if (!hasActiveCharacter) return;
            var position = new Vector3(selectedCharacter.RectTransform().position.x - 31,
                selectedCharacter.RectTransform().position.y + 31, selectedCharacter.RectTransform().position.z);
            SelectionRing.RectTransform().position = position;
        }
        
        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            UpdateCharacters();
        }

        public void SelectCharacter(Avatar avatar)
        {
            selectedCharacter = avatar;
            UpdateSelectedCharacter();
            if (avatar)
            {
                selectedCharacterController.Show(avatar.Character);
                characterInfo.Character = avatar.Character;
            }
        }

        [ContextMenu("Update characters")]
        private void UpdateCharacters()
        {
            SelectedCharacter = null;
            for(int i = selectionArea.transform.childCount -1; i >= 0; i--)
            {
                Object.DestroyImmediate(selectionArea.transform.GetChild(i).gameObject);
            }
            foreach (var character in characters)
            {
                var avatar = Instantiate(avatarPrefab, selectionArea.transform);
                avatar.Character = character;
                if (!selectedCharacter) SelectedCharacter = avatar;
                avatar.GetComponent<Button>().onClick.AddListener(() => SelectCharacter(avatar));
            }
        }
    }
}
