using TMPro;
using UnityEngine;

namespace Netherlands3D
{
    public class HideObjectDialog : Dialog
    {
        [SerializeField] private TextMeshProUGUI bagIdText;

        public void SetBagId(string bagId)
        {
            bagIdText.text = bagId;
        }
    }
}
