using System;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D
{
    public class ViewportEdge : MonoBehaviour
    {

        private const string stylingClassName = "vignette-edge";
        [SerializeField] private Texture2D cornerSprite;
        [SerializeField] private Texture2D sideSprite;
        private const float edgeWidth = 120;

        void Start()
        {
            var anchor = App.UIRoot.Root.parent;

            CreateEdge(anchor, sideSprite, SpriteRotation.Deg0,   left: 0,         top: edgeWidth,  bottom: edgeWidth, width: edgeWidth);
            CreateEdge(anchor, sideSprite, SpriteRotation.Deg180, right: 0,        top: edgeWidth,  bottom: edgeWidth, width: edgeWidth);
            CreateEdge(anchor, sideSprite, SpriteRotation.Deg270, left: edgeWidth, right: edgeWidth, top: 0,           height: edgeWidth);
            CreateEdge(anchor, sideSprite, SpriteRotation.Deg90,  left: edgeWidth, right: edgeWidth, bottom: 0,        height: edgeWidth);

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
            e.AddToClassList(stylingClassName);
            e.style.backgroundImage = new StyleBackground(Rotate(sprite, rotation));

            if (left.HasValue)   e.style.left   = left.Value;
            if (right.HasValue)  e.style.right  = right.Value;
            if (top.HasValue)    e.style.top    = top.Value;
            if (bottom.HasValue) e.style.bottom = bottom.Value;
            if (width.HasValue)  e.style.width  = width.Value;
            if (height.HasValue) e.style.height = height.Value;

            anchor.Insert(0, e);
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
                case SpriteRotation.Deg90:
                    return Rotate90(source);
                case SpriteRotation.Deg180:
                    return Rotate90(Rotate90(source));
                case SpriteRotation.Deg270:
                    return Rotate90(Rotate90(Rotate90(source)));
            }
            return source;
        }

        private static Texture2D Rotate90(Texture2D source)
        {
            int w = source.width;
            int h = source.height;
            Texture2D result = new Texture2D(h, w, source.format, false);
    
            var src = source.GetRawTextureData<Color32>();
            var dst = result.GetRawTextureData<Color32>();
    
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
    
            result.Apply();
            return result;
        }
    }
}
