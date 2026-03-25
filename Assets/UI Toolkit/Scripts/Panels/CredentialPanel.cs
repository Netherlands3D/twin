using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using static Netherlands3D.UI.Components.Toggle;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class CredentialPanel : VisualElement
    {
        private Button button;
        private Button Button => button ??= this.Q<Button>("RetryButton");

        public CredentialPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            Button.clicked += () =>
            {
                EnableInClassList("secondary", true);
                this.ReplacePrefixedValueInClassList("credential-panel__message-title-text", ".secondary");
            };
        }

        
    }
}