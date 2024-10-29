using Netherlands3D.Coordinates;
using Netherlands3D.Minimap;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class MinimapBoundingBoxController : MonoBehaviour
    {
        private UIBoundingBox bbox;
        private WMTSMap wmtsMap;
        private RectTransform minimapTransform;
        private RectTransform rTransform;
        private FreeCamera freeCam;
        private GameObject test;
        public Bounds BoundingBox;

        public float lineWidth;
        public Color lineColor;
        public float boxSize = 20000;

        //lon lat amstel 1
        private Coordinate coord = new Coordinate(CoordinateSystem.WGS84, 52.36748063234993, 4.901222522254939, 0);

        private void Start()
        {
            freeCam = FindObjectOfType<FreeCamera>();   
            MinimapUI map = FindObjectOfType<MinimapUI>();
            wmtsMap = map.GetComponentInChildren<WMTSMap>();
            minimapTransform = map.GetComponent<RectTransform>();
            rTransform = GetComponent<RectTransform>();
            transform.SetParent(wmtsMap.transform, false);

            GameObject bboxObject = new GameObject("boundingbox");
            bboxObject.transform.SetParent(map.transform, false);   
            bbox = bboxObject.AddComponent<UIBoundingBox>();
            BoundingBox = new Bounds();
            //test = GameObject.CreatePrimitive(PrimitiveType.Cube);           
        }

        private Vector3 GetLocalMapPositionForWorldPosition(Vector3 unityPosition)
        {
            Vector3RD rdPos = CoordinateConverter.UnitytoRD(unityPosition);
            Vector3 mapPos = wmtsMap.DeterminePositionOnMap(rdPos);
            rTransform.localPosition = mapPos;
            Vector3 worldPosition = rTransform.position;
            Vector3 localPositionInGrandparent = minimapTransform.InverseTransformPoint(worldPosition);
            return localPositionInGrandparent - (Vector3)minimapTransform.sizeDelta * 0.5f;
        }

        private void Update()
        {
            Vector3 target = coord.ToUnity();
            target.y = 0;

            wmtsMap.PositionObjectOnMap(rTransform, CoordinateConverter.UnitytoRD(target));
            BoundingBox.center = target;
            BoundingBox.size = new Vector3(boxSize, Mathf.Max(boxSize, 100000000), boxSize);
            //test.transform.position = BoundingBox.center;
            //test.transform.transform.localScale = Vector3.one * boxSize;

            UpdateMap();


            if (!BoundingBox.Contains(freeCam.transform.position))
                freeCam.transform.position = BoundingBox.ClosestPoint(freeCam.transform.position);
        }

        private void UpdateMap()
        {
            RectTransform rt = wmtsMap.GetComponent<RectTransform>();
            float width = BoundingBox.size.x;
            float depth = BoundingBox.size.z;
            bbox.points = new Vector2[]
            {
                GetLocalMapPositionForWorldPosition(BoundingBox.center + new Vector3(-0.5f * width, 0, 0.5f * depth)),
                GetLocalMapPositionForWorldPosition(BoundingBox.center + new Vector3(0.5f * width, 0, 0.5f * depth)),
                GetLocalMapPositionForWorldPosition(BoundingBox.center + new Vector3(0.5f * width, 0, -0.5f * depth)),
                GetLocalMapPositionForWorldPosition(BoundingBox.center + new Vector3(-0.5f * width, 0, -0.5f * depth)),
                GetLocalMapPositionForWorldPosition(BoundingBox.center + new Vector3(-0.5f * width, 0, 0.5f * depth))
            };
            bbox.lineWidth = lineWidth;
            bbox.color = lineColor;
            bbox.SetVerticesDirty();
        }
    }
}
