using TMPro;
using UnityEngine;

namespace Netherlands3D.Indicators.UI
{
    public class Sidebar : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private TMP_Text nameField;

        private void OnEnable()
        {
            if (nameField) nameField.text = dossier.Data.HasValue ? dossier.Data.Value.name : "";
        }
    }
}
