using Netherlands3D.Services;
using Netherlands3D.Twin.UI;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class ObjectPanel : VisualElement
    {
        public UnityEvent OnClose = new();

        private Toggle position;
        private Toggle rotation;
        private Toggle scale;
        
        private GameObject target;
        private TransformHandleInterfaceToggle  transformInterfaceToggle;

        public ObjectPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            position = this.Q<Toggle>("Position");
            rotation = this.Q<Toggle>("Rotation");
            scale = this.Q<Toggle>("Scale");
            position.RegisterValueChangedCallback(OnTogglePosition);
            rotation.RegisterValueChangedCallback(OnToggleRotation);
            scale.RegisterValueChangedCallback(OnToggleScale);
        }
        
        public ObjectPanel(GameObject target) :  this()
        {
            this.target = target;
            transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            transformInterfaceToggle.OnUpdateGizmoHandles.AddListener(OnUpdate);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            OnUpdate();
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            transformInterfaceToggle.OnUpdateGizmoHandles.RemoveListener(OnUpdate);
        }

        private void OnTogglePosition(ChangeEvent<bool> evt)
        {
            transformInterfaceToggle.CurrentMode = TransformHandleInterfaceToggle.TransformMode.Position;
        }
        
        private void OnToggleRotation(ChangeEvent<bool> evt)
        {
            transformInterfaceToggle.CurrentMode = TransformHandleInterfaceToggle.TransformMode.Rotation;
        }
        
        private void OnToggleScale(ChangeEvent<bool> evt)
        {
            transformInterfaceToggle.CurrentMode = TransformHandleInterfaceToggle.TransformMode.Scale;
        }

        private void OnUpdate()
        {
            position.SetEnabled(transformInterfaceToggle.PositionInteractable);
            rotation.SetEnabled(transformInterfaceToggle.RotationInteractable);
            scale.SetEnabled(transformInterfaceToggle.ScaleInteractable);
            position.SetValueWithoutNotify(transformInterfaceToggle.CurrentMode == TransformHandleInterfaceToggle.TransformMode.Position);
            rotation.SetValueWithoutNotify(transformInterfaceToggle.CurrentMode == TransformHandleInterfaceToggle.TransformMode.Rotation);
            scale.SetValueWithoutNotify(transformInterfaceToggle.CurrentMode == TransformHandleInterfaceToggle.TransformMode.Scale);
        }
    }
}