using System.Collections.Generic;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Cameras;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class ViewportEdge : MonoBehaviour
    {
        [SerializeField] private Texture2D cornerSprite;
        [SerializeField] private Texture2D sideSprite;
        private float baseEdgeWidth = 160;
        private float edgeWidth;

        private struct EdgeConfig
        {
            public VisualElement Element;
            public bool HasWidth, HasHeight;
            public bool MarginTopBottom, MarginLeftRight;
        }

        private List<EdgeConfig> panels = new List<EdgeConfig>();
        private FreeCamera cam;

        void Start()
        {
            edgeWidth = baseEdgeWidth;
            cam = FindAnyObjectByType<FreeCamera>();
            var anchor = App.UIRoot.Root.parent;

            CreateEdge(anchor, sideSprite,   SpriteRotation.Deg0,   left: 0,  top: edgeWidth, bottom: edgeWidth, width: edgeWidth);
            CreateEdge(anchor, sideSprite,   SpriteRotation.Deg180, right: 0, top: edgeWidth, bottom: edgeWidth, width: edgeWidth);
            CreateEdge(anchor, sideSprite,   SpriteRotation.Deg270, left: edgeWidth, top: 0, right: edgeWidth,   height: edgeWidth);
            CreateEdge(anchor, sideSprite,   SpriteRotation.Deg90,  left: edgeWidth, bottom: 0, right: edgeWidth, height: edgeWidth);

            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg270, left: 0,  top: 0,    width: edgeWidth, height: edgeWidth);
            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg180, right: 0, top: 0,    width: edgeWidth, height: edgeWidth);
            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg0,   left: 0,  bottom: 0, width: edgeWidth, height: edgeWidth);
            CreateEdge(anchor, cornerSprite, SpriteRotation.Deg90,  right: 0, bottom: 0, width: edgeWidth, height: edgeWidth);
        }

        void CreateEdge(
            VisualElement anchor,
            Texture2D sprite,
            SpriteRotation rotation,
            float? left = null, float? right = null, float? top = null, float? bottom = null,
            float? width = null, float? height = null)
        {
            var e = new VisualElement();
            e.pickingMode = PickingMode.Ignore;
            e.style.position = Position.Absolute;
            e.AddToClassList("vignette-edge");
            e.style.backgroundImage = new StyleBackground(Rotate(sprite, rotation));

            if (left.HasValue)   e.style.left   = left.Value;
            if (right.HasValue)  e.style.right  = right.Value;
            if (top.HasValue)    e.style.top    = top.Value;
            if (bottom.HasValue) e.style.bottom = bottom.Value;
            if (width.HasValue)  e.style.width  = width.Value;
            if (height.HasValue) e.style.height = height.Value;

            panels.Add(new EdgeConfig
            {
                Element = e,
                HasWidth = width.HasValue,
                HasHeight = height.HasValue,
                MarginTopBottom = top.HasValue && bottom.HasValue && !height.HasValue,
                MarginLeftRight = left.HasValue && right.HasValue && !width.HasValue
            });

            anchor.Insert(0, e);
        }

        private float tempEdgeWidth;
        private void Update()
        {
            tempEdgeWidth = Mathf.Clamp(baseEdgeWidth - Mathf.Abs(cam.DynamicZoomSpeed), 0, baseEdgeWidth); 
            edgeWidth = Mathf.Lerp(edgeWidth, tempEdgeWidth, Time.deltaTime * baseEdgeWidth / tempEdgeWidth);
            
            
            foreach (var config in panels)
            {
                if (config.HasWidth)        config.Element.style.width  = edgeWidth;
                if (config.HasHeight)       config.Element.style.height = edgeWidth;
                if (config.MarginTopBottom) { config.Element.style.top = edgeWidth; config.Element.style.bottom = edgeWidth; }
                if (config.MarginLeftRight) { config.Element.style.left = edgeWidth; config.Element.style.right = edgeWidth; }
            }
        }

        public enum SpriteRotation
        {
            Deg0,
            Deg90,
            Deg180,
            Deg270
        }

        public static Texture2D Rotate(Texture2D source, SpriteRotation rotation)
        {
            switch (rotation)
            {
                case SpriteRotation.Deg90:  return Rotate90(source);
                case SpriteRotation.Deg180: return Rotate90(Rotate90(source));
                case SpriteRotation.Deg270: return Rotate90(Rotate90(Rotate90(source)));
            }
            return source;
        }

        private static Texture2D Rotate90(Texture2D source)
        {
            int w = source.width;
            int h = source.height;
            Texture2D result = new Texture2D(h, w, source.format, false);
            Color32[] src = source.GetPixels32();
            Color32[] dst = new Color32[src.Length];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int srcIndex = x + y * w;
                    int newX = h - 1 - y;
                    int newY = x;
                    int dstIndex = newX + newY * h;
                    dst[dstIndex] = src[srcIndex];
                }
            }
            result.SetPixels32(dst);
            result.Apply();
            return result;
        }
    }
}