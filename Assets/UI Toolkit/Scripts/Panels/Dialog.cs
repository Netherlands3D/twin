using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.UI;
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
        public UnityEvent onClose;
        public UnityEvent onOpen;

        public UnityEvent OnClose = new();
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
            Debug.Log("OnAccept");
        }

        private void OnReject(ClickEvent e)
        {
            Debug.Log("OnReject");
        }
    }
}