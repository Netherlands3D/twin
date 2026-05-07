using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.EventSystems;
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
        
        private PolygonSelectionService polygonSelectionService;
        private InputService inputService;

        [SerializeField] private TriggerEvent OnGridCreate;
        [SerializeField] private TriggerEvent OnGridEdit;
        [SerializeField] private TriggerEvent OnGridSelect;

        private ShapeType currentShapeType = ShapeType.Undefined;

        private void OnEnable()
        {
            polygonInput.createdNewPolygonArea.AddListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.AddListener(UpdateLayer);

            lineInput.createdNewPolygonArea.AddListener(CreateLineLayer);
            lineInput.editedPolygonArea.AddListener(UpdateLayer);

            gridInput.whenAreaIsSelected.AddListener(CreateOrEditGridLayer);
            
            OnGridCreate.AddListenerStarted(SetGridInputModeToCreate);
            OnGridEdit.AddListenerStarted(SetGridInputModeToEdit);
            OnGridSelect.AddListenerStarted(SetGridInputModeToSelected);
            
            inputService = ServiceLocator.GetService<InputService>();
            inputService.PolygonTapAction.performed -= TapAction_performed;
            inputService.PolygonClickAction.performed -= ClickAction_performed;
            inputService.PolygonClickAction.canceled -= ClickAction_canceled;
            inputService.PolygonEscapeAction.canceled -= EscapeAction_canceled;
            inputService.PolygonFinishAction.performed -= FinishAction_performed;
        }

        private void OnDisable()
        {
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdateLayer);

            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLayer);

            gridInput.whenAreaIsSelected.RemoveListener(CreateOrEditGridLayer);
            
            OnGridCreate.RemoveListenerStarted(SetGridInputModeToCreate);
            OnGridEdit.RemoveListenerStarted(SetGridInputModeToEdit);
            OnGridSelect.RemoveListenerStarted(SetGridInputModeToSelected);
           
            inputService.PolygonTapAction.performed -= TapAction_performed;
            inputService.PolygonClickAction.performed -= ClickAction_performed;
            inputService.PolygonClickAction.canceled -= ClickAction_canceled;
            inputService.PolygonEscapeAction.canceled -= EscapeAction_canceled;
            inputService.PolygonFinishAction.performed -= FinishAction_performed;
            
        }

        private void Start()
        {
            polygonSelectionService = ServiceLocator.GetService<PolygonSelectionService>();
        }
        
        private void TapAction_performed(InputAction.CallbackContext obj)
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if (currentShapeType == ShapeType.Line || currentShapeType == ShapeType.Polygon)
            {
                if (input.Mode == PolygonInput.DrawMode.Edit)
                {
                    Debug.LogWarning("PolygonInput is in edit mode, cannot Add a new point in edit mode.", gameObject);
                    return;
                }

                if(ServiceLocator.GetService<ContextMenuBehaviour>().IsUIClicked())
                    return;

                var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
                Vector3 currentPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
                input.SetSelectionCurrentPosition(currentPosition);

                if (doubleClickToCloseLoop)
                {
                    if ((Time.time - lastTapTime) < doubleClickTimer && Vector3.Distance(currentPointerPosition, previousFrameScreenCoordinate) < doubleClickDistance)
                    {
                        Debug.Log("Double click, closing loop.");
                        CloseLoop(true);
                        return;
                    }
                    else
                    {
                        lastTapTime = Time.time;
                        previousFrameScreenCoordinate = currentPointerPosition;
                    }
                }

                AddPoint(selectionCurrentPosition);
            }
            else if (currentShapeType == ShapeType.Grid)
            {
                if (Interface.PointerIsOverUI())
                    return;

                var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
                var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane,
                    maxSelectionDistanceFromCamera);
                var tappedPosition = GetGridPosition(worldPosition);
                gridInput.DrawSelectionArea(tappedPosition, tappedPosition);
                MakeSelection();
            }
        }

        private void ClickAction_performed(InputAction.CallbackContext obj)
        {
            var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            input.SetSelectionStartPosition(Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera));
        }

        private void ClickAction_canceled(InputAction.CallbackContext obj)
        {
            var currentPointerPosition = inputService.PolygonPointerAction.ReadValue<Vector2>();
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            input.SetSelectionEndPosition(Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera));
        }

        private void EscapeAction_canceled(InputAction.CallbackContext obj)
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            input.ClearPolygon(true);
        }

        private void FinishAction_performed(InputAction.CallbackContext obj)
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            input.CloseLoop(true);
        }
        
        protected void Update()
        {
            PolygonInput input = GetInputFromShapeType(currentShapeType);
            if (currentShapeType == ShapeType.Undefined || currentShapeType == ShapeType.Undefined)
            {

                UpdateCurrentWorldCoordinate();
                UpdatePreviewLine();

                if (pointerRepresentation)
                    pointerRepresentation.position = selectionCurrentPosition;

                if (mode == DrawMode.Edit) //dont auto draw in edit mode
                {
                    if (autoDrawPolygon) // reset auto draw mode if mode changes to edit mode while auto drawing
                    {
                        autoDrawPolygon = false;
                        blockCameraDrag.Invoke(false);
                    }

                    return;
                }

                //if mode is not edit mode, we check if we are not auto drawing, but we are clicking and the auto draw modifier is pressed 
                if (!autoDrawPolygon && clickAction.IsPressed() && modifierAction.IsPressed())
                {
                    autoDrawPolygon = true;
                    blockCameraDrag.Invoke(true);
                }
                else if (autoDrawPolygon && !clickAction.IsPressed()) // reset auto draw mode
                {
                    autoDrawPolygon = false;
                    blockCameraDrag.Invoke(false);
                }

                if (!requireReleaseBeforeRedraw && autoDrawPolygon)
                {
                    AutoAddPoint();
                }
                else if (requireReleaseBeforeRedraw && !clickAction.IsPressed())
                {
                    requireReleaseBeforeRedraw = false;
                }

                previousFrameWorldCoordinate = selectionCurrentPosition;
            }
            else if (currentShapeType == ShapeType.Grid)
            {
                var currentPointerPosition = pointerAction.ReadValue<Vector2>();
                var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
                var currentWorldCoordinate = GetGridPosition(worldPosition);
                gridHighlight.transform.position = currentWorldCoordinate;

                if (!drawingArea && clickAction.IsPressed() && modifierAction.IsPressed())
                {
                    if (Interface.PointerIsOverUI() || mode == DrawMode.Selected) return;

                    drawingArea = true;
                    SetSelectionVisualEnabled(true);
                    blockCameraDragging.Invoke(true);
                }
                else if (drawingArea && !clickAction.IsPressed())
                {
                    drawingArea = false;
                    blockCameraDragging.Invoke(false);
                }

                if (drawingArea)
                {
                    DrawSelectionArea(selectionStartPosition, currentWorldCoordinate);
                }
            }
        }

        private PolygonInput GetInputFromShapeType(ShapeType type)
        {
            switch (currentShapeType)
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
            switch (type)
            {
                case ShapeType.Undefined: break;
                case ShapeType.Polygon: polygonInput.gameObject.SetActive(true); break;
                case ShapeType.Line: lineInput.gameObject.SetActive(true); break;
                case ShapeType.Grid: gridInput.gameObject.SetActive(true); break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        public void ClearInputs()
        {
            //Clear inputs if no layer is selected by default
            lineInput.ClearPolygon(true);
            polygonInput.ClearPolygon(true);
            gridInput.SetSelectionVisualEnabled(false);
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
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        private void UpdateLayer(List<Vector3> editedPolygon)
        {
            polygonSelectionService.ActiveLayer.GetProperty<PolygonSelectionLayerPropertyData>().OriginalPolygon = editedPolygon.ToCoordinates().ToList();
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
            lineInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        //called in the inspector
        public void CreateOrEditGridLayer(Bounds bounds)
        {
            Vector3 bottomLeft = new Vector3(bounds.min.x, 0, bounds.min.z);
            Vector3 topLeft = new Vector3(bounds.min.x, 0, bounds.max.z);
            Vector3 topRight = new Vector3(bounds.max.x, 0, bounds.max.z);
            Vector3 bottomRight = new Vector3(bounds.max.x, 0, bounds.min.z);

            PolygonSelectionLayerPropertyData data = polygonSelectionService.ActiveLayer?.GetProperty<PolygonSelectionLayerPropertyData>();

            //is the current selected layer already a grid and the current input mode is not selected, then we can adjust the polygon
            if (data?.ShapeType == ShapeType.Grid && gridInput.Mode != PolygonInput.DrawMode.Selected)
            {
                var newPolygon = new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(bottomRight), new Coordinate(topRight), new Coordinate(topLeft)  };
                data.OriginalPolygon = newPolygon;
                return;
            }

            var preset = new PolygonLayerPreset.Args(
                "Grid",
                ShapeType.Grid,
                new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(bottomRight), new Coordinate(topRight), new Coordinate(topLeft) }
            );
            
            var layer = App.Layers.Add(preset);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            OnGridEdit.InvokeStarted();
        }

        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            polygonSelectionService.ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Polygon);
            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputModeToCreate(bool isCreateMode)
        {
            polygonSelectionService.ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Line);
            lineInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetGridInputModeToCreate()
        {
            polygonSelectionService.ActiveLayer?.DeselectLayer();
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