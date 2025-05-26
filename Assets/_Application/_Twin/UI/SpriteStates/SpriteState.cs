using UnityEngine;

namespace Netherlands3D
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/SpriteState", fileName = "SpriteState", order = 0)]
    public class SpriteState : ScriptableObject
    {
        public Sprite defaultSprite;
        public Sprite highlightedSprite;
        public Sprite pressedSprite;
        public Sprite selectedSprite;
        public Sprite disabledSprite;
    }
}
