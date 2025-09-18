using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CityJson.Structure;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.CityJson.Visualisation
{
    public class BoundaryMeshData
    {
        public GeometryTriangulationData TriangulationData;
        public CityGeometrySemanticsObject SemanticsObject;

        public BoundaryMeshData(GeometryTriangulationData triangulationData, CityGeometrySemanticsObject semanticsObject)
        {
            TriangulationData = triangulationData;
            SemanticsObject = semanticsObject;
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
    public class CityObjectVisualizer : MonoBehaviour
    {
        private static int meshesCreatedThisFrame = 0;
        private const int maxMeshesPerFrame = 20;
        private static int lastFrameCount = -1;

        private CityObject cityObject;
        private Dictionary<CityGeometry, MeshWithMaterials> meshes;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        [SerializeField] private int activeLOD;
        public int ActiveLod => activeLOD;
        public Mesh ActiveMesh { get; private set; }

        public UnityEvent<GameObject> jsonVisualized;
        [SerializeField] private SemanticMaterials[] materials;

#if UNITY_EDITOR
        // allow to change the visible LOD from the inspector during runtime
        private void OnValidate()
        {
            if (meshes != null)
                SetLODActive(activeLOD);
        }
#endif
        private void Awake()
        {
            cityObject = GetComponent<CityObject>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }

        private void OnEnable()
        {
            cityObject.CityObjectParsed.AddListener(Visualize);
        }

        private void OnDisable()
        {
            cityObject.CityObjectParsed.RemoveAllListeners();
        }

        //create the meshes
        private void Visualize()
        {
            transform.localPosition = SetLocalPosition(cityObject); //set position first so the CityObject's transformationMatrix can be used to position the mesh.
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
            
            //SetMaterials(cityObject); //todo: create the materials for the meshes
            
            var highestLod = meshes.Keys.Max(g => g.Lod);
            SetLODActive(highestLod);
            
            jsonVisualized?.Invoke(gameObject);
        }

        private Vector3 SetLocalPosition(CityObject cityObject)
        {
            var crs = cityObject.CoordinateSystem;
            
            //todo: Any 2D CRS should be converted to its 3D counterpart (like RD to RDNAP), we should check if we want to do this in the initial CityJSON parsing function or here
            if (cityObject.CoordinateSystem == CoordinateSystem.RD) 
            {
                crs = CoordinateSystem.RDNAP;
            }
            
            var relativeCenter = cityObject.RelativeCenter;
            var relativeCoordinate = new Coordinate(crs, relativeCenter.x, relativeCenter.y, relativeCenter.z); //this is not a valid coordinate, but we need to use the Coordinate struct to determine the axis order
            return new Vector3((float)relativeCoordinate.easting, (float)relativeCoordinate.height, (float)relativeCoordinate.northing);
        }


        //enable the mesh of a certain LOD
        public bool SetLODActive(int lod)
        {
            activeLOD = lod;

            var geometry = meshes.Keys.FirstOrDefault(g => g.Lod == lod);
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

        private Material GetMaterial(SurfaceSemanticType type)
        {
            var mat = materials.FirstOrDefault(m => m.Type == type);
            if (mat != null)
                return mat.Material;

            mat = materials.FirstOrDefault(m => m.Type == SurfaceSemanticType.Null);
            if (mat != null)
                return mat.Material;

            return null;
        }

        //create the meshes for the object geometries
        private Dictionary<CityGeometry, MeshWithMaterials> CreateMeshes(CityObject cityObject)
        {
            meshes = new Dictionary<CityGeometry, MeshWithMaterials>();
            foreach (var geometry in cityObject.Geometries)
            {
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
                    origin = GetComponentInParent<CityJSON>().AbsoluteCenter;//we cannot convert an undefined crs, so we assume the origin is at the absolute center
                }
                // The geometry's vertices are in world space, so we need to subtract the cityJSON's origin to get them in cityJSON space, and then subtract the cityObject's origin to be able to create a mesh with the origin at the cityObject's position.
                // The CityJSON origin is at the citJSON WorldTransform coordinate, the CityObject's origin is its localPosition, since we set it previously.
                var mesh = CreateMeshFromGeometry(geometry, coordinateSystem, origin, cityObject.transform.localPosition); 
                meshes.Add(geometry, mesh);
            }

            return meshes;
        }

        public MeshWithMaterials CreateMeshFromGeometry(CityGeometry geometry, CoordinateSystem coordinateSystem, Vector3Double vertexOffset, Vector3 objectOffset)
        {
            var boundaryMeshes = BoundariesToMeshes(geometry.BoundaryObject, coordinateSystem, vertexOffset);
            var subMeshes = CombineBoundaryMeshesWithTheSameSemanticObject(boundaryMeshes, objectOffset, out var types);
            var materials = new Material[types.Count];

            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = GetMaterial(types[i]);
            }

            var mesh = CombineMeshes(subMeshes, Matrix4x4.identity, false); //use identity matrix because we already transformed the submeshes
            return new MeshWithMaterials(mesh, materials);
        }

        public static List<Mesh> CombineBoundaryMeshesWithTheSameSemanticObject(List<BoundaryMeshData> boundaryMeshes, Vector3 offset, out List<SurfaceSemanticType> types)
        {
            List<Mesh> combinedMeshes = new List<Mesh>(boundaryMeshes.Count);
            types = new List<SurfaceSemanticType>(boundaryMeshes.Count);
            // var offset = 
            while (boundaryMeshes.Count > 0)
            {
                List<GeometryTriangulationData> meshDataToCombine = new List<GeometryTriangulationData>();
                CityGeometrySemanticsObject activeSemanticsObject = boundaryMeshes[boundaryMeshes.Count - 1].SemanticsObject;
                for (int i = boundaryMeshes.Count - 1; i >= 0; i--) //go backwards because collection will be modified
                {
                    var boundaryMesh = boundaryMeshes[i];
                    if (boundaryMesh.SemanticsObject == activeSemanticsObject)
                    {
                        if (boundaryMesh.TriangulationData != null) //skip invalid polygons
                            meshDataToCombine.Add(boundaryMesh.TriangulationData);
                        boundaryMeshes.Remove(boundaryMesh);
                    }
                }

                var combinedMesh = PolygonVisualisationUtility.CreatePolygonMesh(meshDataToCombine, offset);
                combinedMeshes.Add(combinedMesh);
                if (activeSemanticsObject != null)
                    types.Add(activeSemanticsObject.SurfaceType);
                else
                    types.Add(SurfaceSemanticType.Null);
            }

            return combinedMeshes;
        }

        public static Mesh CombineMeshes(List<Mesh> meshes, Matrix4x4 transformationMatrix, bool mergeSubMeshes)
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
        protected virtual List<BoundaryMeshData> BoundariesToMeshes(CityBoundary boundary, CoordinateSystem coordinateSystem, Vector3Double origin)
        {
            if (boundary is CityMultiPoint)
                throw new NotSupportedException("Boundary of type " + typeof(CityMultiPoint) + "is not supported by this Visualiser script since it contains no mesh data. Use MultiPointVisualiser instead and assign an object to use as visualization of the points");
            if (boundary is CityMultiLineString) //todo this boundary type is not supported at all
                throw new NotSupportedException("Boundary of type " + typeof(CityMultiLineString) + "is currently not supported.");
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
            
            return new BoundaryMeshData(triangulationData, semanticsObject);
        }
        
        // convert the list of Vector3Doubles to a list of Vector3s and convert the coordinates to unity in the process.
        public static List<Vector3> GetConvertedPolygonVertices(CityPolygon polygon, CoordinateSystem coordinateSystem, Vector3Double origin)
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
    }
}