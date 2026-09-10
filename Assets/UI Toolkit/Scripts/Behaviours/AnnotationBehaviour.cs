using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "WorldTextBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/WorldTextBehaviour", order = 1)]
    public class AnnotationBehaviour : FloatingButtonBehaviour
    {
        //new Coordinate(CoordinateSystem.RDNAP, 139607, 478158, 0); naarden start coordinate
        
        private List<AnnotationTextObject> worldTextObjects = new();
        private InputService inputService;

        [SerializeField] private Material DebugMaterial;
        [SerializeField] private bool debug = false;

        public override void Initialize(VisualElement parent)
        {
            //base.Initialize(parent);
            this.content = parent;
            inputService = ServiceLocator.GetService<InputService>();
        }

        public override VisualElement SpawnFloatingButtonContent() //todo this maybe shouldnt be based on floatingbuttonbheavour and more generic like floatingelementbehaviour
        {
            return new FloatingElement();
        }

        public AnnotationTextObject AddWorldTextObject(string text, Coordinate coord, WorldText.SnappingSide side, float offsetFromPoint)
        {
            FloatingElement floatingElement = SpawnFloatingButtonContent() as FloatingElement;
            content.Add(floatingElement);
           
            AnnotationTextObject annotationTextObject = new AnnotationTextObject(text, floatingElement, side, offsetFromPoint);
            annotationTextObject.AddTextEditListener(OnEditingChangedEvent);
            annotationTextObject.coordinate = coord;
            
            worldTextObjects.Add(annotationTextObject);
            return annotationTextObject;
        }

        private void OnEditingChangedEvent(bool isEditing)
        {
            inputService.SetCameraActionsEnabled(!isEditing);
        }

        public void RemoveWorldTextObject(AnnotationTextObject annotationTextObject)
        {
            annotationTextObject.RemoveTextEditListener(OnEditingChangedEvent);
            content.Remove(annotationTextObject.floatingElement);
            worldTextObjects.Remove(annotationTextObject);
        }

        private GameObject testObject = null;

        public override void UpdateBehaviour()
        {
            if(debug)
            {
                if (testObject == null)
                {
                    testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(testObject.GetComponent<Collider>());

                    testObject.transform.localScale = new Vector3(10f, 10f, 10f);
                    MeshRenderer meshRenderer = testObject.GetComponent<MeshRenderer>();
                    meshRenderer.material = DebugMaterial;
                }
                if(worldTextObjects.Count > 0)
                    testObject.transform.position = worldTextObjects[0].coordinate.ToUnity();
            }
            
            
            foreach (AnnotationTextObject worldTextObject in worldTextObjects)
            {
                var screenPos =  App.Cameras.ActiveCamera.WorldToScreenPoint(worldTextObject.coordinate.ToUnity());
                Vector2 panelPos = App.UIRoot.GetUIPositionFromScreenPosition(screenPos);
                var contentPos = content.worldBound.position;
                var localPos = panelPos - contentPos;
                worldTextObject.floatingElement.SetPosition(localPos);
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
        public bool Visible => visible; //  !element.ClassListContains(UtilityClassConstants.HIDDEN); 

        private bool visible;

        public AnnotationTextObject(string text, FloatingElement floatingElement, WorldText.SnappingSide side, float offsetPixels)
        {
            this.floatingElement = floatingElement;
            if(floatingElement == null)
                throw new Exception("FloatingElement is missing");
            
            element = new WorldText();
            element.SetText(text);
            element.SetSnappingSide(side);
            element.SetLabelOffset(offsetPixels);
            floatingElement.Add(element);
        }

        public void AddTextEditListener(UnityAction<bool> action)
        {
            element.NameField.OnEditingChanged.AddListener(action);
        }

        public void RemoveTextEditListener(UnityAction<bool> action)
        {
            element.NameField.OnEditingChanged.RemoveListener(action);
        }

        public void SetVisible(bool visible)
        {
            this.visible = visible;
            element.EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }

        public void SetColor(Color color)
        {
            element.SetColor(color);
        }
    }
}