using System.Collections.Generic;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotationLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private TextPopout popoutPrefab;
        private Tool layerTool;

        private TextPopout annotation;

        private enum EditMode
        {
            Disabled,
            Move,
            TextEdit
        }

        private EditMode mode = EditMode.Disabled;

        public override BoundingBox Bounds => new BoundingBox(
            new Coordinate(transform.position - 5 * Vector3.one),
            new Coordinate(transform.position + 5 * Vector3.one));

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            layerTool = ServiceLocator.GetService<ToolService>().GetTool(ToolType.Layer);
            CreateTextPopup();

            WorldInteractionBlocker.ClickedOnBlocker.AddListener(OnBlockerClicked);
        }

        private void OnBlockerClicked()
        {
            if (mode == EditMode.TextEdit)
                SetEditMode(EditMode.Move);
        }

        private void CreateTextPopup()
        {
            Canvas canvas = CanvasID.GetCanvasByType(CanvasType.World);

            annotation = Instantiate(popoutPrefab, canvas.transform);
            annotation.RectTransform().SetPivot(PivotPresets.BottomCenter);
            annotation.SetSnappingSide(TextPopout.SnappingSide.Above);
            annotation.transform.SetSiblingIndex(1);
            annotation.ReadOnly = !layerTool.IsOpen;
        }

        private void OnDestroy()
        {
            if (annotation != null)
                Destroy(annotation.gameObject);
        }

        private void OnAnnotationSelected()
        {
            if (!layerTool.IsOpen)
                return;

            SetEditMode(EditMode.Move);
        }

        private void OnAnnotationDoubleClicked()
        {
            if (!layerTool.IsOpen)
            {
                layerTool.Open();
                SetEditMode(EditMode.Move);
            }
            else
            {
                SetEditMode(EditMode.TextEdit);
            }
        }

        private void OnAnnotationTextConfirmed()
        {
            SetEditMode(EditMode.Move);
        }

        private void SetEditMode(EditMode newMode)
        {
            mode = newMode;
            switch (mode)
            {
                case EditMode.Disabled:
                    annotation.ReadOnly = true;
                    annotation.SelectableText = true;
                    LayerData.DeselectLayer();
                    WorldInteractionBlocker.ReleaseBlocker(this);
                    break;

                case EditMode.Move:
                    annotation.ReadOnly = true;
                    annotation.SelectableText = false;
                    LayerData.SelectLayer(true);
                    WorldInteractionBlocker.ReleaseBlocker(this);
                    break;

                case EditMode.TextEdit:
                    annotation.ReadOnly = false;
                    annotation.SelectableText = true;
                    LayerData.SelectLayer(true);
                    WorldInteractionBlocker.AddBlocker(this);
                    ClearTransformHandles();
                    break;
            }
        }

        private void SetPropertyDataText(string annotationText)
        {
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.AnnotationText = annotationText;
        }

        protected override void Update()
        {
            base.Update();
            annotation.StickTo(WorldTransform.Coordinate);
        }

        public override void ApplyStyling()
        {
            base.ApplyStyling();
            List<LayerFeature> features = CreateFeaturesByType<Image>(annotation.gameObject);
            foreach (var feature in features)
            {
                if (feature.Geometry is not Image image) continue;

                Symbolizer styling = GetStyling(feature);
                var fillColor = styling.GetFillColor();

                if (!fillColor.HasValue) continue;

                image.color = fillColor.Value;
            }
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);
            InitProperty<AnnotationPropertyData>(properties, null, "", "", "");
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            AnnotationPropertyData annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();

            annotation.Show(annotationPropertyData.AnnotationText, WorldTransform.Coordinate, true);
            UpdateAnnotation(annotationPropertyData.AnnotationText);
            UpdateAnnotationImage(annotationPropertyData.ImagePreviewPath);
            UpdateAnnotationImageCaption(annotationPropertyData.ImageCaption);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.OnAnnotationTextChanged.AddListener(UpdateAnnotation);
            annotationPropertyData.OnImagePreviewPathChanged.AddListener(UpdateAnnotationImage);
            annotationPropertyData.OnImageCaptionChanged.AddListener(UpdateAnnotationImageCaption);

            annotation.OnEndEdit.AddListener(SetPropertyDataText);
            annotation.TextFieldSelected.AddListener(OnAnnotationSelected);
            annotation.TextFieldDoubleClicked.AddListener(OnAnnotationDoubleClicked);
            annotation.TextFieldInputConfirmed.AddListener(OnAnnotationTextConfirmed);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.OnAnnotationTextChanged.RemoveListener(UpdateAnnotation);
            annotationPropertyData.OnImagePreviewPathChanged.RemoveListener(UpdateAnnotationImage);
            annotationPropertyData.OnImageCaptionChanged.RemoveListener(UpdateAnnotationImageCaption);

            annotation.OnEndEdit.RemoveListener(SetPropertyDataText);
            annotation.TextFieldSelected.RemoveListener(OnAnnotationSelected);
            annotation.TextFieldDoubleClicked.RemoveListener(OnAnnotationDoubleClicked);
            annotation.TextFieldInputConfirmed.RemoveListener(OnAnnotationTextConfirmed);

            WorldInteractionBlocker.ClickedOnBlocker.RemoveListener(OnBlockerClicked);
        }

        private void UpdateAnnotationImage(string path)
        {
            annotation.SetImageFromPath(path);
        }

        private void UpdateAnnotationImageCaption(string caption)
        {
            annotation.SetImageCaption(caption);
        }

        private void UpdateAnnotation(string newText)
        {
            annotation.SetTextWithoutNotify(newText);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            annotation.gameObject.SetActive(isActive);
        }

        public AnnotationPropertyData GetAnnotationPropertyData()
        {
            return LayerData.GetProperty<AnnotationPropertyData>();
        }

        public void InitializeFromImportedData(Coordinate coordinate, string text, string imageUrl, string imageCaption)
        {
            InitializeFromImportedData(coordinate, text, imageUrl, imageUrl, imageCaption);
        }

        public void InitializeFromImportedData(Coordinate coordinate, string text, string imageUrl, string imagePreviewUrl, string imageCaption)
        {
            WorldTransform.MoveToCoordinate(coordinate);

            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.AnnotationText = text ?? "";
            annotationPropertyData.ImagePath = imageUrl ?? "";
            annotationPropertyData.ImagePreviewPath = imagePreviewUrl ?? imageUrl ?? "";
            annotationPropertyData.ImageCaption = imageCaption ?? "";

            if (annotation != null)
            {
                annotation.Show(annotationPropertyData.AnnotationText, WorldTransform.Coordinate, true);
                UpdateAnnotation(annotationPropertyData.AnnotationText);
                UpdateAnnotationImage(annotationPropertyData.ImagePreviewPath);
                UpdateAnnotationImageCaption(annotationPropertyData.ImageCaption);
            }
        }
    }
}
