using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.Indicators.Dossiers.DataLayers;
using UnityEngine;

namespace Netherlands3D.Indicators.UI
{
    public class Legend : MonoBehaviour
    {
        [SerializeField] private LegendInterfaceItem legendItemPrefab;

        [Tooltip("Always clear the old items before showing the new ones")]
        [SerializeField] private bool clearOld = true;

        private void Awake() {
            ClearItems();
        }

        public void ShowItems(DataLayer dataLayer)
        {
            ShowItems(dataLayer.legend);
        }

        public void ShowItems(List<LegendItem> items)
        {
            if(clearOld)
                ClearItems();

            this.gameObject.SetActive(true);
            
            foreach (var item in items)
            {
                var legendItem = Instantiate(legendItemPrefab, transform);        
                legendItem.Set(item.label, item.color);
            }
        }

        public void ClearItems()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            this.gameObject.SetActive(false);
        }
    }
}
