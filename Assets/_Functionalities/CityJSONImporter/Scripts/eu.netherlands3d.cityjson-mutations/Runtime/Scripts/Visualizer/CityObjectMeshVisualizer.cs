using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.CityJson.Visualisation
{
    public class BoundaryMeshData
    {
        public GeometryTriangulationData TriangulationData;
        public CityGeometrySemanticsObject SemanticsObject;
        public List<int> MaterialIndices;

        public BoundaryMeshData(GeometryTriangulationData triangulationData, CityGeometrySemanticsObject semanticsObject, List<int> materialIndices)
        {
            TriangulationData = triangulationData;
            SemanticsObject = semanticsObject;
            MaterialIndices = materialIndices;
        }
    }

    public class MeshWithMaterials
    {
        public Mesh Mesh;
        public Material[] Materials;

        public MeshWithMaterials(Mesh mesh, Material[] materials)
        {
            Mesh = mesh;
            Materials = materials;
        }
    }

    [Serializable]
    public class SemanticMaterials
    {
        public SurfaceSemanticType Type;
        public Material Material;
    }

    /// <summary>
    /// This class visualizes a CityObject by creating a mesh for each LOD geometry.
    /// </summary>
    [RequireComponent(typeof(CityObject))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CityObjectMeshVisualizer : CityObjectVisualizer
    {
        private static int meshesCreatedThisFrame = 0;
        private const int maxMeshesPerFrame = 20;
        private static int lastFrameCount = -1;

        private Dictionary<CityGeometry, MeshWithMaterials> meshes;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        [SerializeField] private int activeLOD;
        public int ActiveLod => activeLOD;
        public Mesh ActiveMesh { get; private set; }

        [SerializeField] private bool addMeshCollider = true;
        [SerializeField] private CityMaterialConverter materialConverter;

        public override Material[] Materials => meshRenderer.materials;

#if UNITY_EDITOR
        // allow to change the visible LOD from the inspector during runtime
        private void OnValidate()
        {
            if (meshes != null)
                SetLODActive(activeLOD);
        }
#endif
        protected override void Awake()
        {
            base.Awake();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }

        //create the meshes
        protected override void Visualize()
        {
            transform.localPosition = SetLocalPosition(cityObject); //set position first so the CityObject's transformationMatrix can be used to position the mesh.
            materialConverter.Initialize(cityObject.Appearance);
            StartCoroutine(CreateMeshesPerFrame(maxMeshesPerFrame));
        }

        private IEnumerator CreateMeshesPerFrame(int maxCallsPerFrame)
        {
            yield return null; //a bit ugly, but we wait a frame to process the garbage of parsing the data.

            while (meshesCreatedThisFrame >= maxCallsPerFrame)
            {
                yield return null; // wait next frame (the counter will be reset there)
                if (Time.frameCount != lastFrameCount)
                {
                    lastFrameCount = Time.frameCount;
                    meshesCreatedThisFrame = 0;
                }
            }

            meshesCreatedThisFrame++;
            meshes = CreateMeshes(cityObject);

            var highestLod = meshes.Count > 0 ? meshes.Keys.Max(g => g.Lod) : -1;
            SetLODActive(highestLod);

            if(addMeshCollider && meshes.Count > 0)
                gameObject.AddComponent<MeshCollider>();
            
            cityObjectVisualized?.Invoke(this);
        }

        private Vector3 SetLocalPosition(CityObject cityObject)
        {
            var crs = cityObject.CoordinateSystem;
            crs = CoordinateSystems.To3D(crs);

            var relativeCenter = cityObject.RelativeCenter;
            var relativeCoordinate = new Coordinate(crs, relativeCenter.x, relativeCenter.y, relativeCenter.z); //this is not a valid coordinate, but we need to use the Coordinate struct to determine the axis order
            return new Vector3((float)relativeCoordinate.easting, (float)relativeCoordinate.height, (float)relativeCoordinate.northing);
        }


        //enable the mesh of a certain LOD
        public bool SetLODActive(int lod)
        {
            activeLOD = lod;

            var geometry = meshes.Keys.FirstOrDefault(g => g.Lod == lod); // More than one Geometry Object is used to represent several different levels-of-detail (LoDs) for the same object.
            //TODO: according to the CityJSON specs: "However, the different Geometry Objects of a given City Object do not have to be of different LoDs." In this case we need to change FirstOrDefault with Select, and change SetMesh to accept a collection.
            if (geometry != null)
            {
                SetMesh(meshes[geometry]);
                return true;
            }

            SetMesh(null);
            return false;
        }

        private void SetMesh(MeshWithMaterials mesh)
        {
            if (mesh != null)
            {
                ActiveMesh = mesh.Mesh;
                meshRenderer.materials = mesh.Materials;
            }
            else
            {
                ActiveMesh = null;
            }

            meshFilter.mesh = ActiveMesh;

            if (meshCollider)
                meshCollider.sharedMesh = ActiveMesh;
        }

        //create the meshes for the object geometries
        private Dictionary<CityGeometry, MeshWithMaterials> CreateMeshes(CityObject cityObject)
        {
            meshes = new Dictionary<CityGeometry, MeshWithMaterials>(cityObject.Geometries.Count);
            foreach (var geometry in cityObject.Geometries)
            {
                if (geometry.Type == GeometryType.MultiPoint || geometry.Type == GeometryType.MultiLineString)
                    continue; // MultiPoint/Lines have their own visualizer and don't create meshes

                Vector3Double origin = new Vector3Double();
                var cityJsonCoord = GetComponentInParent<WorldTransform>().Coordinate; //todo: this getComponentInParent is a bit hacky
                var coordinateSystem = cityObject.CoordinateSystem;
                if (coordinateSystem != CoordinateSystem.Undefined)
                {
                    var convertedCoord = cityJsonCoord.Convert(coordinateSystem);
                    origin = new Vector3Double(convertedCoord.value1, convertedCoord.value2, convertedCoord.value3);
                }
                else
                {
                    origin = GetComponentInParent<CityJSON>().AbsoluteCenter; //we cannot convert an undefined crs, so we assume the origin is at the absolute center
                }

                // The geometry's vertices are in world space, so we need to subtract the cityJSON's origin to get them in cityJSON space, and then subtract the cityObject's origin to be able to create a mesh with the origin at the cityObject's position.
                // The CityJSON origin is at the citJSON WorldTransform coordinate, the CityObject's origin is its localPosition, since we set it previously.
                var mesh = CreateMeshFromGeometry(geometry, coordinateSystem, origin, cityObject.transform.localPosition);
                meshes.Add(geometry, mesh);
            }

            return meshes;
        }

        private MeshWithMaterials CreateMeshFromGeometry(CityGeometry geometry, CoordinateSystem coordinateSystem, Vector3Double vertexOffset, Vector3 objectOffset)
        {
            var boundaryMeshData = BoundariesToMeshes(geometry.BoundaryObject, coordinateSystem, vertexOffset);
            List<Mesh> subMeshes = new List<Mesh>();
            List<Material> materials = new List<Material>();

            Dictionary<SurfaceSemanticType, List<BoundaryMeshData>> sortedBoundaryMeshData = SortBoundaryMeshDataBySemanticObject(boundaryMeshData);

            foreach (var semanticTypeKVP in sortedBoundaryMeshData)
            {
                if (geometry.UseSingleMaterialForEntireGeometry)
                {
                    subMeshes.AddRange(CombineBoundaryMeshes(semanticTypeKVP.Value, objectOffset));
                    var materialIndices = geometry.GetMaterialIndices(string.Empty); //TODO: we currently don't support themes, and will always return the first available theme
                    materials.AddRange(materialConverter.GetMaterials(materialIndices, semanticTypeKVP.Key));
                }
                else
                {
                    var themeIndex = geometry.GetThemeIndex(string.Empty); //TODO: we currently don't support themes, and will always return the first available theme
                    subMeshes.AddRange(CombineBoundaryMeshesWithTheSameMaterial(semanticTypeKVP.Value, objectOffset, themeIndex, out var materialIndices));
                    materials.AddRange(materialConverter.GetMaterials(materialIndices, semanticTypeKVP.Key));
                }
            }

            var mesh = CombineMeshes(subMeshes, Matrix4x4.identity, false); //use identity matrix because we already transformed the submeshes
            return new MeshWithMaterials(mesh, materials.ToArray());
        }

        private static Dictionary<SurfaceSemanticType, List<BoundaryMeshData>> SortBoundaryMeshDataBySemanticObject(List<BoundaryMeshData> boundaryMeshData)
        {
            var dictionary = new Dictionary<SurfaceSemanticType, List<BoundaryMeshData>>();
            while (boundaryMeshData.Count > 0)
            {
                List<BoundaryMeshData> list = new List<BoundaryMeshData>();
                CityGeometrySemanticsObject activeSemanticsObject = boundaryMeshData[^1].SemanticsObject;
                for (int i = boundaryMeshData.Count - 1; i >= 0; i--) //go backwards because collection will be modified
                {
                    var boundaryMesh = boundaryMeshData[i];
                    if (boundaryMesh.SemanticsObject == activeSemanticsObject)
                    {
                        list.Add(boundaryMesh);
                        boundaryMeshData.Remove(boundaryMesh);
                    }
                }

                if (activeSemanticsObject != null)
                    dictionary.Add(activeSemanticsObject.SurfaceType, list);
                else
                    dictionary.Add(SurfaceSemanticType.Null, list);
            }

            return dictionary;
        }

        private static List<Mesh> CombineBoundaryMeshesWithTheSameMaterial(List<BoundaryMeshData> boundaryMeshData, Vector3 offset, int themeIndex, out List<int> materialIndices)
        {
            List<Mesh> combinedMeshes = new List<Mesh>(boundaryMeshData.Count);
            List<GeometryTriangulationData> meshDataToCombine = new List<GeometryTriangulationData>(boundaryMeshData.Count);
            List<GeometryTriangulationData> meshDataWithNullMaterial = new List<GeometryTriangulationData>(boundaryMeshData.Count);
            materialIndices = new List<int>();

            //combine mesh datas per materialIndexList. Since we want to support different themes, we can only combine meshes that have the same exact MaterialIndexList
            while (boundaryMeshData.Count > 0)
            {
                int activeMaterialIndex = boundaryMeshData[boundaryMeshData.Count - 1].MaterialIndices[themeIndex];
                materialIndices.Add(activeMaterialIndex);

                for (int i = boundaryMeshData.Count - 1; i >= 0; i--) //go backwards because collection will be modified
                {
                    var boundaryMesh = boundaryMeshData[i];
                    if (boundaryMesh.MaterialIndices == null && boundaryMesh.TriangulationData != null)
                    {
                        meshDataWithNullMaterial.Add(boundaryMesh.TriangulationData);
                        boundaryMeshData.Remove(boundaryMesh);
                        continue;
                    }

                    if (boundaryMesh.MaterialIndices[themeIndex] == activeMaterialIndex)
                    {
                        if (boundaryMesh.TriangulationData != null) //skip invalid polygons
                        {
                            meshDataToCombine.Add(boundaryMesh.TriangulationData);
                        }

                        boundaryMeshData.Remove(boundaryMesh);
                    }
                }

                var combinedMesh = PolygonVisualisationUtility.CreatePolygonMesh(meshDataToCombine, offset);
                combinedMeshes.Add(combinedMesh);
                meshDataToCombine.Clear();
            }

            if (meshDataWithNullMaterial.Count > 0)
            {
                var meshWithNullMaterial = PolygonVisualisationUtility.CreatePolygonMesh(meshDataWithNullMaterial, offset);
                combinedMeshes.Add(meshWithNullMaterial);
                materialIndices.Add(-1); //no material is defined, so we use material index -1 for the submesh
            }

            return combinedMeshes;
        }

        private static List<Mesh> CombineBoundaryMeshes(List<BoundaryMeshData> boundaryMeshData, Vector3 offset)
        {
            //combine all mesh datas without checking for submesh separations
            List<GeometryTriangulationData> meshDataToCombine = new List<GeometryTriangulationData>(boundaryMeshData.Count);
            foreach (var meshData in boundaryMeshData)
            {
                if (meshData.TriangulationData != null)
                    meshDataToCombine.Add(meshData.TriangulationData);
            }

            var combinedMesh = PolygonVisualisationUtility.CreatePolygonMesh(meshDataToCombine, offset);
            return new List<Mesh>() { combinedMesh };
        }

        private static Mesh CombineMeshes(List<Mesh> meshes, Matrix4x4 transformationMatrix, bool mergeSubMeshes)
        {
            CombineInstance[] combine = new CombineInstance[meshes.Count];

            for (int i = 0; i < meshes.Count; i++)
            {
                combine[i].mesh = meshes[i];
                combine[i].transform = transformationMatrix;
            }

            var mesh = new Mesh();
            mesh.CombineMeshes(combine, mergeSubMeshes);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        //Different boundary objects need to be parsed into meshes in different ways because of the different depths of the boundary arrays. We need to go as deep as needed to create meshes from surfaces.
        private List<BoundaryMeshData> BoundariesToMeshes(CityBoundary boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            if (boundary is CityMultiPoint)
                throw new NotSupportedException("Boundary of type " + typeof(CityMultiPoint) + "Cannot create mesh from multipoint boundary, use CityObjectPointAndLineVisualizer with a PointRenderer3D instead");
            if (boundary is CityMultiLineString)
                throw new NotSupportedException("Boundary of type " + typeof(CityMultiLineString) + "Cannot create mesh from multiLineString boundary, use CityObjectPointAndLineVisualizer with a LineRenderer3D instead");
            if (boundary is CitySurface)
                return BoundariesToMeshes(boundary as CitySurface, coordinateSystem, origin);
            if (boundary is CityMultiOrCompositeSurface)
                return BoundariesToMeshes(boundary as CityMultiOrCompositeSurface, coordinateSystem, origin);
            if (boundary is CitySolid)
                return BoundariesToMeshes(boundary as CitySolid, coordinateSystem, origin);
            if (boundary is CityMultiOrCompositeSolid)
                return BoundariesToMeshes(boundary as CityMultiOrCompositeSolid, coordinateSystem, origin);

            throw new ArgumentException("Unknown boundary type: " + boundary.GetType() + " is not supported to convert to mesh");
        }

        private static List<BoundaryMeshData> BoundariesToMeshes(CitySurface boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            var meshes = new List<BoundaryMeshData>();
            var mesh = CitySurfaceToMesh(boundary, coordinateSystem, origin);
            meshes.Add(mesh);
            return meshes;
        }

        private static List<BoundaryMeshData> BoundariesToMeshes(CityMultiOrCompositeSurface boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            var meshes = new List<BoundaryMeshData>();
            foreach (var surface in boundary.Surfaces)
            {
                var mesh = CitySurfaceToMesh(surface, coordinateSystem, origin);
                meshes.Add(mesh);
            }

            return meshes;
        }

        private static List<BoundaryMeshData> BoundariesToMeshes(CitySolid boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            var meshes = new List<BoundaryMeshData>();
            foreach (var shell in boundary.Shells)
            {
                var shellMeshes = BoundariesToMeshes(shell, coordinateSystem, origin);
                meshes.AddRange(shellMeshes);
            }

            return meshes;
        }

        private static List<BoundaryMeshData> BoundariesToMeshes(CityMultiOrCompositeSolid boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            var meshes = new List<BoundaryMeshData>();
            foreach (var solid in boundary.Solids)
            {
                var solidMeshes = BoundariesToMeshes(solid, coordinateSystem, origin);
                meshes.AddRange(solidMeshes);
            }

            return meshes;
        }

        //create a mesh of a surface.
        private static BoundaryMeshData CitySurfaceToMesh(CitySurface surface, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            if (surface.VertexCount == 0)
                return null;

            List<List<Vector3>> contours = new List<List<Vector3>>(surface.Polygons.Count);
            var convertedVerts = GetConvertedPolygonVertices(surface.SolidSurfacePolygon, coordinateSystem, origin);
            contours.Add(convertedVerts);
            foreach (var hole in surface.HolePolygons)
            {
                contours.Add(GetConvertedPolygonVertices(hole, coordinateSystem, origin));
            }

            var triangulationData = PolygonVisualisationUtility.CreatePolygonGeometryTriangulationData(contours);
            var semanticsObject = surface.SemanticsObject;

            return new BoundaryMeshData(triangulationData, semanticsObject, surface.materialIndices);
        }

        // convert the list of Vector3Doubles to a list of Vector3s and convert the coordinates to unity in the process.
        private static List<Vector3> GetConvertedPolygonVertices(CityPolygon polygon, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            List<Vector3> convertedPolygon = new List<Vector3>(polygon.Vertices.Length);
            foreach (var vert in polygon.Vertices)
            {
                var relativeVert = vert - origin;
                Vector3 convertedVert = relativeVert.AsVector3();

                if (coordinateSystem == CoordinateSystem.RD ||
                    coordinateSystem == CoordinateSystem.RDNAP ||
                    coordinateSystem == CoordinateSystem.Undefined) // todo: make this consistent for all crs
                {
                    convertedVert = new Vector3(convertedVert.x, convertedVert.z, convertedVert.y);
                }

                convertedPolygon.Add(convertedVert);
            }

            convertedPolygon.Reverse();
            return convertedPolygon;
        }

        public override void SetFillColor(Color color)
        {
            foreach (var material in meshRenderer.materials)
            {
                material.color = color;
            }
        }

        public override void SetLineColor(Color color)
        {
        }
    }
}