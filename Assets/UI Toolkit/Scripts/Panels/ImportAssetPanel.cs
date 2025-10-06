using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ImportAssetPanel : BaseInspectorContentPanel
    {
        private Button uploadButton;
        private Button UploadButton => uploadButton ??= this.Q<Button>(className: "import-asset-panel-button-upload");

        public EventCallback<ClickEvent> UploadStarted { get; set; }

        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.AddLayer;

        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);
            UploadButton.RegisterCallback<ClickEvent>(OnUploadStarted);
        }

        public override string GetTitle() => "Importeren";

        private void OnUploadStarted(ClickEvent evt)
        {
            UploadStarted?.Invoke(evt);
        }
    }
}
