//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
//using Netherlands3D.Coordinates;
//using Netherlands3D.SelectionTools;
//using Netherlands3D.Twin.FloatingOrigin;
//using Netherlands3D.Twin.Layers.ExtensionMethods;
//using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
//using Netherlands3D.Twin.Layers.Properties;
//using Newtonsoft.Json;
//using UnityEngine;
//using UnityEngine.Events;

//namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
//{
//    //public enum ShapeType
//    //{
//    //    Undefined = 0,
//    //    Polygon = 1,
//    //    Line = 2,
//    //    Grid = 3
//    //}

//    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers", Name = "PolygonSelection")]
//    public class PolygonSelectionLayer : LayerData
//    {
//        //[DataMember] public List<Coordinate> OriginalPolygon { get; private set; }
//        //[DataMember] private ShapeType shapeType;

//        //[JsonIgnore] private PolygonSelectionLayerPropertyData PolygonPropertyData => GetProperty<PolygonSelectionLayerPropertyData>();
//        //[JsonIgnore] public CompoundPolygon Polygon { get; set; }
//        //[JsonIgnore] public UnityEvent<PolygonSelectionLayer> polygonSelected = new();
//        //[JsonIgnore] public UnityEvent polygonMoved = new();
//        //[JsonIgnore] public UnityEvent polygonChanged = new();

//        //[JsonIgnore]
//        //public ShapeType ShapeType
//        //{
//        //    get => shapeType;
//        //    set
//        //    {
//        //        shapeType = value;
//        //        SetShape(OriginalPolygon);
//        //    }
//        //}

//        //[JsonIgnore]
//        //public float LineWidth
//        //{
//        //    get => PolygonPropertyData.LineWidth;
//        //    set
//        //    {
//        //        PolygonPropertyData.LineWidth = value;
//        //        SetShape(OriginalPolygon);
//        //    }
//        //}

//        //[JsonIgnore]
//        //public bool IsMask
//        //{
//        //    get => PolygonPropertyData.IsMask;
//        //    set => PolygonPropertyData.IsMask = value;
//        //}

//        //[JsonIgnore]
//        //public bool InvertMask
//        //{
//        //    get => PolygonPropertyData.InvertMask;
//        //    set => PolygonPropertyData.InvertMask = value;
//        //}

//        //[JsonIgnore]
//        //public int MaskBitIndex
//        //{
//        //    get => PolygonPropertyData.MaskBitIndex;
//        //    set => PolygonPropertyData.MaskBitIndex = value;
//        //}
//        //private static List<int> availableMaskChannels = new List<int>() { 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 };
//        //public static int NumAvailableMasks => availableMaskChannels.Count;
//        //public static int MaxAvailableMasks => 22;
//        //public static UnityEvent<int> MaskDestroyed = new();

//        [JsonIgnore] public PolygonSelectionVisualisation PolygonVisualisation => Visualization as PolygonSelectionVisualisation;

//        [JsonConstructor]
//        public PolygonSelectionLayer(
//            string name, 
//            string prefabId, 
//            List<LayerPropertyData> layerProperties, 
//            List<Coordinate> originalPolygon, 
//            ShapeType shapeType
//        ) : base(name, prefabId) {
//            this.shapeType = shapeType;            
//            //SetShape(originalPolygon); 
//            ////PolygonSelectionCalculator.RegisterPolygon(this);
//            //UpdatePolygonVisualisation(OriginalPolygon);
//            ////RegisterListeners();
//            //availableMaskChannels.Remove(MaskBitIndex);
//        }

//        public PolygonSelectionLayer(
//            string name,
//            string prefabId,
//            List<Vector3> polygonUnityInput,
//            ShapeType shapeType,
//            float defaultLineWidth = 10f, 
//            Action<LayerData> onSpawn = null
//        ) : base(
//            name, 
//            prefabId
//        ) {
//            onSpawn?.Invoke(this);
//            this.shapeType = shapeType;
//            this.layerProperties = new List<LayerPropertyData>
//            {
//                new PolygonSelectionLayerPropertyData() { LineWidth = defaultLineWidth }
//            };
//            SetShape(polygonUnityInput.ToCoordinates().ToList());
//            //PolygonSelectionCalculator.RegisterPolygon(this);
//            UpdatePolygonVisualisation(OriginalPolygon);
//            //RegisterListeners();

