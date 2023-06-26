using Netherlands3D.Indicators.Data;
using TMPro;
using UnityEngine;

namespace Netherlands.Indicators
{
    public class DossierUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI nameField;

        public void Load(Dossier dossier)
        {
            nameField.text = dossier.name;
        }
    }
}
