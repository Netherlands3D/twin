using System;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.UI;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class GridSelectionToolCoordinateLabels : MonoBehaviour
    {
        private AreaSelection areaSelection;
        [SerializeField] private CoordinateSystem displayCrs = CoordinateSystem.RD;
        [SerializeField] private TextPopout popoutPrefab;
        private TextPopout northEastTooltip;
        private TextPopout southWestTooltip;
        
        private void WhenSelectionBoundsChanged(Bounds selectedArea)
        {
            var southWestAndNorthEast = ConvertBoundsToCoordinates(selectedArea);
            
            var northExtentText = southWestAndNorthEast.northEast.northing.ToString("0");
            var southExtentText = southWestAndNorthEast.southWest.northing.ToString("0");
            var eastExtentText = southWestAndNorthEast.northEast.easting.ToString("0");
            var westExtentText = southWestAndNorthEast.southWest.easting.ToString("0");

            southWestTooltip.Show($"X: {westExtentText}\nY: {southExtentText}", southWestAndNorthEast.Item1, true);
            northEastTooltip.Show($"X: {eastExtentText}\nY: {northExtentText}", southWestAndNorthEast.Item2, true);
        }

        private void Awake()
        {
            areaSelection = GetComponent<AreaSelection>();
        }

        private void OnEnable()
        {
            areaSelection.whenDrawingArea.AddListener(WhenSelectionBoundsChanged);
        }

        private void OnDisable()
        {
            areaSelection.whenDrawingArea.RemoveListener(WhenSelectionBoundsChanged);
        }

        private void Start()
        {
            var canvasTransform = FindAnyObjectByType<Canvas>().transform; //todo: replace this with the correct canvas once we split them
            northEastTooltip = CreateCornerPopout(canvasTransform, PivotPresets.MiddleLeft);
            southWestTooltip = CreateCornerPopout(canvasTransform, PivotPresets.MiddleRight);
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
            var southWest = CoordinateConverter.ConvertTo(min, displayCrs);

            var maxUnityPosition = new Vector3(bounds.max.x, bounds.center.y, bounds.max.z);
            var max = new Coordinate(maxUnityPosition);
            var northEast = CoordinateConverter.ConvertTo(max, displayCrs);

            return (southWest, northEast);
        }
    }
}
