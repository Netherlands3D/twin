using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class HiddenObjectsVisibilityItem : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private TMP_Text bagId;

        [SerializeField] private Sprite visible;
        [SerializeField] private Sprite invisible;

        private void Start()
        {
            toggle.onValueChanged.AddListener(OnToggle);
        }

        void OnToggle(bool isOn)
        {            
            toggle.targetGraphic.GetComponent<Image>().sprite = isOn ? visible : invisible;
        }

        void OnDestroy()
        {
            toggle.onValueChanged.RemoveListener(OnToggle);
        }
    }
}
