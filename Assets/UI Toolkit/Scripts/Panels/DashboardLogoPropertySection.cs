using System;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Services;
using Netherlands3D.Twin.Configuration;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(DashboardCsvPropertyData), PropertySectionCategory.Settings)]
    public partial class DashboardLogoPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private readonly Button importButton;
        private readonly Label fileName;
        private readonly ErrorPanelContent errorPanel;
        private FileOpenService fileOpen;
        private Functionality dashboarding;
        private DashboardCsvPropertyData propertyData;
        private bool awaitingFile;

        public DashboardLogoPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            importButton = this.Q<Button>("ImportLogo");
            fileName = this.Q<Label>("LogoFileName");
            errorPanel = this.Q<ErrorPanelContent>();
            errorPanel.Hide();
            importButton.clicked += StartImport;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.Get<DashboardCsvPropertyData>();
            RefreshFileName();
            UpdateAvailability();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.destinationPanel.contextType != ContextType.Player)
                return;

            fileOpen = ServiceLocator.GetService<FileOpenService>();
            fileOpen.onFilesSelected.AddListener(ImportLogo);
            dashboarding = Configurator.Dashboarding;
            dashboarding.OnEnableFunctionality.AddListener(UpdateAvailability);
            dashboarding.OnDisableFunctionality.AddListener(UpdateAvailability);
            UpdateAvailability();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            awaitingFile = false;
            if (fileOpen != null)
                fileOpen.onFilesSelected.RemoveListener(ImportLogo);
            if (dashboarding != null)
            {
                dashboarding.OnEnableFunctionality.RemoveListener(UpdateAvailability);
                dashboarding.OnDisableFunctionality.RemoveListener(UpdateAvailability);
            }
        }

        private void UpdateAvailability()
        {
            var available = Configurator.IsDashboardingEnabled && propertyData != null;
            EnableInClassList(UtilityClassConstants.HIDDEN, !available);
            if (!available)
                awaitingFile = false;
        }

        private void RefreshFileName()
        {
            var hasLogo = propertyData.LogoFile != null;
            fileName.text = hasLogo ? Path.GetFileName(propertyData.LogoFile.LocalPath) : "Geen logo toegevoegd.";
            importButton.LabelText = hasLogo ? "Logo vervangen" : "Logo toevoegen";
        }

        private void StartImport()
        {
            if (!Configurator.IsDashboardingEnabled || propertyData == null)
                return;

            errorPanel.Hide();
            awaitingFile = true;
            fileOpen.OpenFile("png,jpg");
        }

        private void ImportLogo(string path)
        {
            if (!awaitingFile)
                return;

            awaitingFile = false;
            if (!Configurator.IsDashboardingEnabled || propertyData == null || string.IsNullOrEmpty(path))
                return;

            path = path.TrimEnd(',');
            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase))
            {
                errorPanel.Show();
                return;
            }

            var texture = new Texture2D(2, 2);
            try
            {
                var uri = AssetUriFactory.CreateProjectAssetUri(path);
                if (!texture.LoadImage(File.ReadAllBytes(AssetUriFactory.GetLocalPath(uri)), true))
                {
                    errorPanel.Show();
                    return;
                }

                propertyData.LogoFile = uri;
                RefreshFileName();
                errorPanel.Hide();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or UnityException)
            {
                errorPanel.Show();
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }
        }
    }
}
