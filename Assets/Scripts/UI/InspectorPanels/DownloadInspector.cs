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

using System;
using GG.Extensions;
using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Interface.BAG
{
    public class DownloadInspector : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("In what coordinate system should the coordinates be shown to the user?")]
        [SerializeField] private CoordinateSystem DisplayCrs = CoordinateSystem.RD;
        
        [Header("References")]
        [SerializeField] private AreaSelection areaSelection;
        [SerializeField] private RenderedThumbnail renderedThumbnail;
        [SerializeField] private TMP_InputField northExtentTextField;
        [SerializeField] private TMP_InputField southExtentTextField;
        [SerializeField] private TMP_InputField eastExtentTextField;
        [SerializeField] private TMP_InputField westExtentTextField;
        [SerializeField] private Button copyNorthEastExtentButton;
        [SerializeField] private Button copySouthWestExtentButton;
        [SerializeField] private TextPopout popoutPrefab;

        private TextPopout northEastTooltip;
        private TextPopout southWestTooltip;

        private void OnEnable()
        {
            areaSelection.WhenSelectionAreaBoundsChanged.AddListener(WhenSelectionBoundsChanged);
            areaSelection.OnSelectionAreaBoundsChanged.AddListener(OnSelectionBoundsChanged);
            copyNorthEastExtentButton.onClick.AddListener(CopyNorthEastToClipboard);
            copySouthWestExtentButton.onClick.AddListener(CopySouthWestToClipboard);

            var canvasTransform = transform.GetComponentInParent<Canvas>().transform;
            northEastTooltip = Instantiate(popoutPrefab, canvasTransform);
            northEastTooltip.RectTransform().SetPivot(PivotPresets.BottomCenter);
            southWestTooltip = Instantiate(popoutPrefab, canvasTransform);
            southWestTooltip.RectTransform().SetPivot(PivotPresets.TopCenter);
        }

        private void OnDisable()
        {
            areaSelection.OnSelectionAreaBoundsChanged.RemoveListener(OnSelectionBoundsChanged);
            areaSelection.WhenSelectionAreaBoundsChanged.RemoveListener(WhenSelectionBoundsChanged);
            copyNorthEastExtentButton.onClick.RemoveListener(CopyNorthEastToClipboard);
            copySouthWestExtentButton.onClick.RemoveListener(CopySouthWestToClipboard);
            
            Destroy(northEastTooltip);
            Destroy(southWestTooltip);
        }

        private void WhenSelectionBoundsChanged(Bounds selectedArea)
        {
            var southWestAndNorthEast = ConvertBoundsToCoordinates(selectedArea);
            
            northExtentTextField.text = southWestAndNorthEast.Item2.Points[0].ToString("F");
            southExtentTextField.text = southWestAndNorthEast.Item1.Points[0].ToString("F");
            eastExtentTextField.text = southWestAndNorthEast.Item2.Points[1].ToString("F");
            westExtentTextField.text = southWestAndNorthEast.Item1.Points[1].ToString("F");

            southWestTooltip.Show($"X: {southExtentTextField.text}\nY: {westExtentTextField.text}", southWestAndNorthEast.Item1, true);
            northEastTooltip.Show($"X: {northExtentTextField.text}\nY: {eastExtentTextField.text}", southWestAndNorthEast.Item2, true);
        }

        private void OnSelectionBoundsChanged(Bounds selectedArea)
        {
            renderedThumbnail.RenderThumbnail(selectedArea);
        }

        private void CopySouthWestToClipboard()
        {
            // TODO: As expected, this does not work in WebGL
            GUIUtility.systemCopyBuffer = $"{southExtentTextField.text},{westExtentTextField.text}";
        }

        private void CopyNorthEastToClipboard()
        {
            // TODO: As expected, this does not work in WebGL
            GUIUtility.systemCopyBuffer = $"{northExtentTextField.text},{eastExtentTextField.text}";
        }

        // TODO: This should be moved to the Coordinates package and make it configurable whether you want a 2D (where
        // the y equals the center of the bound) or a 3D results (containing the full bounds)
        private Tuple<Coordinate, Coordinate> ConvertBoundsToCoordinates(Bounds bounds)
        {
            var min = new Coordinate(CoordinateSystem.Unity, bounds.min.x, bounds.center.y, bounds.min.z);
            var southWest = CoordinateConverter.ConvertTo(min, DisplayCrs);
            
            var max = new Coordinate(CoordinateSystem.Unity, bounds.max.x, bounds.center.y, bounds.max.z);
            var northEast = CoordinateConverter.ConvertTo(max, DisplayCrs);

            return new Tuple<Coordinate, Coordinate>(southWest, northEast);
        }
    }
}