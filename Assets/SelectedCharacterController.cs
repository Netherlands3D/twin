using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class SelectedCharacterController : MonoBehaviour
    {
        [SerializeField] private Avatar avatar;
        [SerializeField] private TMP_Text nameField;
        [SerializeField] private TMP_Text descriptionField;
        
        public void Show(Character character)
        {
            avatar.Character = character;
            nameField.text = character.name;
            descriptionField.text = character.description;
        }
    }
}
