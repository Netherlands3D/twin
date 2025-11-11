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
        
        private Button goToAssetLibraryButton;
        private Button GoToAssetLibraryButton => goToAssetLibraryButton ??= this.Q<Button>("GoToAssetLibraryButton");

        private Button importFromUrlButton;
        
        // TODO: Remove once we have fixed the copy/paste and credential flow in UI Toolkit
        private Button ImportFromUrlButton => importFromUrlButton ??= this.Q<Button>("FileImportFromUrlButton");
        private TextField importUriField;
        // End: Remove once we have fixed the copy/paste and credential flow in UI Toolkit
        
        private TextField ImportUriField => importUriField ??= this.Q<TextField>("ImportUriField");
        private Button importUriButton;
        private Button ImportUriButton => importUriButton ??= this.Q<Button>("ImportUriButton");

        public EventCallback<ClickEvent> OpenAssetLibrary { get; set; }
        public EventCallback<ClickEvent> FileImportFromUrlStarted { get; set; }
        public EventCallback<ClickEvent> FileUploadStarted { get; set; }
        public Action<Uri> UriImportStarted { get; set; }

        public override ToolbarInspector.ToolbarStyle ToolbarStyle => ToolbarInspector.ToolbarStyle.AddLayer;

        public ImportAssetPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            OnShow += () => EnableInClassList("active", true);
            OnHide += () => EnableInClassList("active", false);
            GoToAssetLibraryButton.RegisterCallback<ClickEvent>(OnOpenAssetLibrary);
            UploadButton.RegisterCallback<ClickEvent>(OnUploadStarted);
            ImportUriButton.RegisterCallback<ClickEvent>(OnImportUri);
            
            // TODO: Remove once we have fixed the copy/paste and credential flow in UI Toolkit
            ImportFromUrlButton.RegisterCallback<ClickEvent>(OnFileImportFromUrlStarted);
        }

        public override string GetTitle() => "Importeren";

        private void OnOpenAssetLibrary(ClickEvent evt) => OpenAssetLibrary?.Invoke(evt);
        private void OnFileImportFromUrlStarted(ClickEvent evt) => FileImportFromUrlStarted?.Invoke(evt);
        private void OnUploadStarted(ClickEvent evt) => FileUploadStarted?.Invoke(evt);

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
