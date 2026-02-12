using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.UI
{
    public class HideObjectDialog : Dialog
    {
        [SerializeField] private BagIdLabel bagIdEntry;
        [SerializeField] private RectTransform bagIdContainer;

        public void SetBagId(List<string> bagId)
        {
            foreach (var item in bagId)
            {
                BagIdLabel label = Instantiate(bagIdEntry, bagIdContainer);
                label.SetText(item);
            }
        }
    }
}
