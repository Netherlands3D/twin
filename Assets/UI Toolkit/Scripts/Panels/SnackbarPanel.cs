using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class SnackbarPanel : VisualElement
    {
        public UnityEvent OnClose = new();
        public UnityEvent OnOpen = new();

        private SnackBarItem snackBarItem;

        public SnackbarPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            snackBarItem = this.Q<SnackBarItem>();
        }

        public void SetMessage(string title, string details, SnackBarItem.SnackbarMessageType type, IconImage icon)
        {
            snackBarItem.SetMessage(title, details, type, icon);
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}