//            //availableMaskChannels.Remove(MaskBitIndex);
//        }

//        //private void RegisterListeners()
//        //{
//        //    //Add shifter that manipulates the polygon if the world origin is shifted
//        //    Origin.current.onPostShift.AddListener(ShiftedPolygon);

//        //    LayerActiveInHierarchyChanged.AddListener(OnLayerActiveInHierarchyChanged);
//        //    PolygonPropertyData.OnIsMaskChanged.AddListener(OnIsMaskChanged);
//        //    PolygonPropertyData.OnInvertMaskChanged.AddListener(OnInvertMaskChanged);
//        //    OnPrefabIdChanged.AddListener(OnSwitchVisualisation);
//        //}

//        //private void UnregisterListeners()
//        //{
//        //    LayerActiveInHierarchyChanged.RemoveListener(OnLayerActiveInHierarchyChanged);
//        //    Origin.current.onPostShift.RemoveListener(ShiftedPolygon);
//        //    PolygonPropertyData.OnIsMaskChanged.RemoveListener(OnIsMaskChanged);
//        //    PolygonPropertyData.OnInvertMaskChanged.RemoveListener(OnInvertMaskChanged);
//        //    OnPrefabIdChanged.RemoveListener(OnSwitchVisualisation);
//        //}

//        //private void OnSwitchVisualisation()
//        //{
//        //    OnIsMaskChanged(IsMask); //The reference changed, so we need to treat it as if we make a new mask
//        //}

//        //private void CleanupMasking()
//        //{
//        //    // first clear shader properties with the existing mask bit index
//        //    UpdateInvertedMaskBitInShaders(false, false, false);
//        //    //now that the shader properties are cleared, we can free up the mask bit
//        //    SetMaskBitIndex(false, false);
//        //}

//        //private void ShiftedPolygon(Coordinate fromOrigin, Coordinate toOrigin)
//        //{
//        //    //Silent update of the polygon shape, so the visualisation is updated without notifying the listeners
//        //    RecalculatePolygon();
//        //    UpdatePolygonVisualisation(OriginalPolygon);
//        //    polygonMoved.Invoke();
//        //}

//        ///// <summary>
//        ///// Sets the contour causing update of Line or Polygon, based on chosen ShapeType
//        ///// </summary>
//        //public void SetShape(List<Coordinate> coordinates)
//        //{
//        //    OriginalPolygon = coordinates;
//        //    RecalculatePolygon();
//        //    UpdatePolygonVisualisation(coordinates);
//        //    polygonChanged.Invoke();
//        //}

//        //private void UpdatePolygonVisualisation(List<Coordinate> polygon)
//        //{
//        //    if (PolygonVisualisation)
//        //    {
//        //        var vertices = CoordinatesToVertices(polygon);
//        //        PolygonVisualisation.UpdateVisualisation(vertices, PolygonPropertyData.ExtrusionHeight);
//        //        PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
//        //    }
//        //}

//        //private void RecalculatePolygon()
//        //{
//        //    var vertices = CoordinatesToVertices(OriginalPolygon);
//        //    Polygon = new CompoundPolygon(vertices);
//        //}

//        //private Vector2[] CoordinatesToVertices(List<Coordinate> coordinates)
//        //{
//        //    var positions = coordinates.ToUnityPositions().ToList();
//        //    var vertices = PolygonCalculator.FlattenPolygon(positions, new Plane(Vector3.up, 0));
//        //    if (vertices.Length == 2)
//        //    {
//        //        vertices = LineToPolygon(vertices, PolygonPropertyData.LineWidth);
//        //    }

//        //    return vertices;
//        //}

//        //private static Vector2[] LineToPolygon(Vector2[] vertices, float width)
//        //{
//        //    if (vertices.Length != 2)
//        //    {
//        //        Debug.LogError("cannot create rectangle because position list contains more than 2 entries");
//        //        return null;
//        //    }

