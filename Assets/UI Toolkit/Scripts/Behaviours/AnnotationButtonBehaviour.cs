using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "AnnotationButtonBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/AnnotationButtonBehaviour", order = 1)]
    public class AnnotationButtonBehaviour : FloatingButtonBehaviour
    {
        
        
        private ToolService toolService;
        
        [SerializeField] private PointerStyle.Style styleOnHover = PointerStyle.Style.GRABBING;

        public override void Initialize(VisualElement parent)
        {
            base.Initialize(parent);
          
            
         
            
        }



        public override VisualElement SpawnFloatingButtonContent()
        {
            return null;
        }

        public override void UpdateBehaviour()
        {
          
        }
        
        public override void Dispose()
        {
            base.Dispose();
            PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
          
        }
    }
}