using System.Collections.Generic;
using System.Runtime.CompilerServices;
using netDxf.Entities;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "WorldTextBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/WorldTextBehaviour", order = 1)]
    public class AnnotationBehaviour : FloatingButtonBehaviour
    {
        private List<AnnotationTextObject> worldTextObjects = new();

        public override void Initialize(VisualElement parent)
        {
            //base.Initialize(parent);
            this.content = parent;
            
            //test
            //this.content.Add(SpawnFloatingButtonContent());
        }

        public override VisualElement SpawnFloatingButtonContent()
        {
            return new FloatingElement();
        }
//new Coordinate(CoordinateSystem.RDNAP, 139607, 478158, 0);
        public AnnotationTextObject AddWorldTextObject(string text, Coordinate coord, WorldText.SnappingSide side, float offsetFromPoint)
        {
            FloatingElement floatingElement = new FloatingElement();
            content.Add(floatingElement);
            
            WorldText worldText = new WorldText();
            worldText.SetText(text);
            worldText.SetSnappingSide(side);
            worldText.SetLabelOffset(offsetFromPoint);
            floatingElement.Add(worldText);
            
            AnnotationTextObject annotationTextObject = new AnnotationTextObject();
            annotationTextObject.floatingElement = floatingElement;
            annotationTextObject.element = worldText;
            annotationTextObject.coordinate = coord;
            
            worldTextObjects.Add(annotationTextObject);
            return annotationTextObject;
        }

        public void RemoveWorldTextObject(AnnotationTextObject annotationTextObject)
        {
            content.Remove(annotationTextObject.floatingElement);
            worldTextObjects.Remove(annotationTextObject);
        }

        private GameObject testObject = null;

        public override void UpdateBehaviour()
        {
            foreach (AnnotationTextObject worldTextObject in worldTextObjects)
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
    
    public class AnnotationTextObject
    {
        public Coordinate coordinate;
        public WorldText element;
        public FloatingElement floatingElement;
        public bool enabled;
        public Color color;
    }
}