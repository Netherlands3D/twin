using System;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using TextField = Netherlands3D.UI.Components.TextField;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ImportAssetPanel : BaseInspectorContentPanel
    {
        private Button uploadButton;
        private Button UploadButton => uploadButton ??= this.Q<Button>("FileUploadButton");
        private TextField importUriField;
        private TextField ImportUriField => importUriField ??= this.Q<TextField>("ImportUriField");
        private Button importUriButton;
        private Button ImportUriButton => importUriButton ??= this.Q<Button>("ImportUriButton");

        public EventCallback<ClickEvent> FileUploadStarted { get; set; }
        public Action<Uri> UriImportStarted { get; set; }

        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.AddLayer;

        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);
            UploadButton.RegisterCallback<ClickEvent>(OnUploadStarted);
            ImportUriButton.RegisterCallback<ClickEvent>(OnImportUri);
        }

        public override string GetTitle() => "Importeren";

        private void OnUploadStarted(ClickEvent evt)
        {
            FileUploadStarted?.Invoke(evt);
        }

        private void OnImportUri(ClickEvent evt)
        {
            try
            {
                Uri uri = new Uri(ImportUriField.value);
                UriImportStarted?.Invoke(uri);
            }
            catch (Exception e)
            {
                // TODO: Add better error handling
                Debug.LogException(e);
            }
        }
    }
}
