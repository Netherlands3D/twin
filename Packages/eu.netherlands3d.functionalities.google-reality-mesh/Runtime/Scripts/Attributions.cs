using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.GoogleRealityMesh
{
    public class Attributions : MonoBehaviour
    {
        [SerializeField] private GameObject attributionsPanel;
        [SerializeField] private TMP_Text attributionsText;


        public void SetAttributionsText(string attributions)
        {
            attributionsText.text = attributions;
            var hasText = !string.IsNullOrEmpty(attributions);
            
            attributionsPanel.gameObject.SetActive(hasText);
        }
    }
}
