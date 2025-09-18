using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.CityJson.Structure
{
    /// <summary>
    /// Class to represent and parse a CityJSON. Currently based on CityJSON v1.0.3
    /// https://www.cityjson.org/specs/1.0.3/
    /// </summary>
    public class CityJSON : MonoBehaviour
    {
        //nodes defined in a CityJSON and therefore reserved node keys.
        private static string[] definedNodes = { "type", "version", "CityObjects", "vertices", "extensions", "metadata", "transform", "appearance", "geometry-templates" };

        public string Version { get; private set; } = "1.0"; //default version
        public JSONNode Extensions { get; private set; }
        public JSONNode Metadata { get; private set; }
        public Vector3Double TransformScale { get; private set; } = new Vector3Double(1d, 1d, 1d);
        public Vector3Double TransformTranslate { get; private set; } = new Vector3Double(0d, 0d, 0d);
        public JSONNode Appearance { get; private set; }
        public JSONNode GeometryTemplates { get; private set; }

        public List<CityObject> CityObjects { get; private set; } = new List<CityObject>();
        public Vector3Double MinExtent { get; private set; }
        public Vector3Double MaxExtent { get; private set; }
        public Vector3Double RelativeCenter => (MaxExtent - MinExtent) / 2;
        public Vector3Double AbsoluteCenter => (MaxExtent + MinExtent) / 2;

        public CoordinateSystem CoordinateSystem { get; private set; } = CoordinateSystem.Undefined;

        private Dictionary<string, JSONNode> extensionNodes = new Dictionary<string, JSONNode>();

        [Tooltip("A cityObject will be created as a GameObject with a CityObject script. This field can hold a prefab with multiple extra scripts (such as CityObjectVisualizer) to be created instead. This prefab must have a CityObject script attached.")] [SerializeField]
        private GameObject cityObjectPrefab;

        [Header("Optional events")] 
        [Tooltip("Event that is called when the CityJSON is parsed")] 
        public UnityEvent onAllCityObjectsProcessed;

        [Tooltip("If assigned it will call this event instead of Asserting the type field is \"CityJSON\"")]
        public UnityEvent<bool> isCityJSONType; //if assigned it will call this event instead of Asserting the type field is "CityJSON"

        public static CityJSON CreateEmpty()
        {
            var cityJSON = new GameObject("CityJSON").AddComponent<CityJSON>();
            return cityJSON;
        }

        public CityObject AddEmptyCityObject(string id)
        {
            var cityObject = CityObject.CreateEmpty(id);
            cityObject.transform.SetParent(transform);
            CityObjects.Add(cityObject);
            return cityObject;
        }

        public void ParseCityJSON(string cityJson)
        {
            //remove old data if re-parsing
            foreach (var co in CityObjects)
            {
                co.UnparentFromAll(); //needed because OnDestroy is not immediately called.
                Destroy(co.gameObject);
                CityObjects = new List<CityObject>(); //reset this in case an invalid CityJSON is parsed after a succesful parse
            }

            RemoveExtensionNodes(extensionNodes);

            //parse
            var node = JSONNode.Parse(cityJson);

            var type = node["type"];
            var isCityJSON = type == "CityJSON";

            isCityJSONType.Invoke(isCityJSON);
            if (!isCityJSON)
            {
                Debug.LogError("The provided string is not a CityJSON");
                return;
            }

            Version = node["version"];

            //optional data
            Extensions = node["extensions"];
            Metadata = node["metadata"];
            Appearance = node["appearance"]; //todo: not implemented yet
            GeometryTemplates = node["geometry-templates"]; //todo: not implemented yet
            var transformNode = node["transform"];

            if (transformNode.Count > 0)
            {
                TransformScale = new Vector3Double(transformNode["scale"][0], transformNode["scale"][1], transformNode["scale"][2]);
                TransformTranslate = new Vector3Double(transformNode["translate"][0], transformNode["translate"][1], transformNode["translate"][2]);
            }

            //custom nodes must be added as is to the exporter to ensure they are preserved.
            AddExtensionNodesToExporter(node, out extensionNodes);

            //vertices
            List<Vector3Double> parsedVertices = new List<Vector3Double>(node["vertices"].Count);
            foreach (var vertArray in node["vertices"])
            {
                var vert = new Vector3Double(vertArray.Value.AsArray);
                vert *= TransformScale;
                vert += TransformTranslate;
                parsedVertices.Add(vert);
            }

            if (parsedVertices.Count == 0)
            {
                Debug.LogWarning("Vertex list is empty, nothing can be visualized because empty meshes will be created!");
            }

            //metadata
            var explicitGeographicalExtentsSet = false;
            if (Metadata.Count > 0)
            {
                var coordinateSystemNode = Metadata["referenceSystem"];
                CoordinateSystem = CoordinateSystems.FindCoordinateSystem(coordinateSystemNode.Value);

                var geographicalExtent = Metadata["geographicalExtent"];
                if (geographicalExtent.Count > 0)
                {
                    explicitGeographicalExtentsSet = true;
                    MinExtent = new Vector3Double(geographicalExtent[0].AsDouble, geographicalExtent[1].AsDouble, geographicalExtent[2].AsDouble);
                    MaxExtent = new Vector3Double(geographicalExtent[3].AsDouble, geographicalExtent[4].AsDouble, geographicalExtent[5].AsDouble);
                }
            }

            if (!explicitGeographicalExtentsSet)
            {
                var minX = parsedVertices.Min(v => v.x);
                var minY = parsedVertices.Min(v => v.y);
                var minZ = parsedVertices.Min(v => v.z);
                var maxX = parsedVertices.Max(v => v.x);
                var maxY = parsedVertices.Max(v => v.y);
                var maxZ = parsedVertices.Max(v => v.z);

                MinExtent = new Vector3Double(minX, minY, minZ);
                MaxExtent = new Vector3Double(maxX, maxY, maxZ);
            }
            
            //todo: this is a quick fix for current RD files that don't have a CRS defined but are in RDNAP.
            //The real fix is to add IsValid functions for all supported CRS options and return a list of the ones that could be valid. The user can then choose which one to use, from the list, or if there is only 1 CRS in the list, default to this one.
            var absoluteCenter = AbsoluteCenter;
            if (EPSG7415.IsValid(absoluteCenter.x, absoluteCenter.y, absoluteCenter.z, out var _)) 
            {
                CoordinateSystem = CoordinateSystem.RDNAP;
            }

            //CityObjects
            Dictionary<JSONNode, CityObject> cityObjects = new Dictionary<JSONNode, CityObject>();
            foreach (var cityObjectNode in node["CityObjects"])
            {
                GameObject go;
                CityObject co;
                if (cityObjectPrefab == null)
                {
                    go = new GameObject(cityObjectNode.Key);
                    go.transform.SetParent(transform);
                    co = go.AddComponent<CityObject>();
                }
                else
                {
                    go = Instantiate(cityObjectPrefab, transform);
                    co = go.GetComponent<CityObject>();
                }

                co.FromJSONNode(cityObjectNode.Key, cityObjectNode.Value, CoordinateSystem, parsedVertices);
                cityObjects.Add(cityObjectNode.Value, co);
            }

            //after creating all the objects, set the parent/child relations. this can only be done after, since the referenced parent/child might not exist yet during initialization in the previous loop
            foreach (var co in cityObjects)
            {
                var parents = co.Key["parents"];
                var parentObjects = new CityObject[parents.Count];
                for (int i = 0; i < parents.Count; i++)
                {
                    string parentId = parents[i];
                    parentObjects[i] = cityObjects.First(co => co.Value.Id == parentId).Value;
                }

                co.Value.SetParents(parentObjects);
            }

            CityObjects = cityObjects.Values.ToList();

            if (TryGetComponent<WorldTransform>(out var worldTransform))
                MoveToAbsoluteCenter(worldTransform);

            foreach (var co in CityObjects)
            {
                co.OnCityObjectParseCompleted();
            }

            onAllCityObjectsProcessed?.Invoke();
        }

        //custom nodes must be added as is to the exporter to ensure they are preserved.
        public static void AddExtensionNodesToExporter(JSONNode cityJsonNode, out Dictionary<string, JSONNode> extensionNodes)
        {
            extensionNodes = new Dictionary<string, JSONNode>();
            foreach (var node in cityJsonNode)
            {
                if (definedNodes.Contains(node.Key))
                    continue;

                extensionNodes.Add(node.Key, node.Value);
                CityJSONFormatter.AddExtensionNode(node.Key, node.Value);
            }
        }

        // function to remove Extension nodes for use in future scripts that may need to do so
        public static void RemoveExtensionNodes(Dictionary<string, JSONNode> extensionNodes)
        {
            foreach (var node in extensionNodes)
            {
                if (definedNodes.Contains(node.Key))
                    continue;

                CityJSONFormatter.RemoveExtensionNode(node.Key);
            }
        }

        // set the relative RD center to avoid floating point issues of GameObject far from the Unity origin
        private void MoveToAbsoluteCenter(WorldTransform worldTransform)
        {
            var absoluteCenter = AbsoluteCenter;
            var coord = new Coordinate(CoordinateSystem, absoluteCenter.x, absoluteCenter.y, absoluteCenter.z);
            worldTransform.MoveToCoordinate(coord);
        }
    }
}