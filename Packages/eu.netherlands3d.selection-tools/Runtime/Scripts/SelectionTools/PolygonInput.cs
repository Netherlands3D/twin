/*
 *  Copyright (C) X Gemeente
 *                X Amsterdam
 *                X Economic Services Departments
 *
 *  Licensed under the EUPL, Version 1.2 or later (the "License");
 *  You may not use this work except in compliance with the License.
 *  You may obtain a copy of the License at:
 *
 *    https://github.com/Amsterdam/3DAmsterdam/blob/master/LICENSE.txt
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" basis,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 *  implied. See the License for the specific language governing
 *  permissions and limitations under the License.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Netherlands3D.SelectionTools
{
    [RequireComponent(typeof(LineRenderer))]
    public class PolygonInput : MonoBehaviour
    {
        [System.Serializable]
        public enum WindingOrder
        {
            CLOCKWISE,
            COUNTERCLOCKWISE,
            IGNORE
        }

        public enum DrawMode
        {
            CreateAndEdit,
            Create,
            Edit
        }

        [Header("Input")] 
        [SerializeField] private InputActionAsset inputActionAsset;
        private InputActionMap polygonSelectionActionMap;

        [Header("Settings")] 
        [SerializeField] Color lineColor = Color.red;
        [SerializeField] Color closedLoopLineColor = Color.red;
        [SerializeField] private float lineWidthMultiplier = 10.0f;
        [SerializeField] private float maxSelectionDistanceFromCamera = 10000;
        [SerializeField] private bool snapToStart = true;
        [SerializeField, Tooltip("Closing a polygon shape is required. If set to false, you can output lines.")] private bool requireClosedPolygon = true;
        [SerializeField, Tooltip("If you click close to the starting point the loop will finish")] private bool closeLoopAtStartPoint = true;
        [SerializeField, Tooltip("Handles allow you to transform the line.")] private bool createHandles = false;
        [SerializeField] private int maxPoints = 1000;
        [SerializeField] private int minPointsToCloseLoop = 3;
        [SerializeField] private float minDistanceBetweenAutoPoints = 10.0f;
        [SerializeField] private float minDirectionThresholdForAutoPoints = 0.8f;
        [SerializeField] private WindingOrder windingOrder = WindingOrder.CLOCKWISE;
        [SerializeField] private bool doubleClickToCloseLoop = true;
        [SerializeField] private float doubleClickTimer = 0.5f;
        [SerializeField] private float doubleClickDistance = 10.0f;
        [SerializeField] private bool displayLineUntilRedraw = true;
        [SerializeField] private bool clearOnEnable = false;
        [SerializeField] private LayerMask lockInputLayers = 32; //UI layer 5th bit is a 1
        [SerializeField] private DrawMode mode = DrawMode.CreateAndEdit;
        public DrawMode Mode => mode;

        private InputAction pointerAction;
        private InputAction tapAction;
        private InputAction escapeAction;
        private InputAction finishAction;
        private InputAction tapSecondaryAction;
        private InputAction clickAction;
        private InputAction modifierAction;

        [SerializeField] private LineRenderer polygonLineRenderer;
        [SerializeField] private LineRenderer previewLineRenderer;

        public Vector3 currentWorldCoordinate = default;
        public List<Vector3> positions = new List<Vector3>();

        protected Vector3 lastAddedPoint = default;
        protected Vector3 selectionStartPosition = default;
        protected Vector3 previousFrameWorldCoordinate = default;
        protected Vector2 previousFrameScreenCoordinate = default;
        protected Vector3 lastNormal = Vector3.zero;
        protected Plane worldPlane;

        private bool closedLoop = false;
        private bool snappingToStartPoint = false;
        private bool polygonFinished = false;
        private bool previewLineCrossed = false;
        private bool autoDrawPolygon = false;
        private bool requireReleaseBeforeRedraw = false;
        private Camera mainCamera;

        private float lastTapTime = 0;

        [SerializeField] private Transform pointerRepresentation;
        public bool showPointerInEditMode = false;
        public bool showPointerInCreateMode = true;
        public bool showPointerInCreateAndEditMode = true;
        [SerializeField] private PolygonDragHandle handleTemplate;
        private List<PolygonDragHandle> handles = new List<PolygonDragHandle>();

        [Header("Invoke")] 
        public UnityEvent<bool> blockCameraDrag;
        public UnityEvent<List<Vector3>> createdNewPolygonArea;
        [Header("Optional Invoke")] 
        public UnityEvent<List<Vector3>> editedPolygonArea;

        [Tooltip("Contains the list of points the line is made of")] public UnityEvent<List<Vector3>> previewLineHasChanged;

        void Awake()
        {
            mainCamera = Camera.main;
            polygonLineRenderer.startColor = polygonLineRenderer.endColor = lineColor;
            polygonLineRenderer.widthMultiplier = lineWidthMultiplier;
            polygonLineRenderer.positionCount = 1;
            polygonLineRenderer.loop = false;

            polygonSelectionActionMap = inputActionAsset.FindActionMap("PolygonSelection");
            tapAction = polygonSelectionActionMap.FindAction("Tap");
            escapeAction = polygonSelectionActionMap.FindAction("Escape");
            finishAction = polygonSelectionActionMap.FindAction("Finish");
            tapSecondaryAction = polygonSelectionActionMap.FindAction("TapSecondary");
            clickAction = polygonSelectionActionMap.FindAction("Click");
            pointerAction = polygonSelectionActionMap.FindAction("Point");
            modifierAction = polygonSelectionActionMap.FindAction("Modifier");

            worldPlane = new Plane(this.transform.up, this.transform.position);

            if (handleTemplate)
            {
                handleTemplate.gameObject.SetActive(false);
            }
            else
            {
                if (createHandles) Debug.Log("Please set a handleTemplate reference to create handles.", this.gameObject);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (createHandles && doubleClickToCloseLoop)
            {
                //Second clicks would select the handle, so auto disable double click
                Debug.Log("Disabled double click to close loop. This is not allowed in combination with handles.");
                doubleClickToCloseLoop = false;
            }

            SetDrawMode(mode); //set pointer active according to mode
        }
#endif

        protected virtual void OnEnable()
        {
            if (clearOnEnable)
            {
                ClearPolygon(true);
            }

            tapAction.performed += TapAction_performed;
            clickAction.performed += ClickAction_performed;
            clickAction.canceled += ClickAction_canceled;
            escapeAction.canceled += EscapeAction_canceled;
            finishAction.performed += FinishAction_performed;

            polygonSelectionActionMap.Enable(); //make sure to look at the execution order in case there are multiple Scripts that make use of this ActionMap, OnDisable() of another object might be called after this function, resulting in no inputs being registered
        }

        protected virtual void OnDisable()
        {
            blockCameraDrag.Invoke(false);

            tapAction.performed -= TapAction_performed;
            clickAction.performed -= ClickAction_performed;
            clickAction.canceled -= ClickAction_canceled;
            escapeAction.canceled -= EscapeAction_canceled;
            finishAction.performed -= FinishAction_performed;

            polygonSelectionActionMap.Disable();
        }

        private void TapAction_performed(InputAction.CallbackContext obj)
        {
            Tap();
        }

        private void ClickAction_performed(InputAction.CallbackContext obj)
        {
            StartClick();
        }

        private void ClickAction_canceled(InputAction.CallbackContext obj)
        {
            Release();
        }

        private void EscapeAction_canceled(InputAction.CallbackContext obj)
        {
            ClearPolygon(true);
        }

        private void FinishAction_performed(InputAction.CallbackContext obj)
        {
            CloseLoop(true);
        }

        public void ReselectPolygon(List<Vector3> points)
        {
            if (mode == DrawMode.Create)
            {
                Debug.LogWarning("PolygonInput is in create mode, cannot reselect polygon in edit mode.", gameObject);
                return;
            }

            ClearPolygon(true);
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 point = points[i];
                if (i == points.Count - 1)
                {
                    if (point == points[0])
                        continue;
                }

                AddPoint(point, false);
            }

            CloseLoop(false);
        }

        protected virtual void Update()
        {
            UpdateCurrentWorldCoordinate();

            UpdatePreviewLine();

            if (pointerRepresentation)
                pointerRepresentation.position = currentWorldCoordinate;

            if (!autoDrawPolygon && clickAction.IsPressed() && modifierAction.IsPressed())
            {
                autoDrawPolygon = true;
                blockCameraDrag.Invoke(true);
            }
            else if (autoDrawPolygon && !clickAction.IsPressed())
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

            previousFrameWorldCoordinate = currentWorldCoordinate;
        }

        protected virtual void UpdateCurrentWorldCoordinate()
        {
            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            currentWorldCoordinate = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
        }

        private float GetMinPointDistance(int handleIndex)
        {
            if (handles.Count > handleIndex)
            {
                var handle = handles[handleIndex];
                return handle.transform.lossyScale.magnitude * 0.5f; //todo: This code does not take the handle shape, or handle mesh bounds into account: should replace 0.5f with a multiplication of something like handle.GetComponent<MeshFilter>().mesh.bounds.size.magnitude;
            }

            return 0;
        }

        /// <summary>
        /// Draw the preview line between last placed point and current pointer position
        /// </summary>
        private void UpdatePreviewLine()
        {
            if (positions.Count == 0 || closedLoop)
                return;

            snappingToStartPoint = false;
            if (snapToStart && positions.Count > 2)
            {
                if (Vector3.Distance(currentWorldCoordinate, positions[0]) < GetMinPointDistance(0))
                {
                    currentWorldCoordinate = positions[0];
                    snappingToStartPoint = true;
                }
            }

            var previewLineFirstPoint = positions[positions.Count - 1];
            var previewLineLastPoint = currentWorldCoordinate;
            previewLineRenderer.SetPosition(0, previewLineFirstPoint);
            previewLineRenderer.SetPosition(1, previewLineLastPoint);
            previewLineCrossed = false;

            //Compare all lines in drawing if we do not cross (except last, we cant cross that one)
            for (int i = 1; i < polygonLineRenderer.positionCount - 1; i++)
            {
                if (snappingToStartPoint && i == 1) continue; //Skip first line check if we are snapping to it

                var comparisonStart = polygonLineRenderer.GetPosition(i - 1);
                var comparisonEnd = polygonLineRenderer.GetPosition(i);
                if (PolygonCalculator.LinesIntersectOnPlane(previewLineFirstPoint, previewLineLastPoint, comparisonStart, comparisonEnd))
                {
                    previewLineCrossed = true;
                    previewLineRenderer.startColor = previewLineRenderer.endColor = Color.red;
                    Debug.Log("Preview line crosses another line");
                    return;
                }
            }

            previewLineRenderer.startColor = previewLineRenderer.endColor = Color.green;
        }

        /// <summary>
        /// Automatically add a new point if our pointer position changed direction (based on threshold) on the 2D plane 
        /// </summary>
        private void AutoAddPoint()
        {
            //Clear on fresh start
            if (polygonFinished) ClearPolygon(true);

            //automatically add a new point if pointer is far enough from last point, or edge normal is different enough from last line
            if (positions.Count == 0)
            {
                AddPoint(currentWorldCoordinate);
            }
            else
            {
                var normal = (currentWorldCoordinate - previousFrameWorldCoordinate);
                var distance = normal.sqrMagnitude;
                var normalisedNormal = normal.normalized;
                var directionThreshold = Vector3.Dot(normalisedNormal, lastNormal);
                if (distance > minDistanceBetweenAutoPoints && (lastNormal == Vector3.zero || (directionThreshold != 0 && directionThreshold < minDirectionThresholdForAutoPoints)))
                {
                    AddPoint(previousFrameWorldCoordinate);
                    lastNormal = normalisedNormal;
                }
            }
        }

        private void CloseLoop(bool isNewPolygon, bool checkPreviewLine = true)
        {
            if (closedLoop)
                return;

            if (requireClosedPolygon)
            {
                if (positions.Count < minPointsToCloseLoop)
                {
                    Debug.Log("Not closing loop. Need more points.");
                    return;
                }

                if (checkPreviewLine && previewLineCrossed)
                {
                    Debug.Log("Not closing loop. Preview line is crossing another line");
                    return;
                }

                var lastPointOnTopOfFirst = (Vector3.Distance(positions[0], positions[positions.Count - 1]) < GetMinPointDistance(0));
                if (lastPointOnTopOfFirst)
                {
                    Debug.Log("Closing loop by placing last point on first");
                    snappingToStartPoint = true;
                    positions[positions.Count - 1] = positions[0];
                }
                else
                {
                    Debug.Log("Try to add a finishing line.");
                    var closingLineStart = positions[positions.Count - 1];
                    var closingLineEnd = positions[0];
                    if (PolygonCalculator.LineCrossesOtherLine(closingLineStart, closingLineEnd, positions, true, true))
                    {
                        Debug.Log("Cant close loop, closing line will cross another line.");
                        return;
                    }
                    else
                    {
                        positions.Add(closingLineEnd);
                    }
                }
            }

            closedLoop = true;

            FinishPolygon(isNewPolygon);
        }

        private void Tap()
        {
            if (mode == DrawMode.Edit)
            {
                Debug.LogWarning("PolygonInput is in edit mode, cannot Add a new point in edit mode.", gameObject);
                return;
            }

            var pointerRaycastResult = EventSystem.current.GetComponent<InputSystemUIInputModule>().GetLastRaycastResult(0);

            if (pointerRaycastResult.gameObject && pointerRaycastResult.gameObject.IsInLayerMask(lockInputLayers))
                return;

            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            UpdateCurrentWorldCoordinate();

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

            AddPoint(currentWorldCoordinate);
        }

        private void ClearPolygon(bool redraw = false)
        {
            polygonFinished = false;
            ClearHandles();

            polygonLineRenderer.startColor = polygonLineRenderer.endColor = lineColor;
            closedLoop = false;
            previewLineCrossed = false;
            positions.Clear();

            lastNormal = Vector3.zero;
            previewLineRenderer.enabled = false;

            if (redraw)
                UpdateLine();
        }

        /// <summary>
        /// Destroys all handle objects
        /// </summary>
        private void ClearHandles()
        {
            foreach (var handle in handles)
            {
                if (handle) Destroy(handle.gameObject);
            }

            handles.Clear();
        }

        private void AddPoint(Vector3 pointPosition, bool isNewPolygon = true)
        {
            //Clear on fresh start
            if (polygonFinished) ClearPolygon(true);

            //Added at start? finish and select
            if (positions.Count == 0)
            {
                previewLineRenderer.enabled = true;
                selectionStartPosition = pointPosition;
                positions.Add(pointPosition);
                if (createHandles && handleTemplate)
                    CreateHandle(positions.Count - 1);

                lastAddedPoint = pointPosition;
            }
            else if (previewLineCrossed)
            {
                Debug.Log("Cant place point. Crossing own line.");
                return;
            }
            else if (Vector3.Distance(pointPosition, positions[positions.Count - 1]) < GetMinPointDistance(positions.Count - 1))
            {
                Debug.Log("Point at same location as previous, skipping this one.");
                return;
            }
            else
            {
                positions.Add(pointPosition);
                if (createHandles)
                    CreateHandle(positions.Count - 1);

                lastAddedPoint = pointPosition;
                if (positions.Count >= maxPoints)
                {
                    if (closeLoopAtStartPoint)
                    {
                        CloseLoop(isNewPolygon);
                    }
                    else
                    {
                        FinishPolygon(isNewPolygon);
                    }
                }

                if (closeLoopAtStartPoint && snappingToStartPoint && isNewPolygon) //the reselect function should only call CloseLoop() once all the points have been re-added
                {
                    CloseLoop(true);
                }
            }

            UpdateLine();
        }

        /// <summary>
        /// Create a drag handle for line vertex position with index for changing its position
        /// </summary>
        /// <param name="positionIndex">Line vertex position index</param>
        private void CreateHandle(int positionIndex)
        {
            var lineHandle = Instantiate(handleTemplate, this.transform);
            lineHandle.gameObject.SetActive(true);
            lineHandle.pointIndex = positionIndex;
            lineHandle.transform.position = positions[positionIndex];

            lineHandle.pointerDown.AddListener(() => { blockCameraDrag.Invoke(true); });

            lineHandle.clicked.AddListener(() =>
            {
                blockCameraDrag.Invoke(false);
                if (!closedLoop && lineHandle.pointIndex == 0)
                    CloseLoop(false);
            });
            lineHandle.dragged.AddListener(() =>
            {
                if (closedLoop)
                    polygonLineRenderer.startColor = polygonLineRenderer.endColor = closedLoopLineColor;

                var handlePositionBeforeCross = lineHandle.transform.position;
                MoveHandle(lineHandle, currentWorldCoordinate);
                if (positions.Count > 2 && HandleAttachedLinesCross(lineHandle))
                {
                    polygonLineRenderer.startColor = polygonLineRenderer.endColor = lineColor;
                    MoveHandle(lineHandle, handlePositionBeforeCross);
                }
            });
            lineHandle.endDrag.AddListener(() =>
            {
                blockCameraDrag.Invoke(false);

                if (!closedLoop) return;

                FinishPolygon(false);
            });

            handles.Add(lineHandle);
        }

        private bool HandleAttachedLinesCross(PolygonDragHandle dragHandle)
        {
            Vector3 lineOneA = Vector3.zero;
            Vector3 lineOneB = Vector3.zero;
            Vector3 lineTwoA = Vector3.zero;
            Vector3 lineTwoB = Vector3.zero;

            if (dragHandle.pointIndex == 0 || dragHandle.pointIndex == positions.Count - 1)
            {
                lineOneA = positions[0];
                lineOneB = positions[positions.Count - 2];
                lineTwoA = positions[0];
                lineTwoB = positions[1];
            }
            else if (dragHandle.pointIndex < positions.Count - 1)
            {
                lineOneA = positions[dragHandle.pointIndex];
                lineOneB = positions[dragHandle.pointIndex + 1];
                lineTwoA = positions[dragHandle.pointIndex];
                lineTwoB = positions[dragHandle.pointIndex - 1];
            }

            if (PolygonCalculator.LineCrossesOtherLine(lineOneA, lineOneB, positions, false, false, true)) return true;
            if (PolygonCalculator.LineCrossesOtherLine(lineTwoA, lineTwoB, positions, false, false, true)) return true;

            return false;
        }

        private void MoveHandle(PolygonDragHandle handle, Vector3 targetPosition)
        {
            handle.transform.position = targetPosition;
            positions[handle.pointIndex] = targetPosition;

            if (closedLoop && handle.pointIndex == 0)
                positions[positions.Count - 1] = targetPosition;

            UpdateLine();
        }

        private void MoveAllHandlesToPoint()
        {
            foreach (var handle in handles)
            {
                handle.transform.position = positions[handle.pointIndex];
            }
        }

        /// <summary>
        /// Apply positions to LineRenderer positions
        /// </summary>
        private void UpdateLine()
        {
            polygonLineRenderer.positionCount = positions.Count;
            polygonLineRenderer.SetPositions(positions.ToArray());
            polygonLineRenderer.enabled = true;

            if (positions.Count > 1)
            {
                previewLineHasChanged.Invoke(positions);
            }
        }

        private void StartClick()
        {
            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            selectionStartPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
        }

        private void Release()
        {
            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var selectionEndPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
        }

        private void FinishPolygon(bool invokeNewPolygonEvent)
        {
            polygonLineRenderer.startColor = polygonLineRenderer.endColor = closedLoopLineColor;

            UpdateLine();

            requireReleaseBeforeRedraw = true;
            previewLineRenderer.enabled = false;

            if (!displayLineUntilRedraw)
                polygonLineRenderer.enabled = false;

            var positions2D = PolygonCalculator.FlattenPolygon(positions.ToArray(), worldPlane);
            var polygonIsClockwise = PolygonCalculator.PolygonIsClockwise(positions2D);
            if ((windingOrder == WindingOrder.COUNTERCLOCKWISE && polygonIsClockwise) || (windingOrder == WindingOrder.CLOCKWISE && !polygonIsClockwise))
            {
                Debug.Log($"Forcing to {windingOrder}");
                positions.Reverse();
                MoveAllHandlesToPoint();
            }

            if (mode == DrawMode.Create)
                mode = DrawMode.Edit;

            var positionsCopy = new List<Vector3>(positions);
            if (!polygonFinished && invokeNewPolygonEvent && positions.Count > 1)
                createdNewPolygonArea.Invoke(positionsCopy);
            else if (positions.Count > 1)
                editedPolygonArea.Invoke(positionsCopy);
            polygonFinished = true;
        }

        public void SetDrawMode(DrawMode mode)
        {
            this.mode = mode;

            switch (mode)
            {
                case DrawMode.Edit:
                    pointerRepresentation.gameObject.SetActive(showPointerInEditMode);
                    break;
                case DrawMode.Create:
                    pointerRepresentation.gameObject.SetActive(showPointerInCreateMode);
                    break;
                case DrawMode.CreateAndEdit:
                    pointerRepresentation.gameObject.SetActive(showPointerInCreateAndEditMode);
                    break;
            }
        }
    }
}