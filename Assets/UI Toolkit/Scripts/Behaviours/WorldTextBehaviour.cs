using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "WorldTextBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/WorldTextBehaviour", order = 1)]
    public class WorldTextBehaviour : FloatingButtonBehaviour
    {
    

        public override void Initialize(VisualElement parent)
        {
            base.Initialize(parent);
        }
    

        public override VisualElement SpawnFloatingButtonContent()
        {
            WorldText text = new WorldText();
            return text;
        }

    

        public override void UpdateBehaviour()
        {
         
        }
        
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}