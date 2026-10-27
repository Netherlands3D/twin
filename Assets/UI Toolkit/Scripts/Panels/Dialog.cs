using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class Dialog : VisualElement
    {
        //todo maybe we wish to give back the caller object within the event signatures?
        //for now possibly no since a dialog should have a single choice lifecycle
        public UnityEvent Accept = new();
        public UnityEvent Reject = new();
        private Button yes, no;

        public Dialog()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            yes = this.Q<Button>("Yes");
            no = this.Q<Button>("No");

            yes.RegisterCallback<ClickEvent>(OnAccept);
            no.RegisterCallback<ClickEvent>(OnReject);
        }

        private void OnAccept(ClickEvent e)
        {
            Accept.Invoke();
            Clear();
        }

        private void OnReject(ClickEvent e)
        {
            Reject.Invoke();
            Clear();
        }

        private void Clear()
        {
            Show(false);
            Accept.RemoveAllListeners();
            Reject.RemoveAllListeners();
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}