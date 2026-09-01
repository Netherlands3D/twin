using System;
using System.Collections.Generic;
using System.IO;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(AnnotationPropertyData), PropertySectionCategory.Information)]
    public partial class AnnotationInformationPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private const int MaxThumbnailDimension = 512;
        private const float ThumbnailHeight = 160.0f;

        private readonly List<AnnotationInformationItem> empty = new() { new AnnotationInformationItem("Geen informatie", "") };

        private AnnotationPropertyData annotationPropertyData;
        private VisualElement thumbnailContainer;
        private Label emptyThumbnailLabel;
        private ListView informationListView;
        private string currentImagePath;
        private UnityAction<string> updateInformationListener;
        private UnityAction<string> updateThumbnailListener;

        public AnnotationInformationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            thumbnailContainer = this.Q<VisualElement>("ThumbnailContainer");
            emptyThumbnailLabel = this.Q<Label>("EmptyThumbnailLabel");
            informationListView = this.Q<ListView>();

            informationListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            informationListView.selectionType = SelectionType.None;
            informationListView.makeItem = MakeListViewItem;
            informationListView.bindItem = BindListViewItem;

            updateInformationListener = _ => UpdateInformation();
            updateThumbnailListener = UpdateThumbnail;

            RegisterCallback<DetachFromPanelEvent>(_ => UnregisterPropertyListeners());
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            annotationPropertyData = properties.Get<AnnotationPropertyData>();
            if (annotationPropertyData == null)
            {
                Clear();
                return;
            }

            annotationPropertyData.OnAnnotationTextChanged.AddListener(updateInformationListener);
            annotationPropertyData.OnImagePathChanged.AddListener(updateInformationListener);
            annotationPropertyData.OnImagePreviewPathChanged.AddListener(updateThumbnailListener);
            annotationPropertyData.OnImagePreviewPathChanged.AddListener(updateInformationListener);
            annotationPropertyData.OnImageCaptionChanged.AddListener(updateInformationListener);

            UpdateThumbnail(annotationPropertyData.ImagePreviewPath);
            UpdateInformation();
        }

        private void UnregisterPropertyListeners()
        {
            currentImagePath = null;

            if (annotationPropertyData == null) return;

            annotationPropertyData.OnAnnotationTextChanged.RemoveListener(updateInformationListener);
            annotationPropertyData.OnImagePathChanged.RemoveListener(updateInformationListener);
            annotationPropertyData.OnImagePreviewPathChanged.RemoveListener(updateThumbnailListener);
            annotationPropertyData.OnImagePreviewPathChanged.RemoveListener(updateInformationListener);
            annotationPropertyData.OnImageCaptionChanged.RemoveListener(updateInformationListener);
        }

        private VisualElement MakeListViewItem()
        {
            var keyValue = new KeyValue();
            keyValue.ShowDivider(true);

            var linkRow = new VisualElement();
            linkRow.AddToClassList("annotation-information-link__row");

            var linkKey = new Label { name = "LinkKey" };
            linkKey.AddToClassList("annotation-information-link__label");
            linkRow.Add(linkKey);

            var linkButton = new VisualElement();
            linkButton.AddToClassList("annotation-information-link__button");
            linkButton.Add(new Icon { Image = "Link" });
            linkButton.Add(new Hyperlink());
            linkRow.Add(linkButton);

            var listViewItem = new ListViewItem();
            listViewItem.Add(keyValue);
            listViewItem.Add(linkRow);
            return listViewItem;
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (informationListView.itemsSource[index] is not AnnotationInformationItem informationItem) return;

            var keyValue = listViewItem.Q<KeyValue>();
            var linkRow = listViewItem.Q<VisualElement>(className: "annotation-information-link__row");
            bool showLink = informationItem.IsUrl;

            keyValue.EnableInClassList(UtilityClassConstants.HIDDEN, showLink);
            linkRow.EnableInClassList(UtilityClassConstants.HIDDEN, !showLink);

            if (!showLink)
            {
                keyValue.Key = informationItem.Key;
                keyValue.Value = informationItem.Value;
                return;
            }

            linkRow.Q<Label>("LinkKey").text = informationItem.Key;
            var hyperlink = linkRow.Q<Hyperlink>();
            hyperlink.text = informationItem.LinkText;
            hyperlink.url = informationItem.LinkUrl;
            hyperlink.tooltip = informationItem.Value;
        }

        private void UpdateInformation()
        {
            if (annotationPropertyData == null)
            {
                PopulateInformation(empty);
                return;
            }

            var information = new List<AnnotationInformationItem>();
            AddIfNotEmpty(information, "Tekst", annotationPropertyData.AnnotationText);
            AddIfNotEmpty(information, "Bijschrift", annotationPropertyData.ImageCaption);
            AddIfNotEmpty(information, "Afbeelding", annotationPropertyData.ImagePath, true);
            AddIfNotEmpty(information, "Thumbnail", annotationPropertyData.ImagePreviewPath, true);

            PopulateInformation(information.Count == 0 ? empty : information);
        }

        private void PopulateInformation(List<AnnotationInformationItem> information)
        {
            informationListView.itemsSource = information;
            informationListView.RefreshItems();
        }

        private static void AddIfNotEmpty(List<AnnotationInformationItem> target, string key, string value, bool showAsLink = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            target.Add(new AnnotationInformationItem(key, value, showAsLink));
        }

        private void UpdateThumbnail(string imagePath)
        {
            currentImagePath = imagePath;

            if (string.IsNullOrWhiteSpace(imagePath))
            {
                ClearThumbnail();
                return;
            }

            if (TextureThumbnailUtility.TryGetCachedThumbnail(imagePath, out Texture2D cachedThumbnail))
            {
                ApplyThumbnail(cachedThumbnail);
                return;
            }

            LoadThumbnail(imagePath);
        }

        private void LoadThumbnail(string imagePath)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(GetRequestPath(imagePath), true);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            operation.completed += _ =>
            {
                if (currentImagePath != imagePath)
                {
                    request.Dispose();
                    return;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Failed to load annotation inspector image: " + request.error);
                    ClearThumbnail();
                    request.Dispose();
                    return;
                }

                Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(request);
                Texture2D thumbnail = TextureThumbnailUtility.CreateThumbnail(downloadedTexture, MaxThumbnailDimension, "Annotation Inspector Thumbnail");
                if (thumbnail != downloadedTexture)
                    UnityEngine.Object.Destroy(downloadedTexture);

                TextureThumbnailUtility.CacheThumbnail(imagePath, thumbnail);
                ApplyThumbnail(thumbnail);
                request.Dispose();
            };
        }

        private void ApplyThumbnail(Texture2D texture)
        {
            if (texture == null)
            {
                ClearThumbnail();
                return;
            }

            emptyThumbnailLabel.EnableInClassList(UtilityClassConstants.HIDDEN, true);
            thumbnailContainer.style.backgroundImage = new StyleBackground(texture);
            thumbnailContainer.style.height = ThumbnailHeight;
            thumbnailContainer.schedule.Execute(_ =>
            {
                float maxWidth = thumbnailContainer.parent?.resolvedStyle.width ?? 0;
                float width = ThumbnailHeight * texture.width / texture.height;
                if (maxWidth > 0)
                    width = Mathf.Min(width, maxWidth);

                thumbnailContainer.style.width = width;
            });
        }

        private void Clear()
        {
            ClearThumbnail();
            PopulateInformation(empty);
        }

        private void ClearThumbnail()
        {
            thumbnailContainer.style.backgroundImage = null;
            thumbnailContainer.style.height = 0;
            thumbnailContainer.style.width = StyleKeyword.Auto;
            emptyThumbnailLabel.EnableInClassList(UtilityClassConstants.HIDDEN, false);
        }

        private static string GetRequestPath(string path)
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out Uri uri) && !string.IsNullOrWhiteSpace(uri.Scheme))
                return path;

            if (Path.IsPathRooted(path))
                return new Uri(path).AbsoluteUri;

            return path;
        }

        private readonly struct AnnotationInformationItem
        {
            public string Key { get; }
            public string Value { get; }
            public bool IsUrl { get; }
            public string LinkUrl => IsUrl ? GetRequestPath(Value) : "";
            public string LinkText => Key == "Thumbnail" ? "Open thumbnail" : "Open afbeelding";

            public AnnotationInformationItem(string key, string value, bool showAsLink = false)
            {
                Key = key;
                Value = value;
                IsUrl = showAsLink && !string.IsNullOrWhiteSpace(value);
            }
        }
    }
}
