using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    //do not change order for shapetype enum as it is stored in project files
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2,
        Grid = 3
    }

    public class PolygonCreationService : MonoBehaviour
    {
        public AreaSelection GridInput => gridInput;
        public PolygonInput PolygonInput => polygonInput;
        public PolygonInput LineInput => lineInput;
        
        [SerializeField] private AreaSelection gridInput;
        [SerializeField] private PolygonInput polygonInput;
        [SerializeField] private PolygonInput lineInput;

        [SerializeField] private float defaultLineWidth = 10.0f;
        [SerializeField] protected float maxSelectionDistanceFromCamera = 10000;
        private float lastTapTime = 0;
        private Vector2 previousFrameScreenCoordinate = default;
        
        private PolygonSelectionService polygonSelectionService;
        private InputService inputService;
        private ToolService toolService;

        [SerializeField] private BoolEvent OnBlockCameraDragging;

        private ShapeType currentShapeType = ShapeType.Undefined;
        private Plane worldPlane = new(Vector3.up, Vector3.zero);

        private void Start()
        {
            polygonSelectionService = ServiceLocator.GetService<PolygonSelectionService>();
            toolService = ServiceLocator.GetService<ToolService>();
            //we have to listen to inputservice after it is initialized
            inputService = ServiceLocator.GetService<InputService>();
            inputService.PolygonTapAction.performed += TapAction_performed;
            inputService.PolygonClickAction.performed += ClickAction_performed;
            inputService.PolygonClickAction.canceled += ClickAction_canceled;
            inputService.PolygonEscapeAction.canceled += EscapeAction_canceled;
            inputService.PolygonFinishAction.performed += FinishAction_performed;
            
            polygonInput.createdNewPolygonArea.AddListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.AddListener(UpdateLayer);
            lineInput.createdNewPolygonArea.AddListener(CreateLineLayer);
            lineInput.editedPolygonArea.AddListener(UpdateLayer);
            gridInput.whenAreaIsSelected.AddListener(CreateGridLayer);
            gridInput.whenAreaIsSelected.AddListener(EditGridLayer);
        }

        private void OnDestroy()
        {
            inputService.PolygonTapAction.performed -= TapAction_performed;
            inputService.PolygonClickAction.performed -= ClickAction_performed;
            inputService.PolygonClickAction.canceled -= ClickAction_canceled;
            inputService.PolygonEscapeAction.canceled -= EscapeAction_canceled;
            inputService.PolygonFinishAction.performed -= FinishAction_performed;
            
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdateLayer);
            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLayer);
            gridInput.whenAreaIsSelected.RemoveListener(CreateGridLayer);
            gridInput.whenAreaIsSelected.RemoveListener(EditGridLayer);
        }

        private void TapAction_performed(InputAction.CallbackContext obj)
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if(input == null) return;
            
            if (currentShapeType == ShapeType.Line || currentShapeType == ShapeType.Polygon)
            {
                if(App.UIRoot.IsUIClicked() || input.Mode == PolygonInput.DrawMode.Edit)
                    return;

                var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
                Vector3 currentPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
                input.SetSelectionCurrentPosition(currentPosition);

                if (input.DoubleClickToCloseLoop)
                {
                    if ((Time.time - lastTapTime) < input.DoubleClickTimer && Vector3.Distance(currentPointerPosition, previousFrameScreenCoordinate) < input.DoubleClickDistance)
                    {
                        Debug.Log("Double click, closing loop.");
                        input.CloseLoop(true);
                        return;
                    }
                    else
                    {
                        lastTapTime = Time.time;
                        previousFrameScreenCoordinate = currentPointerPosition;
                    }
                }

                input.AddPoint();
            }
            else if (currentShapeType == ShapeType.Grid)
            {
                if(App.UIRoot.IsUIClicked() || input.Mode == PolygonInput.DrawMode.Selected)
                    return;

                var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
                var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
                gridInput.DrawGridAtPosition(worldPosition, worldPosition);
            }
        }

        public void UpdateGridSelectionFromPoints(List<Vector3> points)
        {
            gridInput.SetAreaFromPolygon(points);
            gridInput.MakeSelection();
        }

        private void ClickAction_performed(InputAction.CallbackContext obj)
        {
            var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if(input == null) return;
            
            input.SetSelectionStartPosition(Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera));
        }

        private void ClickAction_canceled(InputAction.CallbackContext obj)
        {
            var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if(input == null) return;
            
            input.SetSelectionEndPosition(Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera));
        }

        private void EscapeAction_canceled(InputAction.CallbackContext obj)
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if(input == null) return;
            
            input.ClearPolygon(true);
        }

        private void FinishAction_performed(InputAction.CallbackContext obj)
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if(input == null) return;
            
            input.CloseLoop(true);
        }
        
        protected void Update()
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if(input == null) return;
            
            var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            input.SetSelectionCurrentPosition(worldPosition);
            
            if (currentShapeType == ShapeType.Line || currentShapeType == ShapeType.Polygon)
            {
                input.UpdatePreviewLine();

                if (input.Mode == PolygonInput.DrawMode.Edit) //dont auto draw in edit mode
                {
                    if (input.AutoDrawPolygon) // reset auto draw mode if mode changes to edit mode while auto drawing
                        input.AutoDrawPolygon = false;

                    return;
                }

                //if mode is not edit mode, we check if we are not auto drawing, but we are clicking and the auto draw modifier is pressed 
                if (!input.AutoDrawPolygon && inputService.PolygonClickAction.IsPressed() && inputService.PolygonModifierAction.IsPressed())
                    input.AutoDrawPolygon = true;
                else if (input.AutoDrawPolygon && !inputService.PolygonClickAction.IsPressed()) // reset auto draw mode
                    input.AutoDrawPolygon = false;

                if (!input.RequireReleaseBeforeRedraw && input.AutoDrawPolygon)
                    input.AutoAddPoint(previousFrameScreenCoordinate);
                else if (input.RequireReleaseBeforeRedraw && !inputService.PolygonClickAction.IsPressed())
                    input.RequireReleaseBeforeRedraw = false;
            }
            else if (currentShapeType == ShapeType.Grid)
            {
                gridInput.SetGridHighlightPosition(worldPosition);

                if (!gridInput.DrawingArea && inputService.PolygonClickAction.IsPressed() && inputService.PolygonModifierAction.IsPressed())
                {
                    if (App.UIRoot.IsUIClicked() || gridInput.Mode == PolygonInput.DrawMode.Selected) return;

                    gridInput.DrawingArea = true;
                    OnBlockCameraDragging.InvokeStarted(true);
                   
                }
                else if (gridInput.DrawingArea && !inputService.PolygonClickAction.IsPressed())
                {
                    gridInput.DrawingArea = false;
                    OnBlockCameraDragging.InvokeStarted(false);
                }
            }
        }

        private PolygonInput GetInputFromShapeType(ShapeType type)
        {
            switch (type)
            {
                case ShapeType.Polygon:
                    return polygonInput;
                case ShapeType.Line:
                    return lineInput;
                case ShapeType.Grid:
                    return gridInput;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Enable the proper line or poly input system based on layer type
        /// </summary>
        public void UpdateInputByType(LayerData layer)
        {
            if(layer == null) return;
            
            PolygonSelectionLayerPropertyData data = layer.GetProperty<PolygonSelectionLayerPropertyData>();
            EnablePolygonInputByType(data.ShapeType);
            var polygonAsUnityPoints = data.OriginalPolygon.ToUnityPositions().ToList();
            if(data.PolygonBoundingBox == null)
                return;

            switch (data.ShapeType)
            {
                case ShapeType.Polygon: polygonInput.SetPolygon(polygonAsUnityPoints); break;
                case ShapeType.Line: lineInput.SetPolygon(polygonAsUnityPoints); break;
                case ShapeType.Grid: gridInput.SetAreaFromPolygon(polygonAsUnityPoints); break;
                default:
                    Debug.LogError("Polygon shape type undefined, defaulting to PolygonInput");
                    polygonInput.gameObject.SetActive(true);
                    polygonInput.SetPolygon(polygonAsUnityPoints);
                    break;
            }
        }

        private void EnablePolygonInputByType(ShapeType type)
        {
            currentShapeType = type;
            polygonInput.gameObject.SetActive(currentShapeType == ShapeType.Polygon);
            lineInput.gameObject.SetActive(currentShapeType == ShapeType.Line);
            gridInput.gameObject.SetActive(currentShapeType == ShapeType.Grid);
        }
        
        public void ClearInputs()
        {
            //Clear inputs if no layer is selected by default
            lineInput.ClearPolygon(true);
            polygonInput.ClearPolygon(true);
            gridInput.SetSelectionVisualEnabled(false);
            currentShapeType =  ShapeType.Undefined;
        }

        private void CreatePolygonLayer(List<Vector3> unityPolygon)
        {
            var preset = new PolygonLayerPreset.Args(
                "Polygon",
                ShapeType.Polygon,
                unityPolygon.ToCoordinates().ToList()
            );
            
            var layer = App.Layers.Add(preset);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            polygonInput.OnHandleCreated.AddListener(RegisterBlockingCameraForHandle);
            SetPolygonToEdit();
        }

        private void UpdateLayer(List<Vector3> editedPolygon)
        {
            polygonSelectionService.SelectedLayer.GetProperty<PolygonSelectionLayerPropertyData>().OriginalPolygon = editedPolygon.ToCoordinates().ToList();
        }

        private void CreateLineLayer(List<Vector3> unityLine)
        {
            var preset = new PolygonLayerPreset.Args(
                "Line",
                ShapeType.Line,
                unityLine.ToCoordinates().ToList(),
                defaultLineWidth
            );
            
            var layer = App.Layers.Add(preset);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            lineInput.OnHandleCreated.AddListener(RegisterBlockingCameraForHandle);
            SetLineInputToEdit();
        }
        
        private void RegisterBlockingCameraForHandle(PolygonDragHandle handle)
        {
            handle.pointerDown.AddListener(()=> OnBlockCameraDragging.InvokeStarted(true));
            handle.clicked.AddListener(() => OnBlockCameraDragging.InvokeStarted(false));
            handle.endDrag.AddListener(() => OnBlockCameraDragging.InvokeStarted(false));
        }

        //called in the inspector
        public void CreateGridLayer(Bounds bounds)
        {
            if(gridInput.Mode != PolygonInput.DrawMode.Create && polygonSelectionService.SelectedLayer != null)
                return;
            
            Vector3 bottomLeft = new Vector3(bounds.min.x, 0, bounds.min.z);
            Vector3 topLeft = new Vector3(bounds.min.x, 0, bounds.max.z);
            Vector3 topRight = new Vector3(bounds.max.x, 0, bounds.max.z);
            Vector3 bottomRight = new Vector3(bounds.max.x, 0, bounds.min.z);

            var preset = new PolygonLayerPreset.Args(
                "Grid",
                ShapeType.Grid,
                new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(bottomRight), new Coordinate(topRight), new Coordinate(topLeft) }
            );
            
            var layer = App.Layers.Add(preset);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            SetGridInputModeToEdit();
        }

        public void EditGridLayer(Bounds bounds)
        {
            if(gridInput.Mode != PolygonInput.DrawMode.Edit && polygonSelectionService.SelectedLayer == null)
                return;
            
            Vector3 bottomLeft = new Vector3(bounds.min.x, 0, bounds.min.z);
            Vector3 topLeft = new Vector3(bounds.min.x, 0, bounds.max.z);
            Vector3 topRight = new Vector3(bounds.max.x, 0, bounds.max.z);
            Vector3 bottomRight = new Vector3(bounds.max.x, 0, bounds.min.z);

            PolygonSelectionLayerPropertyData data = polygonSelectionService.SelectedLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            var newPolygon = new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(bottomRight), new Coordinate(topRight), new Coordinate(topLeft)  };
            data.OriginalPolygon = newPolygon;
        }

        public void CancelLastCreatedGridLayer()
        {
            //for now this works because we only give the user the option to manipulate the current selected grid layer and nothing else until finishing 
            //within the polygon grid tool. But maybe this would be more safe to use a cached last created grid layer instead
            if(polygonSelectionService.SelectedLayer == null) return;
            
            App.Layers.Remove(polygonSelectionService.SelectedLayer);
        }

        public void SetPolygonToCreate()
        {
            polygonSelectionService.SelectedLayer?.DeselectLayer();
            EnablePolygonInputByType(ShapeType.Polygon);
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Create);
        }
        
        public void SetPolygonToEdit()
        {
            polygonSelectionService.SelectedLayer?.DeselectLayer();
            EnablePolygonInputByType(ShapeType.Polygon);
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputToCreate()
        {
            polygonSelectionService.SelectedLayer?.DeselectLayer();
            EnablePolygonInputByType(ShapeType.Line);
            lineInput.SetDrawMode(PolygonInput.DrawMode.Create);
        }

        public void SetLineInputToEdit()
        {
            polygonSelectionService.SelectedLayer?.DeselectLayer();
            EnablePolygonInputByType(ShapeType.Line);
            lineInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        public void SetGridInputModeToCreate()
        {
            polygonSelectionService.SelectedLayer?.DeselectLayer();
            EnablePolygonInputByType(ShapeType.Grid);
            gridInput.SetDrawMode(PolygonInput.DrawMode.Create);
        }

        public void SetGridInputModeToEdit()
        {
            EnablePolygonInputByType(ShapeType.Grid);
            gridInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        public void SetGridInputModeToSelected()
        {
            EnablePolygonInputByType(ShapeType.Grid);
            gridInput.SetDrawMode(PolygonInput.DrawMode.Selected);
        }
    }
}