using System;
using Netherlands3D.Services;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Services
{
    public class DialogService : MonoBehaviour
    {
        public Dialog ActiveDialog => dialog;
        
        private Dialog dialog;     
        private VisualElement dialogAnchor;

        public UnityEvent OnShowDialog = new();
        public UnityEvent OnHideDialog = new();

        private void Start()
        {
            dialog = CreateYesNoDialog();
            dialogAnchor = App.UIRoot.Root.Q<VisualElement>("Dialogs");
            AddDialog(dialog);
        }

        private void OnDestroy()
        {
            RemoveDialog(dialog);
        }

        //todo make generic
        private Dialog CreateYesNoDialog()
        {
            Dialog dialog = new Dialog();
            dialog.Show(false);
            return dialog;
        }
        
        public void AddDialog(Dialog dialog)
        {
            dialogAnchor.Add(dialog);
        }

        public void RemoveDialog(Dialog dialog)
        {
            dialogAnchor.Remove(dialog);
        }
    }
}