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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.SelectionTools
{
    public class AreaSelection : PolygonInput
    {
        private MeshRenderer boundsMeshRenderer;

        [Header("Invoke")]
        public UnityEvent<bool> blockCameraDragging = new();
        [Tooltip("Fires while a new area is being drawn")]
        public UnityEvent<Bounds> whenDrawingArea = new();
        [FormerlySerializedAs("selectedAreaBounds")]
        [Tooltip("Fires when an area is selected")]
        public UnityEvent<Bounds> whenAreaIsSelected = new();

        [Header("Settings")]
        [SerializeField] private float gridSize = 100;
        [SerializeField] private float multiplyHighlightScale = 5.0f;
        [SerializeField] private bool useWorldSpace = false;

        [SerializeField] private GameObject gridHighlight;
        [SerializeField] private GameObject selectionBlock;
        [SerializeField] private Material triplanarGridMaterial;

        private bool drawingArea = false;

        public float GridSize => gridSize;

        private Vector3 gridOffset;
        public Vector3 GridOffset
        {
            get
            {
                return gridOffset;
            }
            set
            {
                gridOffset = value;
                if (triplanarGridMaterial)
                    triplanarGridMaterial.SetVector("_TriplanarGridOffset", value);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (!selectionBlock)
            {
                Debug.LogWarning("The selection block reference is not set in the inspector. Please make sure to set the reference.", this.gameObject);
                return;
            }
            boundsMeshRenderer = selectionBlock.GetComponent<MeshRenderer>();
            SetSelectionVisualEnabled(false);

            worldPlane = (useWorldSpace) ? new Plane(Vector3.up, Vector3.zero) : new Plane(this.transform.up, this.transform.position);
            
            SetDrawMode(DrawMode.Selected);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(selectionBlock)
                selectionBlock.transform.localScale = Vector3.one * gridSize;

            if(gridHighlight)
                gridHighlight.transform.localScale = Vector3.one * gridSize * multiplyHighlightScale;

            if(triplanarGridMaterial)
               triplanarGridMaterial.SetFloat("GridSize", 1.0f / gridSize);
        }
#endif       

        protected override void OnDisable()
        {
            base.OnDisable();
            drawingArea = false;
            SetSelectionVisualEnabled(false);
        }

        protected override void Update()
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

        protected override void Tap()
        {
            if (Interface.PointerIsOverUI())
                return;

            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            var tappedPosition = GetGridPosition(worldPosition);
            DrawSelectionArea(tappedPosition, tappedPosition);
            MakeSelection();
        }

        protected override void StartClick()
        {
            if (Interface.PointerIsOverUI())
                return;

            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            selectionStartPosition = GetGridPosition(worldPosition);
        }

        protected override void Release()
        {
            if (Interface.PointerIsOverUI())
                return;

            var currentPointerPosition = pointerAction.ReadValue<Vector2>();
            var worldPosition = Camera.main.GetCoordinateInWorld(currentPointerPosition, worldPlane, maxSelectionDistanceFromCamera);
            var selectionEndPosition = GetGridPosition(worldPosition);

            if (drawingArea)
            {
                DrawSelectionArea(selectionStartPosition, selectionEndPosition);
                MakeSelection();
            }
        }

        private void MakeSelection()
        {
            if (mode == DrawMode.Selected) return;

            var bounds = boundsMeshRenderer.bounds;
            SetSelectionVisualEnabled(true);
            whenAreaIsSelected.Invoke(bounds);
        }

        /// <summary>
        /// Get a rounded position using the grid size
        /// </summary>
        /// <param name="samplePosition">The position to round to grid position</param>
        /// <returns></returns>
        private Vector3Int GetGridPosition(Vector3 samplePosition)
        {
            // Shift the sample position relative to the aligned grid origin
            float x = samplePosition.x - GridOffset.x;
            float z = samplePosition.z - GridOffset.z;

            // Floor to get the cell that contains the samplePosition
            x = Mathf.Floor(x / gridSize) * gridSize;
            z = Mathf.Floor(z / gridSize) * gridSize;

            // Apply the offset back to re-align with world
            x += GridOffset.x + (gridSize * 0.5f);
            z += GridOffset.z + (gridSize * 0.5f);

            Vector3Int roundedPosition = new Vector3Int
            {
                x = Mathf.RoundToInt(x),
                y = Mathf.RoundToInt(samplePosition.y),
                z = Mathf.RoundToInt(z)
            };

            return roundedPosition;
        }

        /// <summary>
        /// Draw selection area by scaling the block
        /// </summary>
        /// <param name="currentWorldCoordinate">Current pointer position in world</param>
        private void DrawSelectionArea(Vector3 startWorldCoordinate, Vector3 currentWorldCoordinate)
        {
            var xDifference = (currentWorldCoordinate.x - startWorldCoordinate.x);
            var zDifference = (currentWorldCoordinate.z - startWorldCoordinate.z);
            selectionBlock.transform.position = startWorldCoordinate;
            selectionBlock.transform.Translate(xDifference * 0.5f, 0, zDifference * 0.5f);
            float x = Mathf.Abs(currentWorldCoordinate.x - startWorldCoordinate.x) + gridSize;
            float y = gridSize;
            float z = Mathf.Abs(currentWorldCoordinate.z - startWorldCoordinate.z) + gridSize;
            selectionBlock.transform.localScale = new Vector3(x, y, z);
            var bounds = boundsMeshRenderer.bounds;
            whenDrawingArea.Invoke(bounds);
        }

        public void ClearSelection()
        {
            SetSelectionVisualEnabled(false);
        }

        public void SetSelectionVisualEnabled(bool enabled)
        {
            selectionBlock.gameObject.SetActive(enabled);
        }

        public void ReselectAreaFromPolygon(List<Vector3> points)
        {
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (var i = 1; i < points.Count; i++)
            {
                bounds.Encapsulate(points[i]);
            }
            //we need to inset the bounds by half a grid size so that the selection aligns with the grid properly
            //when selection is drawn the size of the selection will be increased by one gridsize
            Vector3 insetHalfGridSize = new Vector3(0.5f * gridSize, 0, 0.5f * gridSize);
            selectionStartPosition = GetGridPosition(bounds.min + insetHalfGridSize);
            var selectionEndPosition = GetGridPosition(bounds.max - insetHalfGridSize);

            DrawSelectionArea(selectionStartPosition, selectionEndPosition);
        }

        public override void SetDrawMode(DrawMode mode)
        {
            this.mode = mode;

            gridHighlight.gameObject.SetActive(mode != DrawMode.Selected);

        }
    }
}