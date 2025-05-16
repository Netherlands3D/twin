using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.UI
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