//        //    var dir = vertices[1] - vertices[0];
//        //    var normal = new Vector2(-dir.y, dir.x).normalized;

//        //    var dist = normal * width / 2;

//        //    var point1 = vertices[0] + new Vector2(dist.x, dist.y);
//        //    var point4 = vertices[1] + new Vector2(dist.x, dist.y);
//        //    var point3 = vertices[1] - new Vector2(dist.x, dist.y);
//        //    var point2 = vertices[0] - new Vector2(dist.x, dist.y);

//        //    var polygon = new Vector2[]
//        //    {
//        //        point1,
//        //        point2,
//        //        point3,
//        //        point4
//        //    };

//        //    return polygon;
//        //}

//        //private void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
//        //{
//        //    SetVisualisationActive(activeInHierarchy);
//        //    UpdateInvertedMaskBitInShaders(IsMask, InvertMask, activeInHierarchy);
//        //}

//        //private void UpdateInvertedMaskBitInShaders(bool isMask, bool invertMask, bool activeInHierarchy)
//        //{
//        //    var active = isMask && invertMask && activeInHierarchy;
//        //    PolygonProjectionMask.UpdateInvertedMaskBit(MaskBitIndex, active);
//        //    PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
//        //}

//        //public void SetVisualisationActive(bool active)
//        //{
//        //    PolygonVisualisation.gameObject.SetActive(active);
//        //}

//        //public override void SelectLayer(bool deselectOthers = false)
//        //{
//        //    base.SelectLayer(deselectOthers);
//        //    polygonSelected.Invoke(this);
//        //}

//        //public override void DeselectLayer()
//        //{
//        //    base.DeselectLayer();
//        //    polygonSelected.Invoke(null);
//        //}

//        //public override void DestroyLayer()
//        //{
//        //    PolygonSelectionCalculator.UnregisterPolygon(this);
//        //    CleanupMasking();
//        //    base.DestroyLayer();
//        //    UnregisterListeners();
//        //}

//        //private void OnIsMaskChanged(bool isMask)
//        //{
//        //    if (isMask && MaskBitIndex == -1 && availableMaskChannels.Count == 0)
//        //    {
//        //        Debug.LogError("No more masking channels available");
//        //        IsMask = false;
//        //        return;
//        //    }

//        //    SetMaskBitIndex(isMask, InvertMask);
//        //}

//        //private void SetMaskBitIndex(bool isMask, bool invertMask)
//        //{
//        //    if (!isMask && MaskBitIndex != -1)
//        //    {
//        //        OnInvertMaskChanged(invertMask); //clear the inverted mask property before clearing the bit index
//        //        availableMaskChannels.Add(MaskBitIndex);
//        //        MaskBitIndex = -1;
//        //    }
//        //    else if (isMask && MaskBitIndex == -1)
//        //    {
//        //        MaskBitIndex = availableMaskChannels.Last();
//        //        availableMaskChannels.Remove(MaskBitIndex);
//        //        OnInvertMaskChanged(invertMask); //set the inverted mask property after assigning the bit index
//        //    }
//        //}

//        //private void OnInvertMaskChanged(bool invert)
//        //{
//        //    var layer = GetLayer(IsMask);
//        //    SetPolygonLayer(layer);
//        //    UpdateInvertedMaskBitInShaders(IsMask, invert, ActiveInHierarchy);
//        //    PolygonVisualisation.SetMaterial(IsMask, MaskBitIndex, invert);
//        //}

//        //private void SetPolygonLayer(LayerMask layer)
//        //{
//        //    foreach (Transform t in PolygonVisualisation.gameObject.transform)
//        //    {
//        //        t.gameObject.gameObject.layer = layer;
//        //    }

//        //    PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
//        //}

//        //private LayerMask GetLayer(bool isMask)
//        //{
//        //    var layer = LayerMask.NameToLayer("Projected");
//        //    if (!isMask) return layer;

//        //    return LayerMask.NameToLayer("PolygonMask");
//        //}
//    }
//}