using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(LayerGameObject))]
    public class Windmill : MonoBehaviour, IVisualizationWithPropertyData
    {
        public float RotorDiameter
        {
            get => windmillPropertyData.RotorDiameter;
            set => windmillPropertyData.RotorDiameter = value;
        }
        private WindmillPropertyData windmillPropertyData;
        private TransformLayerPropertyData transformPropertyData;

        public float AxisHeight
        {
            get => windmillPropertyData.AxisHeight;
            set => windmillPropertyData.AxisHeight = value;
        }

        [Header("Settings")] [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float defaultHeight = 120f;
        [SerializeField] private float defaultDiameter = 120f;

        [Header("Models")] [SerializeField] private Transform windmillBase;
        [SerializeField] private Transform windmillAxis;
        [SerializeField] private Transform windmillRotor;
        [SerializeField] private Transform[] windmillBlades;

        [SerializeField] private float baseModelHeight = 1.679f;
        [SerializeField] private float baseModelDiameter = 0.79405f;
        [SerializeField] private float rotorModelLength = 13.2f;
        [SerializeField] private float basePercentage = 0.1f;

        private MeshRenderer baseRenderer;

        private void Awake()
        {
            baseRenderer = windmillBase.GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            UpdateAxisHeight(windmillPropertyData.AxisHeight);
            UpdateRotorDiameter(windmillPropertyData.RotorDiameter);
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var lgo = GetComponent<LayerGameObject>();
            lgo.InitProperty<WindmillPropertyData>(properties, null, defaultHeight, defaultDiameter);
            windmillPropertyData = lgo.LayerData.GetProperty<WindmillPropertyData>();

            //in case we cannot transform the object, we also should not be able to edit the Windmill property data, so we need to match the IsEditable state of the WindmillPropertyData to the TransformPropertyData
            transformPropertyData = lgo.LayerData.GetProperty<TransformLayerPropertyData>();
            windmillPropertyData.IsEditable = transformPropertyData.IsEditable;
            
            AddListeners();
        }

        private void MatchEditableState(bool transformIsEditable)
        {
            windmillPropertyData.IsEditable = transformIsEditable;
        }

        private void AddListeners()
        {
            windmillPropertyData.OnAxisHeightChanged.AddListener(UpdateAxisHeight);
            windmillPropertyData.OnRotorDiameterChanged.AddListener(UpdateRotorDiameter);
            transformPropertyData.IsEditableChanged.AddListener(MatchEditableState);
            
        }

        private void RemoveListeners()
        {
            windmillPropertyData.OnAxisHeightChanged.RemoveListener(UpdateAxisHeight);
            windmillPropertyData.OnRotorDiameterChanged.RemoveListener(UpdateRotorDiameter);
            transformPropertyData.IsEditableChanged.RemoveListener(MatchEditableState);
            
        }

        private void UpdateAxisHeight(float height)
        {
            var baseHeight = height / baseModelHeight;

            var baseScale = baseHeight * basePercentage;
            baseScale /= baseModelDiameter;

            windmillBase.localScale = new Vector3(baseScale, baseHeight, baseScale);
            windmillAxis.localScale = new Vector3(baseScale, baseScale, baseScale);

            var axisPosition = baseRenderer.bounds.size.y;
            windmillAxis.localPosition = new Vector3(0, axisPosition, 0);
        }

        private void UpdateRotorDiameter(float diameter)
        {
            var rotorsLength = diameter * 0.5f;
            
            //Scale the windmillRotors
            foreach (var windmillBlade in windmillBlades)
            {
                var rotorScale = rotorsLength / windmillAxis.localScale.x;
                rotorScale /= rotorModelLength;

                windmillBlade.localScale = new Vector3(rotorScale, rotorScale, rotorScale);
            }
        }

        private void Update()
        {
            windmillRotor.Rotate(Vector3.forward, Time.deltaTime * rotationSpeed, Space.Self);
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
    }
}