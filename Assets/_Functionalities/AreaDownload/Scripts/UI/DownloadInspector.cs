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

using System.Runtime.InteropServices;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Rendering;
using Netherlands3D.Twin.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.AreaDownload.UI
{
    public class DownloadInspector : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void CopyToClipboard(string textToCopy);

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
            northEastTooltip = CreateCornerPopout(canvasTransform, PivotPresets.MiddleLeft);
            southWestTooltip = CreateCornerPopout(canvasTransform, PivotPresets.MiddleRight);
        }

        private void OnDisable()
        {
            areaSelection.OnSelectionAreaBoundsChanged.RemoveListener(OnSelectionBoundsChanged);
            areaSelection.WhenSelectionAreaBoundsChanged.RemoveListener(WhenSelectionBoundsChanged);
            copyNorthEastExtentButton.onClick.RemoveListener(CopyNorthEastToClipboard);
            copySouthWestExtentButton.onClick.RemoveListener(CopySouthWestToClipboard);
            
            Destroy(northEastTooltip.gameObject);
            Destroy(southWestTooltip.gameObject);
        }

        private void WhenSelectionBoundsChanged(Bounds selectedArea)
        {
            var southWestAndNorthEast = ConvertBoundsToCoordinates(selectedArea);
            
            northExtentTextField.text = southWestAndNorthEast.northEast.Points[0].ToString("0");
            southExtentTextField.text = southWestAndNorthEast.southWest.Points[0].ToString("0");
            eastExtentTextField.text = southWestAndNorthEast.northEast.Points[1].ToString("0");
            westExtentTextField.text = southWestAndNorthEast.southWest.Points[1].ToString("0");

            southWestTooltip.Show($"X: {southExtentTextField.text}\nY: {westExtentTextField.text}", southWestAndNorthEast.Item1, true);
            northEastTooltip.Show($"X: {northExtentTextField.text}\nY: {eastExtentTextField.text}", southWestAndNorthEast.Item2, true);
        }

        private void OnSelectionBoundsChanged(Bounds selectedArea)
        {
            renderedThumbnail.RenderThumbnail(selectedArea);
        }

        private void CopySouthWestToClipboard()
        {
            var text = $"{southExtentTextField.text},{westExtentTextField.text}";
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
        }

        private void CopyNorthEastToClipboard()
        {
            var text = $"{northExtentTextField.text},{eastExtentTextField.text}";
#if UNITY_WEBGL && !UNITY_EDITOR
            CopyToClipboard(text);
#else
            GUIUtility.systemCopyBuffer = text;
#endif
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
            var min = new Coordinate(CoordinateSystem.Unity, bounds.min.x, bounds.center.y, bounds.min.z);
            var southWest = CoordinateConverter.ConvertTo(min, DisplayCrs);
            
            var max = new Coordinate(CoordinateSystem.Unity, bounds.max.x, bounds.center.y, bounds.max.z);
            var northEast = CoordinateConverter.ConvertTo(max, DisplayCrs);

            return (southWest, northEast);
        }
    }
}