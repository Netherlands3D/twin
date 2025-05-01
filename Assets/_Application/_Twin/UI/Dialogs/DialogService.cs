using UnityEngine;

namespace Netherlands3D
{
    public class DialogService : MonoBehaviour
    {
        public Dialog ActiveDialog => currentDialog;

        private Dialog currentDialog;

        public void ShowDialog(Dialog dialogPrefab, Vector2 position = default, RectTransform anchor = null)
        {
            Dialog dialog = Instantiate(dialogPrefab);            
            currentDialog = dialog;
            currentDialog.Confirm.AddListener(CloseDialog);
            currentDialog.Cancel.AddListener(CloseDialog);

            if (!dialog.isActiveAndEnabled)
                dialog.Show(true);

            if(anchor != null)
            {                
                RectTransform uiRect = dialog.GetComponent<RectTransform>();
                uiRect.SetParent(anchor, worldPositionStays: false);
                uiRect.anchoredPosition = Vector2.zero;
                uiRect.anchoredPosition += position;
            }
        }

        public void CloseDialog()
        {
            if (currentDialog != null)
            {
                Destroy(currentDialog.gameObject);
                currentDialog = null;
            }
        }
    }
}
