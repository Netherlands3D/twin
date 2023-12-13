using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands.GeoJSON;
using Netherlands3D.Coordinates;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.Indicators.Dossiers.DataLayers;
using Netherlands3D.SelectionTools;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Indicators
{
    public class DossierVisualiser : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private Material meshMaterial;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Material selectedMeshMaterial;

        [SerializeField] private Pin dataValuePin;
        [SerializeField] private float meshExtrusionHeight = 10f;

        private readonly List<ProjectAreaVisualisation> areas = new();
        public List<ProjectAreaVisualisation> Areas => areas;

        private FeatureCollection geometry;

        public DossierSO Dossier{
            get => dossier;
        }

        public FeatureCollection Geometry
        {
            get => geometry;
            set
            {
                geometry = value;
                UpdateVisualisation();
            }
        }

        private ProjectAreaVisualisation selectedArea;
        public ProjectAreaVisualisation SelectedArea
        {
            get => selectedArea;
            private set
            {
                dossier.ActiveProjectArea = value?.ProjectArea;
                selectedArea = value;
            }
        }

        public bool IsTransparent
        {
            get => isTransparent;
            set
            {
                isTransparent = value;
                ColorizeAreaMeshes();
            }
        }

        public UnityEvent<ProjectAreaVisualisation> onAreaVisualised = new();
        public UnityEvent<ProjectAreaVisualisation> onAreaRemoved = new();
        public UnityEvent<ProjectAreaVisualisation> onSelectedArea = new();
        public UnityEvent<double> onReadValueAtLocation = new();
        private bool isTransparent = false;

        private void OnEnable()
        {
            onAreaRemoved.AddListener(OnAreaRemoved);
            onAreaVisualised.AddListener(OnAreaVisualised);
            UpdateVisualisation();
        }

        private void OnDisable()
        {
            HideVisualisation();
            onAreaRemoved.RemoveListener(OnAreaRemoved);
            onAreaVisualised.RemoveListener(OnAreaVisualised);
        }

        public void UpdateVisualisation()
        {
            string previouslySelectedAreaId = SelectedArea?.ProjectArea.id;
            Clear();

            if (geometry == null)
            {
                return;
            }

            if (dossier.ActiveVariant.HasValue == false)
            {
                return;
            }

            for (var featureIndex = 0; featureIndex < geometry.Features.Count; featureIndex++)
            {
                var area = VisualizeProjectArea(featureIndex);
                Areas.Add(area);
                onAreaVisualised.Invoke(area);
            }

            // Restore selection
            if (string.IsNullOrEmpty(previouslySelectedAreaId) == false)
            {
                var previouslySelectedArea = Areas.FirstOrDefault(
                    visualisation => visualisation.ProjectArea.id == previouslySelectedAreaId
                );
                if (previouslySelectedArea != null)
                {
                    SelectArea(previouslySelectedArea);
                }
            }
        }

        public void HideVisualisation()
        {
            foreach (var area in Areas)
            {
                area.Polygons.ForEach(visualisation => visualisation.gameObject.SetActive(false));
            }
        }

        public void Clear()
        {
            dataValuePin.gameObject.SetActive(false);

            SelectedArea = null;
            foreach (var area in Areas)
            {
                foreach (var polygon in area.Polygons)
                {
                    Destroy(polygon.gameObject);
                }
                area.Polygons.Clear();
                onAreaRemoved.Invoke(area);
            }
            Areas.Clear();
        }

        private ProjectAreaVisualisation VisualizeProjectArea(int featureIndex)
        {
            var variant = dossier.ActiveVariant.Value;
            var feature = geometry.Features[featureIndex];
            var projectAreaId = feature.TryGetIdentifier("Feature" + featureIndex);

            return new ProjectAreaVisualisation(
                variant.FindProjectAreaById(projectAreaId),
                feature,
                VisualizePolygons(projectAreaId, feature)
            );
        }

        private List<PolygonVisualisation> VisualizePolygons(string projectAreaId, Feature feature)
        {
            Polygon polygon = feature.Geometry as Polygon;
            if (polygon != null)
            {
                var polygonVisualisation = VisualizePolygon(projectAreaId, 0, polygon);

                return new List<PolygonVisualisation> { polygonVisualisation };
            }
            
            MultiPolygon multiPolygon = feature.Geometry as MultiPolygon;
            if (multiPolygon == null) return new List<PolygonVisualisation>();

            var polygons = new List<PolygonVisualisation>();
            for (var polygonIndex = 0; polygonIndex < multiPolygon.Coordinates.Count; polygonIndex++)
            {
                polygons.Add(VisualizePolygon(projectAreaId, polygonIndex, multiPolygon.Coordinates[polygonIndex]));
            }

            return polygons;
        }

        private PolygonVisualisation VisualizePolygon(string projectAreaId, int polygonIndex, Polygon polygon)
        {
            var visualisationFromPolygon = CreateVisualisationFromPolygon(polygon);
            visualisationFromPolygon.gameObject.name = $"{projectAreaId}::{polygonIndex}";
            visualisationFromPolygon.gameObject.AddComponent<DossierVisualisationClickHandler>().SetVisualiser(this);            
            
            return visualisationFromPolygon;
        }

        public void MoveSamplePointer(Vector3 worldPosition)
        {
            if(dossier.SelectedDataLayer != null && dossier.SelectedDataLayer.Value.frames != null)
            {
                dataValuePin.gameObject.SetActive(true);
                dataValuePin.transform.position = worldPosition;

                DataLayer dataLayer = dossier.SelectedDataLayer.Value;
                var frames = dataLayer.frames;
                if(frames.Count < 1) 
                    return;

                var frame = frames.FirstOrDefault();
                StartCoroutine(SampleFrameMapDataAtLocation(frame,worldPosition));
            }
            else
            {
                dataValuePin.gameObject.SetActive(false);
            }
        }

        private IEnumerator SampleFrameMapDataAtLocation(Frame frame, Vector3 worldPosition)
        {
            // Load data if not there yet
            if(frame.mapData == null)
                yield return dossier.LoadMapDataAsync(frame);

            // Convert world position to normalised visualisation position
            var targetRDCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                worldPosition.x, 
                worldPosition.y, 
                worldPosition.z
            );
            var rd = CoordinateConverter.ConvertTo(targetRDCoordinate, CoordinateSystem.RD);

            // Get the bounds from our dossier
            var bbox = dossier.Data?.bbox;

            //convert bbox to RD
            var bboxMin = new Coordinate(CoordinateSystem.EPSG_3857, bbox[0], bbox[1], 0);
            var bboxMax = new Coordinate(CoordinateSystem.EPSG_3857, bbox[2], bbox[3], 0);

            //We currently need to do an extra step to unity, because CoordinateConversion does not support EPSG_3857 to RD directly yet.
            var unityBboxMin = CoordinateConverter.ConvertTo(bboxMin, CoordinateSystem.Unity);
            var unityBboxMax = CoordinateConverter.ConvertTo(bboxMax, CoordinateSystem.Unity);

            var rdBboxMin = CoordinateConverter.ConvertTo(unityBboxMin, CoordinateSystem.RD);
            var rdBboxMax = CoordinateConverter.ConvertTo(unityBboxMax, CoordinateSystem.RD);

            // Check the normalised position of the rd coordinate in the bbox
            var bboxWidth = rdBboxMax.Points[0] - rdBboxMin.Points[0];
            var bboxHeight = rdBboxMax.Points[1] - rdBboxMin.Points[1];
            var localX = rd.Points[0]-rdBboxMin.Points[0];
            var localY = rd.Points[1]-rdBboxMin.Points[1];

            var normalisedX = (float)(localX / bboxWidth);
            var normalisedY = (float)(localY / bboxHeight);

            // Sample the mapdata using the normalised location
            var sampleValueUnderPointer = frame.mapData.GetValueAtNormalisedLocation(normalisedX,normalisedY);
            onReadValueAtLocation.Invoke(sampleValueUnderPointer);
            
            dataValuePin.SetLabel(sampleValueUnderPointer.ToString());
        }

        private PolygonVisualisation CreateVisualisationFromPolygon(Polygon polygon)
        {
            var visualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(
                CreateContoursFromPolygon(polygon),
                meshExtrusionHeight,
                true,
                true,
                true,
                meshMaterial,
                lineMaterial
            );
            
            visualisation.transform.SetParent(transform);

            return visualisation;
        }

        private List<List<Vector3>> CreateContoursFromPolygon(Polygon poly)
        {
            return poly.Coordinates.Select(CreateContourFromLineString).ToList();
        }

        private List<Vector3> CreateContourFromLineString(LineString line)
        {
            var coordinateSystem = line.CRS != null ? line.EPSGId() : geometry.EPSGId();

            var contour = new List<Vector3>();
            foreach (var position in line.Coordinates)
            {
                var coordinate = new Coordinate(
                    coordinateSystem,
                    position.Longitude,
                    position.Latitude,
                    position.Altitude.GetValueOrDefault(0)
                );

                contour.Add(CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.Unity).ToVector3());
            }

            // Close polygon
            contour.Add(contour[0]);

            return contour;
        }

        private void SelectAreaBasedOnPolygon(PolygonVisualisation visualisation)
        {
            SelectArea(Areas.First(areaVisualisation => areaVisualisation.Polygons.Contains(visualisation)));
        }

        private void SelectArea(ProjectAreaVisualisation visualisation)
        {
            // nothing happens when you try to select the already selected area
            if (SelectedArea != null && SelectedArea == visualisation)
            {
                return;
            }

            // Deselect previous area
            if (SelectedArea != null)
            {
                SelectedArea
                    .Polygons
                    .ForEach(polygonVisualisation => ChangeMeshMaterial(polygonVisualisation, meshMaterial));
            }
            
            // Select new area
            SelectedArea = visualisation;
            if (SelectedArea != null)
            {
                SelectedArea
                    .Polygons
                    .ForEach(polygonVisualisation => ChangeMeshMaterial(polygonVisualisation, selectedMeshMaterial));
            }
            
            onSelectedArea.Invoke(visualisation);
        }

        private void ColorizeAreaMeshes()
        {
            Material material = null;
            foreach (var area in areas)
            {
                if (IsTransparent == false)
                {
                    material = SelectedArea == area ? selectedMeshMaterial : meshMaterial;
                }

                area
                    .Polygons
                    .ForEach(polygonVisualisation => ChangeMeshMaterial(polygonVisualisation, material));
            }
        }

        private void ChangeMeshMaterial(PolygonVisualisation polygonVisualisation, Material material)
        {
            if (material == null || IsTransparent)
            {
                polygonVisualisation.GetComponent<MeshRenderer>().materials = Array.Empty<Material>();
                return;
            }

            polygonVisualisation.GetComponent<MeshRenderer>().material = material;
        }

        private void OnAreaVisualised(ProjectAreaVisualisation projectAreaVisualisation)
        {
            projectAreaVisualisation.Polygons.ForEach(
                visualisation => visualisation.reselectVisualisedPolygon.AddListener(SelectAreaBasedOnPolygon)
            );
            if (SelectedArea == null)
            {
                SelectArea(projectAreaVisualisation);
            }
        }

        private void OnAreaRemoved(ProjectAreaVisualisation projectAreaVisualisation)
        {
            if (SelectedArea == projectAreaVisualisation)
            {
                SelectArea(null);
            }

            projectAreaVisualisation.Polygons.ForEach(
                visualisation => visualisation.reselectVisualisedPolygon.RemoveListener(SelectAreaBasedOnPolygon)
            );
        }
    }
}