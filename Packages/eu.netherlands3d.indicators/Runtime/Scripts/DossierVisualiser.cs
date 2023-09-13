using System;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands.GeoJSON;
using Netherlands3D.Coordinates;
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
        [SerializeField] private float meshExtrusionHeight = 10f;

        private readonly List<ProjectAreaVisualisation> areas = new();
        public List<ProjectAreaVisualisation> Areas => areas;

        private FeatureCollection geometry;

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
            private set => selectedArea = value;
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

            return visualisationFromPolygon;
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