using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SnackBarItem : VisualElement
    {
        public enum SnackbarMessageType
        {
            Info,
            Warning
        }

        private Icon icon;
        private Label titleLabel;
        private Label detailsLabel;

        private Icon Icon => icon ??= this.Q<Icon>();
        private Label TitleLabel => titleLabel ??= this.Q<Label>("Titel");
        private Label DetailsLabel => detailsLabel ??= this.Q<Label>("Text");

        private SnackbarMessageType messageType = SnackbarMessageType.Info;

        [UxmlAttribute("message-type")]
        public SnackbarMessageType MessageType
        {
            get => messageType;
            set
            {
                messageType = value;
                EnableInClassList("snackbar-item--warning", messageType == SnackbarMessageType.Warning);
            }
        }

        [UxmlAttribute("icon")]
        public IconImage Image
        {
            get => Icon.Image;
            set => Icon.Image = value;
        }

        public SnackBarItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }

        public void SetMessage(string title, string details, SnackbarMessageType type, IconImage icon)
        {
            TitleLabel.text = title;
            DetailsLabel.text = details;
            DetailsLabel.EnableInClassList(UtilityClassConstants.HIDDEN, string.IsNullOrEmpty(details));

            MessageType = type;
            Image = icon;
        }
    }
}