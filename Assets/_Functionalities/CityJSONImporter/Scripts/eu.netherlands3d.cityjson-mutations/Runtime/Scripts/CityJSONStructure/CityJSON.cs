using Netherlands3D.Coordinates;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        // Nodes defined in a CityJSON and therefore reserved node keys.
        private static string[] definedNodes =
        {
            "type",
            "version",
            "CityObjects",
            "vertices",
            "extensions",
            "metadata",
            "transform",
            "appearance",
            "geometry-templates"
        };

        public string Version { get; private set; } = "1.0";
        public JSONNode Extensions { get; private set; }
        public JSONNode Metadata { get; private set; }

        public Vector3Double TransformScale { get; private set; } =
            new Vector3Double(1d, 1d, 1d);

        public Vector3Double TransformTranslate { get; private set; } =
            new Vector3Double(0d, 0d, 0d);

        public CityAppearance Appearance { get; private set; }
        public JSONNode GeometryTemplates { get; private set; }

        public List<CityObject> CityObjects { get; private set; } =
            new List<CityObject>();

        public Vector3Double MinExtent { get; private set; }
        public Vector3Double MaxExtent { get; private set; }

        public Vector3Double RelativeCenter =>
            (MaxExtent - MinExtent) / 2;

        public Vector3Double AbsoluteCenter =>
            (MaxExtent + MinExtent) / 2;

        public CoordinateSystem CoordinateSystem { get; private set; } =
            CoordinateSystem.Undefined;

        private Dictionary<string, JSONNode> extensionNodes =
            new Dictionary<string, JSONNode>();

        [Tooltip("A cityObject will be created as a GameObject with a CityObject script. This field can hold a prefab with multiple extra scripts (such as CityObjectVisualizer) to be created instead. This prefab must have a CityObject script attached.")]
        [SerializeField]
        private GameObject cityObjectPrefab;

        [Header("Optional events")]
        [Tooltip("Event that is called when the CityJSON is parsed")]
        public UnityEvent onAllCityObjectsProcessed;

        [Tooltip("If assigned it will call this event instead of Asserting the type field is \"CityJSON\"")]
        public UnityEvent<bool> isCityJSONType;

        // ---------------------------------------------------------------------
        // NORMAL PARSER
        // ---------------------------------------------------------------------

        public static CityJSON CreateEmpty()
        {
            var cityJSON =
                new GameObject("CityJSON").AddComponent<CityJSON>();

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
            ClearExistingCityObjects();

            RemoveExtensionNodes(extensionNodes);

            // Original synchronous parser.
            var node = JSONNode.Parse(cityJson);

            var type = node["type"];
            var isCityJSON = type == "CityJSON";

            isCityJSONType?.Invoke(isCityJSON);

            if (!isCityJSON)
            {
                Debug.LogError(
                    "The provided string is not a CityJSON"
                );

                return;
            }

            Version = node["version"];

            // Optional data
            Extensions = node["extensions"];
            Metadata = node["metadata"];

            Appearance =
                CityAppearance.FromJSON(node["appearance"]);

            GeometryTemplates =
                node["geometry-templates"];

            var transformNode = node["transform"];

            if (transformNode != null &&
                transformNode.Count > 0)
            {
                TransformScale =
                    new Vector3Double(
                        transformNode["scale"][0],
                        transformNode["scale"][1],
                        transformNode["scale"][2]
                    );

                TransformTranslate =
                    new Vector3Double(
                        transformNode["translate"][0],
                        transformNode["translate"][1],
                        transformNode["translate"][2]
                    );
            }

            AddExtensionNodesToExporter(
                node,
                out extensionNodes
            );

            // -------------------------------------------------------------
            // Vertices
            // -------------------------------------------------------------

            List<Vector3Double> parsedVertices =
                new List<Vector3Double>(
                    node["vertices"].Count
                );

            foreach (var vertArray in node["vertices"])
            {
                var vert =
                    new Vector3Double(
                        vertArray.Value.AsArray
                    );

                vert *= TransformScale;
                vert += TransformTranslate;

                parsedVertices.Add(vert);
            }

            if (parsedVertices.Count == 0)
            {
                Debug.LogWarning(
                    "Vertex list is empty, nothing can be visualized because empty meshes will be created!"
                );
            }

            CalculateExtents(parsedVertices);

            // -------------------------------------------------------------
            // CityObjects
            // -------------------------------------------------------------

            Dictionary<JSONNode, CityObject> cityObjects =
                new Dictionary<JSONNode, CityObject>();

            foreach (var cityObjectNode in node["CityObjects"])
            {
                CityObject co =
                    CreateCityObject(
                        cityObjectNode.Key,
                        cityObjectNode.Value,
                        parsedVertices
                    );

                cityObjects.Add(
                    cityObjectNode.Value,
                    co
                );
            }

            ResolveParents(cityObjects);

            CityObjects =
                cityObjects.Values.ToList();

            CompleteCityObjects();
        }

        // ---------------------------------------------------------------------
        // STREAMING PARSER
        // ---------------------------------------------------------------------

        /// <summary>
        /// Streams a CityJSON file from disk instead of loading the entire
        /// file into memory with File.ReadAllText / JSONNode.Parse.
        ///
        /// Start it with:
        ///
        /// StartCoroutine(cityJson.ParseCityJSONStreaming(path));
        ///
        /// The coroutine yields after each CityObject and therefore gives
        /// Unity the opportunity to render frames while loading.
        /// </summary>
        public IEnumerator ParseCityJSONStreaming(string filePath)
        {
            ClearExistingCityObjects();

            RemoveExtensionNodes(extensionNodes);

            extensionNodes =
                new Dictionary<string, JSONNode>();

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError(
                    "Cannot stream CityJSON: file path is empty."
                );

                yield break;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogError(
                    $"Cannot stream CityJSON: file does not exist:\n{filePath}"
                );

                yield break;
            }

            StreamReader reader = null;

            // try
            {
                reader =
                    new StreamReader(
                        filePath,
                        Encoding.UTF8,
                        true,
                        1024 * 1024
                    );

                using (reader)
                {
                    var jsonReader =
                        new StreamingJsonReader(reader);

                    // ---------------------------------------------------------
                    // Root object
                    // ---------------------------------------------------------

                    jsonReader.SkipWhitespace();
                    jsonReader.Expect('{');

                    List<Vector3Double> parsedVertices = null;

                    // CityObjects can normally be processed after vertices.
                    // We keep them temporarily if a file puts CityObjects
                    // before vertices.
                    List<PendingCityObject> pendingCityObjects =
                        new List<PendingCityObject>();

                    while (true)
                    {
                        jsonReader.SkipWhitespace();

                        if (jsonReader.TryConsume('}'))
                            break;

                        string propertyName =
                            jsonReader.ReadString();

                        jsonReader.SkipWhitespace();
                        jsonReader.Expect(':');
                        jsonReader.SkipWhitespace();

                        // -----------------------------------------------------
                        // type
                        // -----------------------------------------------------

                        if (propertyName == "type")
                        {
                            string typeValue =
                                jsonReader.ReadString();

                            bool isCityJSON =
                                typeValue == "CityJSON";

                            isCityJSONType?.Invoke(
                                isCityJSON
                            );

                            if (!isCityJSON)
                            {
                                Debug.LogError(
                                    "The provided file is not a CityJSON."
                                );

                                yield break;
                            }
                        }

                        // -----------------------------------------------------
                        // version
                        // -----------------------------------------------------

                        else if (propertyName == "version")
                        {
                            Version =
                                jsonReader.ReadString();
                        }

                        // -----------------------------------------------------
                        // extensions
                        // -----------------------------------------------------

                        else if (propertyName == "extensions")
                        {
                            string raw =
                                jsonReader.ReadRawValue();

                            Extensions =
                                JSONNode.Parse(raw);

                            AddExtensionNodesToExporter(
                                Extensions,
                                out extensionNodes
                            );
                        }

                        // -----------------------------------------------------
                        // metadata
                        // -----------------------------------------------------

                        else if (propertyName == "metadata")
                        {
                            string raw =
                                jsonReader.ReadRawValue();

                            Metadata =
                                JSONNode.Parse(raw);
                        }

                        // -----------------------------------------------------
                        // appearance
                        // -----------------------------------------------------

                        else if (propertyName == "appearance")
                        {
                            string raw =
                                jsonReader.ReadRawValue();

                            var appearanceNode =
                                JSONNode.Parse(raw);

                            Appearance =
                                CityAppearance.FromJSON(
                                    appearanceNode
                                );
                        }

                        // -----------------------------------------------------
                        // geometry templates
                        // -----------------------------------------------------

                        else if (propertyName == "geometry-templates")
                        {
                            string raw =
                                jsonReader.ReadRawValue();

                            GeometryTemplates =
                                JSONNode.Parse(raw);
                        }

                        // -----------------------------------------------------
                        // transform
                        // -----------------------------------------------------

                        else if (propertyName == "transform")
                        {
                            string raw =
                                jsonReader.ReadRawValue();

                            JSONNode transformNode =
                                JSONNode.Parse(raw);

                            if (transformNode != null &&
                                transformNode.Count > 0)
                            {
                                TransformScale =
                                    new Vector3Double(
                                        transformNode["scale"][0],
                                        transformNode["scale"][1],
                                        transformNode["scale"][2]
                                    );

                                TransformTranslate =
                                    new Vector3Double(
                                        transformNode["translate"][0],
                                        transformNode["translate"][1],
                                        transformNode["translate"][2]
                                    );
                            }
                        }

                        // -----------------------------------------------------
                        // vertices
                        // -----------------------------------------------------

                        else if (propertyName == "vertices")
                        {
                            parsedVertices =
                                ReadVerticesStreaming(
                                    jsonReader
                                );

                            if (parsedVertices.Count == 0)
                            {
                                Debug.LogWarning(
                                    "Vertex list is empty, nothing can be visualized because empty meshes will be created!"
                                );
                            }

                            CalculateExtents(
                                parsedVertices
                            );

                            // If CityObjects were encountered before
                            // vertices, we can now process them.
                            if (pendingCityObjects.Count > 0)
                            {
                                foreach (
                                    var pending
                                    in pendingCityObjects
                                )
                                {
                                    CityObject co =
                                        CreateCityObject(
                                            pending.Id,
                                            pending.Node,
                                            parsedVertices
                                        );

                                    pending.CreatedObject = co;

                                    yield return null;
                                }
                            }
                        }

                        // -----------------------------------------------------
                        // CityObjects
                        // -----------------------------------------------------

                        else if (propertyName == "CityObjects")
                        {
                            jsonReader.SkipWhitespace();
                            jsonReader.Expect('{');

                            while (true)
                            {
                                jsonReader.SkipWhitespace();

                                if (jsonReader.TryConsume('}'))
                                    break;

                                string id =
                                    jsonReader.ReadString();

                                jsonReader.SkipWhitespace();
                                jsonReader.Expect(':');
                                jsonReader.SkipWhitespace();

                                // Read only THIS CityObject into memory.
                                //
                                // We do not parse the entire CityJSON file
                                // into one enormous SimpleJSON tree.
                                string rawCityObject =
                                    jsonReader.ReadRawValue();

                                JSONNode cityObjectNode =
                                    JSONNode.Parse(
                                        rawCityObject
                                    );

                                if (cityObjectNode == null)
                                {
                                    throw new Exception(
                                        $"Could not parse CityObject '{id}'."
                                    );
                                }

                                if (parsedVertices != null)
                                {
                                    CityObject co =
                                        CreateCityObject(
                                            id,
                                            cityObjectNode,
                                            parsedVertices
                                        );

                                    // Store the node for parent resolution.
                                    pendingCityObjects.Add(
                                        new PendingCityObject
                                        {
                                            Id = id,
                                            Node = cityObjectNode,
                                            CreatedObject = co
                                        }
                                    );
                                }
                                else
                                {
                                    pendingCityObjects.Add(
                                        new PendingCityObject
                                        {
                                            Id = id,
                                            Node = cityObjectNode,
                                            CreatedObject = null
                                        }
                                    );
                                }

                                // IMPORTANT:
                                // Give Unity a frame after every CityObject.
                                yield return null;

                                jsonReader.SkipWhitespace();

                                if (jsonReader.TryConsume(','))
                                    continue;

                                jsonReader.SkipWhitespace();
                                jsonReader.Expect('}');
                                break;
                            }
                        }

                        // -----------------------------------------------------
                        // Unknown/custom root node
                        // -----------------------------------------------------

                        else
                        {
                            string raw =
                                jsonReader.ReadRawValue();

                            JSONNode customNode =
                                JSONNode.Parse(raw);

                            if (customNode != null)
                            {
                                extensionNodes[propertyName] =
                                    customNode;

                                CityJSONFormatter.AddExtensionNode(
                                    propertyName,
                                    customNode
                                );
                            }
                        }

                        // -----------------------------------------------------
                        // Root property separator
                        // -----------------------------------------------------

                        jsonReader.SkipWhitespace();

                        if (jsonReader.TryConsume(','))
                        {
                            continue;
                        }

                        jsonReader.SkipWhitespace();
                        jsonReader.Expect('}');

                        break;
                    }

                    // ---------------------------------------------------------
                    // If CityObjects came before vertices, create them now.
                    // ---------------------------------------------------------

                    if (parsedVertices == null)
                    {
                        parsedVertices =
                            new List<Vector3Double>();
                    }

                    foreach (
                        var pending
                        in pendingCityObjects)
                    {
                        if (pending.CreatedObject == null)
                        {
                            pending.CreatedObject =
                                CreateCityObject(
                                    pending.Id,
                                    pending.Node,
                                    parsedVertices
                                );

                            yield return null;
                        }
                    }

                    // ---------------------------------------------------------
                    // Resolve parents
                    // ---------------------------------------------------------

                    Dictionary<JSONNode, CityObject>
                        cityObjects =
                        new Dictionary<JSONNode, CityObject>();

                    foreach (
                        var pending
                        in pendingCityObjects)
                    {
                        if (pending.CreatedObject != null)
                        {
                            cityObjects.Add(
                                pending.Node,
                                pending.CreatedObject
                            );
                        }
                    }

                    ResolveParents(cityObjects);

                    CityObjects =
                        cityObjects.Values.ToList();

                    // ---------------------------------------------------------
                    // Complete
                    // ---------------------------------------------------------

                    CompleteCityObjects();

                    yield return null;
                }
            }
            // catch (Exception e)
            // {
            //     Debug.LogError(
            //         $"Error while streaming CityJSON:\n{e.Message}\n\n{e.StackTrace}"
            //     );
            // }
            // finally
            {
                if (reader != null)
                    reader.Dispose();
            }
        }

        // ---------------------------------------------------------------------
        // CREATE CITY OBJECT
        // ---------------------------------------------------------------------

        private CityObject CreateCityObject(
            string id,
            JSONNode node,
            List<Vector3Double> parsedVertices)
        {
            GameObject go;
            CityObject co;

            if (cityObjectPrefab == null)
            {
                go = new GameObject(id);
                go.transform.SetParent(transform);

                co =
                    go.AddComponent<CityObject>();
            }
            else
            {
                go =
                    Instantiate(
                        cityObjectPrefab,
                        transform
                    );

                co =
                    go.GetComponent<CityObject>();
            }

            co.SetCityAppearance(Appearance);

            co.FromJSONNode(
                id,
                node,
                CoordinateSystem,
                parsedVertices
            );

            return co;
        }

        // ---------------------------------------------------------------------
        // EXTENTS
        // ---------------------------------------------------------------------

        private void CalculateExtents(
            List<Vector3Double> parsedVertices)
        {
            bool explicitGeographicalExtentsSet =
                false;

            if (Metadata != null &&
                Metadata.Count > 0)
            {
                var coordinateSystemNode =
                    Metadata["referenceSystem"];

                if (coordinateSystemNode != null)
                {
                    CoordinateSystem =
                        CoordinateSystems.FindCoordinateSystem(
                            coordinateSystemNode.Value
                        );
                }

                var geographicalExtent =
                    Metadata["geographicalExtent"];

                if (geographicalExtent != null &&
                    geographicalExtent.Count > 0)
                {
                    explicitGeographicalExtentsSet = true;

                    MinExtent =
                        new Vector3Double(
                            geographicalExtent[0].AsDouble,
                            geographicalExtent[1].AsDouble,
                            geographicalExtent[2].AsDouble
                        );

                    MaxExtent =
                        new Vector3Double(
                            geographicalExtent[3].AsDouble,
                            geographicalExtent[4].AsDouble,
                            geographicalExtent[5].AsDouble
                        );
                }
            }

            if (!explicitGeographicalExtentsSet)
            {
                if (parsedVertices == null ||
                    parsedVertices.Count == 0)
                {
                    MinExtent =
                        new Vector3Double();

                    MaxExtent =
                        new Vector3Double();

                    return;
                }

                var minX =
                    parsedVertices.Min(v => v.x);

                var minY =
                    parsedVertices.Min(v => v.y);

                var minZ =
                    parsedVertices.Min(v => v.z);

                var maxX =
                    parsedVertices.Max(v => v.x);

                var maxY =
                    parsedVertices.Max(v => v.y);

                var maxZ =
                    parsedVertices.Max(v => v.z);

                MinExtent =
                    new Vector3Double(
                        minX,
                        minY,
                        minZ
                    );

                MaxExtent =
                    new Vector3Double(
                        maxX,
                        maxY,
                        maxZ
                    );
            }

            var absoluteCenter =
                AbsoluteCenter;

            if (CoordinateSystems.TryFindValidCoordinates(
                    absoluteCenter.x,
                    absoluteCenter.y,
                    absoluteCenter.z,
                    out var possibleCoordinates))
            {
                if (possibleCoordinates.Count > 0)
                {
                    CoordinateSystem =
                        (CoordinateSystem)
                        possibleCoordinates[0]
                            .CoordinateSystem;
                }
            }
        }

        // ---------------------------------------------------------------------
        // PARENT RELATIONS
        // ---------------------------------------------------------------------

        private void ResolveParents(
            Dictionary<JSONNode, CityObject> cityObjects)
        {
            foreach (var co in cityObjects)
            {
                var parents =
                    co.Key["parents"];

                if (parents == null ||
                    parents.Count == 0)
                {
                    co.Value.SetParents(
                        new CityObject[0]
                    );

                    continue;
                }

                var parentObjects =
                    new CityObject[parents.Count];

                for (int i = 0; i < parents.Count; i++)
                {
                    string parentId =
                        parents[i];

                    var parent =
                        cityObjects.FirstOrDefault(
                            x => x.Value.Id == parentId
                        );

                    if (parent.Value == null)
                    {
                        Debug.LogWarning(
                            $"Could not find parent '{parentId}' for CityObject '{co.Value.Id}'."
                        );

                        continue;
                    }

                    parentObjects[i] =
                        parent.Value;
                }

                co.Value.SetParents(
                    parentObjects
                );
            }
        }

        // ---------------------------------------------------------------------
        // COMPLETE
        // ---------------------------------------------------------------------

        private void CompleteCityObjects()
        {
            foreach (var co in CityObjects)
            {
                co.OnCityObjectParseCompleted();
            }

            onAllCityObjectsProcessed?.Invoke();
        }

        // ---------------------------------------------------------------------
        // CLEANUP
        // ---------------------------------------------------------------------

        private void ClearExistingCityObjects()
        {
            foreach (var co in CityObjects)
            {
                if (co == null)
                    continue;

                co.UnparentFromAll();

                Destroy(co.gameObject);
            }

            CityObjects =
                new List<CityObject>();
        }

        // ---------------------------------------------------------------------
        // EXTENSION NODES
        // ---------------------------------------------------------------------

        public static void AddExtensionNodesToExporter(
            JSONNode cityJsonNode,
            out Dictionary<string, JSONNode> extensionNodes)
        {
            extensionNodes =
                new Dictionary<string, JSONNode>();

            if (cityJsonNode == null)
                return;

            foreach (var node in cityJsonNode)
            {
                if (definedNodes.Contains(node.Key))
                    continue;

                extensionNodes.Add(
                    node.Key,
                    node.Value
                );

                CityJSONFormatter.AddExtensionNode(
                    node.Key,
                    node.Value
                );
            }
        }

        public static void RemoveExtensionNodes(
            Dictionary<string, JSONNode> extensionNodes)
        {
            if (extensionNodes == null)
                return;

            foreach (var node in extensionNodes)
            {
                if (definedNodes.Contains(node.Key))
                    continue;

                CityJSONFormatter.RemoveExtensionNode(
                    node.Key
                );
            }
        }

        // ---------------------------------------------------------------------
        // STREAMING VERTICES
        // ---------------------------------------------------------------------

        private List<Vector3Double> ReadVerticesStreaming(
            StreamingJsonReader reader)
        {
            var vertices =
                new List<Vector3Double>();

            reader.SkipWhitespace();
            reader.Expect('[');

            while (true)
            {
                reader.SkipWhitespace();

                if (reader.TryConsume(']'))
                    break;

                reader.Expect('[');

                reader.SkipWhitespace();

                double x =
                    reader.ReadNumber();

                reader.SkipWhitespace();
                reader.Expect(',');

                reader.SkipWhitespace();

                double y =
                    reader.ReadNumber();

                reader.SkipWhitespace();
                reader.Expect(',');

                reader.SkipWhitespace();

                double z =
                    reader.ReadNumber();

                reader.SkipWhitespace();
                reader.Expect(']');

                var vert =
                    new Vector3Double(
                        x,
                        y,
                        z
                    );

                vert *= TransformScale;
                vert += TransformTranslate;

                vertices.Add(vert);

                reader.SkipWhitespace();

                if (reader.TryConsume(','))
                    continue;

                reader.SkipWhitespace();
                reader.Expect(']');

                break;
            }

            return vertices;
        }

        // ---------------------------------------------------------------------
        // PENDING CITY OBJECT
        // ---------------------------------------------------------------------

        private class PendingCityObject
        {
            public string Id;
            public JSONNode Node;
            public CityObject CreatedObject;
        }

        // =====================================================================
        // STREAMING JSON READER
        // =====================================================================

        private class StreamingJsonReader
        {
            private readonly TextReader reader;

            private int currentCharacter = -2;

            public StreamingJsonReader(
                TextReader reader)
            {
                this.reader = reader;
            }

            private int ReadCharacter()
            {
                if (currentCharacter != -2)
                {
                    int result =
                        currentCharacter;

                    currentCharacter = -2;

                    return result;
                }

                return reader.Read();
            }

            private int PeekCharacter()
            {
                if (currentCharacter == -2)
                    currentCharacter =
                        reader.Read();

                return currentCharacter;
            }

            public void SkipWhitespace()
            {
                while (true)
                {
                    int c =
                        PeekCharacter();

                    if (c == -1)
                        return;

                    if (!char.IsWhiteSpace(
                            (char)c))
                    {
                        return;
                    }

                    ReadCharacter();
                }
            }

            public bool TryConsume(char expected)
            {
                SkipWhitespace();

                int c =
                    PeekCharacter();

                if (c != expected)
                    return false;

                ReadCharacter();

                return true;
            }

            public void Expect(char expected)
            {
                SkipWhitespace();

                int c =
                    ReadCharacter();

                if (c != expected)
                {
                    throw new FormatException(
                        $"Expected '{expected}' but found '{(char)c}'."
                    );
                }
            }

            public string ReadString()
            {
                SkipWhitespace();

                int opening =
                    ReadCharacter();

                if (opening != '"')
                {
                    throw new FormatException(
                        "Expected JSON string."
                    );
                }

                StringBuilder result =
                    new StringBuilder();

                while (true)
                {
                    int c =
                        ReadCharacter();

                    if (c == -1)
                    {
                        throw new FormatException(
                            "Unexpected end of JSON string."
                        );
                    }

                    if (c == '"')
                        break;

                    if (c == '\\')
                    {
                        int escaped =
                            ReadCharacter();

                        switch (escaped)
                        {
                            case '"':
                                result.Append('"');
                                break;

                            case '\\':
                                result.Append('\\');
                                break;

                            case '/':
                                result.Append('/');
                                break;

                            case 'b':
                                result.Append('\b');
                                break;

                            case 'f':
                                result.Append('\f');
                                break;

                            case 'n':
                                result.Append('\n');
                                break;

                            case 'r':
                                result.Append('\r');
                                break;

                            case 't':
                                result.Append('\t');
                                break;

                            case 'u':
                            {
                                int code =
                                    0;

                                for (int i = 0; i < 4; i++)
                                {
                                    int hex =
                                        ReadCharacter();

                                    int value =
                                        HexValue(hex);

                                    if (value < 0)
                                    {
                                        throw new FormatException(
                                            "Invalid unicode escape."
                                        );
                                    }

                                    code =
                                        (code << 4) |
                                        value;
                                }

                                result.Append(
                                    (char)code
                                );

                                break;
                            }

                            default:
                                throw new FormatException(
                                    $"Invalid escape character '\\{(char)escaped}'."
                                );
                        }

                        continue;
                    }

                    result.Append(
                        (char)c
                    );
                }

                return result.ToString();
            }

            public double ReadNumber()
            {
                SkipWhitespace();

                StringBuilder number =
                    new StringBuilder();

                int c =
                    PeekCharacter();

                if (c == '-')
                {
                    number.Append(
                        (char)ReadCharacter()
                    );
                }

                while (true)
                {
                    c =
                        PeekCharacter();

                    if (c < '0' ||
                        c > '9')
                    {
                        break;
                    }

                    number.Append(
                        (char)ReadCharacter()
                    );
                }

                c =
                    PeekCharacter();

                if (c == '.')
                {
                    number.Append(
                        (char)ReadCharacter()
                    );

                    while (true)
                    {
                        c =
                            PeekCharacter();

                        if (c < '0' ||
                            c > '9')
                        {
                            break;
                        }

                        number.Append(
                            (char)ReadCharacter()
                        );
                    }
                }

                c =
                    PeekCharacter();

                if (c == 'e' ||
                    c == 'E')
                {
                    number.Append(
                        (char)ReadCharacter()
                    );

                    c =
                        PeekCharacter();

                    if (c == '+' ||
                        c == '-')
                    {
                        number.Append(
                            (char)ReadCharacter()
                        );
                    }

                    while (true)
                    {
                        c =
                            PeekCharacter();

                        if (c < '0' ||
                            c > '9')
                        {
                            break;
                        }

                        number.Append(
                            (char)ReadCharacter()
                        );
                    }
                }

                if (!double.TryParse(
                        number.ToString(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double value))
                {
                    throw new FormatException(
                        $"Invalid JSON number: {number}"
                    );
                }

                return value;
            }

            /// <summary>
            /// Reads one complete JSON value from the stream and returns it
            /// as text. This is intentionally done per value rather than for
            /// the entire CityJSON document.
            /// </summary>
            public string ReadRawValue()
            {
                SkipWhitespace();

                int first =
                    PeekCharacter();

                if (first == '"')
                    return ReadRawString();

                StringBuilder result =
                    new StringBuilder();

                int depth = 0;
                bool insideString = false;
                bool escaped = false;

                while (true)
                {
                    int c =
                        PeekCharacter();

                    if (c == -1)
                        break;

                    if (insideString)
                    {
                        result.Append(
                            (char)ReadCharacter()
                        );

                        if (escaped)
                        {
                            escaped = false;
                        }
                        else if (c == '\\')
                        {
                            escaped = true;
                        }
                        else if (c == '"')
                        {
                            insideString = false;
                        }

                        continue;
                    }

                    if (c == '"')
                    {
                        insideString = true;

                        result.Append(
                            (char)ReadCharacter()
                        );

                        continue;
                    }

                    if (c == '{' ||
                        c == '[')
                    {
                        depth++;

                        result.Append(
                            (char)ReadCharacter()
                        );

                        continue;
                    }

                    if (c == '}' ||
                        c == ']')
                    {
                        depth--;

                        result.Append(
                            (char)ReadCharacter()
                        );

                        if (depth == 0)
                            break;

                        continue;
                    }

                    // Primitive values such as null, true, false or numbers
                    // have no opening bracket. Read until a JSON separator.
                    if (depth == 0 &&
                        (c == ',' ||
                         c == '}'))
                    {
                        break;
                    }

                    result.Append(
                        (char)ReadCharacter()
                    );
                }

                return result.ToString()
                    .Trim();
            }

            private string ReadRawString()
            {
                StringBuilder result =
                    new StringBuilder();

                bool escaped = false;

                while (true)
                {
                    int c =
                        ReadCharacter();

                    if (c == -1)
                    {
                        throw new FormatException(
                            "Unexpected end of JSON string."
                        );
                    }

                    result.Append(
                        (char)c
                    );

                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (c == '"')
                        break;
                }

                return result.ToString();
            }

            private static int HexValue(int c)
            {
                if (c >= '0' && c <= '9')
                    return c - '0';

                if (c >= 'a' && c <= 'f')
                    return c - 'a' + 10;

                if (c >= 'A' && c <= 'F')
                    return c - 'A' + 10;

                return -1;
            }
        }
    }
}