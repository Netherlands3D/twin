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

        private readonly Components.ScrollView scrollView;

        public SnackbarPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            scrollView = this.Q<Components.ScrollView>();
        }

        public SnackBarItem SetMessage(string title, string details, SnackBarItem.SnackbarMessageType type, string icon)
        {
            var item = new SnackBarItem();
            item.SetMessage(title, details, type, icon);
            item.Closed += RemoveItem;

            scrollView.Insert(0, item);
            Show(true);

            return item;
        }

        public void RemoveItem(SnackBarItem item)
        {
            item.Closed -= RemoveItem;
            item.IsClosed = true;
            scrollView.Remove(item);

            if (scrollView.childCount == 0)
                Show(false);
        }

        public void Show(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }
    }
}