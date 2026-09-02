using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "WorldTextBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/WorldTextBehaviour", order = 1)]
    public class WorldTextBehaviour : FloatingButtonBehaviour
    {
        private List<WorldTextObject> worldTextObjects = new();
        
        
        public struct WorldTextObject
        {
            public Coordinate coordinate;
            public WorldText element;
            public FloatingElement floatingElement;
            public bool enabled;
            public Color color;
        }
        

        public override void Initialize(VisualElement parent)
        {
            //base.Initialize(parent);
            this.content = parent;
            
            //test
            this.content.Add(SpawnFloatingButtonContent());
        }
    

        public override VisualElement SpawnFloatingButtonContent()
        {
            FloatingElement floatingElement = new FloatingElement();
            WorldText text = new WorldText();
            text.SetText("testing much text \n and there is even more text");
            text.SetSnappingSide(WorldText.SnappingSide.Above);
            text.SetLabelOffset(50);
            floatingElement.Add(text);
            
            WorldTextObject worldTextObject = new WorldTextObject();
            worldTextObject.floatingElement = floatingElement;
            worldTextObject.element = text;
            worldTextObject.coordinate = new Coordinate(CoordinateSystem.RDNAP, 139607, 478158, 0);
            
           
            
            worldTextObjects.Add(worldTextObject);
            
            return floatingElement;
        }


        private GameObject testObject = null;

        public override void UpdateBehaviour()
        {
            
            
            foreach (WorldTextObject worldTextObject in worldTextObjects)
            {
                var screenPos =  App.Cameras.ActiveCamera.WorldToScreenPoint(worldTextObject.coordinate.ToUnity());
                Vector2 panelPos = App.UIRoot.GetUIPositionFromScreenPosition(screenPos);
                var contentPos = content.worldBound.position;
                var localPos = panelPos - contentPos;
                worldTextObject.floatingElement.SetPosition(localPos);

                if (testObject == null)
                {
                    testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                   
                    testObject.transform.localScale = new Vector3(10f, 10f, 10f);
                    testObject.GetComponent<MeshRenderer>().material.color = Color.green;
                }
                testObject.transform.position = worldTextObject.coordinate.ToUnity();
                
            }
        }
        
        public override void Dispose()
        {
            base.Dispose();
        }
    }
}