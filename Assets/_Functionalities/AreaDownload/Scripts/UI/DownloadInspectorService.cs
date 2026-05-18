/*
 *  Copyright (C) X Gemeente
 *                X Amsterdam
 *                X Economic Services Departments
 *
 *  Licensed under the EUPL, Version 1.2 or later (the "License");
 *  You may not use this work except in compliance with the License.
 *  You may obtain a copy of the License at:
 *
 *    https://github.com/Amsterdam/Netherlands3D/blob/main/LICENSE.txt
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" basis,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
 *  implied. See the License for the specific language governing
 *  permissions and limitations under the License.
 */

using System.Collections.Generic;
using System.Runtime.InteropServices;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.AreaDownload.UI
{
    public class DownloadInspectorService : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void CopyToClipboard(string textToCopy);

        [Header("Settings")]
        [Tooltip("In what coordinate system should the coordinates be shown to the user?")]
        [SerializeField] private CoordinateSystem DisplayCrs = CoordinateSystem.RD;
        
        [Header("References")]
        [SerializeField] private AreaSelection areaSelection;
        [SerializeField] private TextPopout popoutPrefab;
        
        private Coordinate NorthEast => ConvertBoundsToCoordinates(selectedArea).northEast;
        private Coordinate SouthWest => ConvertBoundsToCoordinates(selectedArea).southWest;

        public string NorthExtent => NorthEast.northing.ToString("0");
        public string SouthExtent => SouthWest.northing.ToString("0");
        public string EastExtent => NorthEast.easting.ToString("0");
        public string WestExtent => SouthWest.easting.ToString("0");
        
        private TextPopout northEastTooltip;
        private TextPopout southWestTooltip;

        private Bounds selectedArea;
        private List<Vector3> selectedAreaPoints = new();
        
        public UnityEvent<Bounds> OnSelectionBoundsChanged = new();

        private void OnEnable()
        {
            OnSelectionBoundsChanged.AddListener(WhenSelectionBoundsChanged);
            areaSelection.OnSelectionAreaBoundsChanged.AddListener(OnSelectionBoundsChanged.Invoke);

            PolygonSelectionService selectionService = ServiceLocator.GetService<PolygonSelectionService>();
            selectionService.OnDeselectActivePolygon.AddListener(WhenDeselected);

            Canvas canvas = CanvasID.GetCanvasByType(CanvasType.World);
            northEastTooltip = CreateCornerPopout(canvas.transform, PivotPresets.MiddleLeft);
            northEastTooltip.SetSnappingSide(TextPopout.SnappingSide.Left);
            southWestTooltip = CreateCornerPopout(canvas.transform, PivotPresets.MiddleRight);
            southWestTooltip.SetSnappingSide(TextPopout.SnappingSide.Right);
        }

        private void OnDisable()
        {
            areaSelection.OnSelectionAreaBoundsChanged.RemoveListener(OnSelectionBoundsChanged.Invoke);
            OnSelectionBoundsChanged.RemoveListener(WhenSelectionBoundsChanged);
            
            PolygonSelectionService selectionService = ServiceLocator.GetService<PolygonSelectionService>();
            selectionService.OnDeselectActivePolygon.RemoveListener(WhenDeselected);
            
            Destroy(northEastTooltip.gameObject);
            Destroy(southWestTooltip.gameObject);
        }

        public void SetNorthValue(int value)
        {
            Coordinate newCoord = new Coordinate(DisplayCrs, NorthEast.easting, value);
            Vector3 pos = newCoord.ToUnity();
            Vector3 newMax = new Vector3(selectedArea.max.x, selectedArea.max.y, pos.z);
            selectedArea.SetMinMax(selectedArea.min, newMax);
            ApplyBounds();
        }

        public void SetSouthValue(int value)
        {
            Coordinate newCoord = new Coordinate(DisplayCrs, NorthEast.easting, value);
            Vector3 pos = newCoord.ToUnity();
            Vector3 newMin = new Vector3(selectedArea.min.x, selectedArea.min.y, pos.z);
            selectedArea.SetMinMax(newMin, selectedArea.max);
            ApplyBounds();
        }

        public void SetEastValue(int value)
        {
            Coordinate newCoord = new Coordinate(DisplayCrs, value, NorthEast.northing);
            Vector3 pos = newCoord.ToUnity();
            Vector3 newMax = new Vector3(pos.x, selectedArea.max.y, selectedArea.max.z);
            selectedArea.SetMinMax(selectedArea.min, newMax);
            ApplyBounds();
        }

        public void SetWestValue(int value)
        {
            Coordinate newCoord = new Coordinate(DisplayCrs, value, NorthEast.northing); 
            Vector3 pos = newCoord.ToUnity();
            Vector3 newMin = new Vector3(pos.x, selectedArea.min.y, selectedArea.min.z);
            selectedArea.SetMinMax(newMin, selectedArea.max);
            ApplyBounds();
        }

        private void ApplyBounds()
        {
            areaSelection.SetSelectionAreaBounds(selectedArea);
            PolygonCreationService creationService = ServiceLocator.GetService<PolygonCreationService>();
            selectedAreaPoints.Clear();
            selectedAreaPoints.Add(new Vector3(selectedArea.min.x, selectedArea.center.y, selectedArea.max.z));
            selectedAreaPoints.Add(new Vector3(selectedArea.max.x, selectedArea.center.y, selectedArea.max.z));
            selectedAreaPoints.Add(new Vector3(selectedArea.max.x, selectedArea.center.y, selectedArea.min.z));
            selectedAreaPoints.Add(new Vector3(selectedArea.min.x, selectedArea.center.y, selectedArea.min.z));
            creationService.UpdateGridSelectionFromPoints(selectedAreaPoints);
        }

        private void WhenSelectionBoundsChanged(Bounds selectedArea)
        {
            this.selectedArea = selectedArea;
            southWestTooltip.Show($"X: {WestExtent}\nY: {SouthExtent}", SouthWest, true);
            northEastTooltip.Show($"X: {EastExtent}\nY: {NorthExtent}", NorthEast, true);
        }

        private void WhenDeselected()
        {
            southWestTooltip.Hide();
            northEastTooltip.Hide();
        }

        private TextPopout CreateCornerPopout(Transform canvasTransform, PivotPresets pivotPoint)
        {
            var popout = Instantiate(popoutPrefab, canvasTransform);
            popout.RectTransform().SetPivot(pivotPoint);
            popout.transform.SetSiblingIndex(0);

            return popout;
        }

        // TODO: This should be moved to the Coordinates package and make it configurable whether you want a 2D (where
        // the y equals the center of the bound) or a 3D results (containing the full bounds)
        private (Coordinate southWest, Coordinate northEast) ConvertBoundsToCoordinates(Bounds bounds)
        {
            var minUnityPosition = new Vector3(bounds.min.x, bounds.center.y, bounds.min.z);
            var min = new Coordinate(minUnityPosition);
            var southWest = min.Convert(DisplayCrs);

            var maxUnityPosition = new Vector3(bounds.max.x, bounds.center.y, bounds.max.z);
            var max = new Coordinate(maxUnityPosition);
            var northEast = max.Convert(DisplayCrs);

            return (southWest, northEast);
        }
        
        public void CopySouthWestToClipboard()
        {
            var text = $"{WestExtent},{SouthExtent}";
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        public void CopyNorthEastToClipboard()
        {
            var text = $"{EastExtent},{NorthExtent}";
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }
    }
}