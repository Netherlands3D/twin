using UnityEngine;

namespace Netherlands3D.ObjectLibrary
{
    public class Windmill : MonoBehaviour
    {
        public float RotorDiameter
        {
            get => rotorDiameter;
            set
            {
                rotorDiameter = value;
                Resize();
            }
        }

        public float AxisHeight
        {
            get => axisHeight;
            set
            {
                axisHeight = value;
                Resize();
            }
        }

        [Header("Settings")]
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float axisHeight = 120f;
        [SerializeField] private float rotorDiameter = 120f;
        [SerializeField] private float defaultHeight = 120f;
        [SerializeField] private float defaultDiameter = 120f;

        [Header("Models")]
        [SerializeField] private Transform windmillBase;
        [SerializeField] private Transform windmillAxis;
        [SerializeField] private Transform windmillRotor;
        [SerializeField] private Transform[] windmillBlades;

        [SerializeField] private float baseModelHeight = 1.679f;
        [SerializeField] private float baseModelDiameter = 0.79405f; 
        [SerializeField] private float rotorModelLength = 13.2f;
        [SerializeField] private float basePercentage = 0.1f;

        private MeshRenderer baseRenderer;

        private void Start()
        {
            baseRenderer = windmillBase.GetComponent<MeshRenderer>();

            // Do initial resize, in case properties were set in inspector
            Resize();
        }

        private void Resize()
        {
            if (axisHeight == 0)
            {
                Debug.LogWarning($"Windmill {name} has no height, using fallback height");
                axisHeight = defaultHeight;
            }

            if (rotorDiameter == 0)
            {
                Debug.LogWarning($"Windmill {name} has no diameter, using fallback diameter");
                rotorDiameter = defaultDiameter;
            }

            var rotorsLength = rotorDiameter * 0.5f;
            var baseHeight = axisHeight / baseModelHeight;

            var baseScale = baseHeight * basePercentage;
            baseScale /= baseModelDiameter;

            windmillBase.localScale = new Vector3(baseScale, baseHeight, baseScale);
            windmillAxis.localScale = new Vector3(baseScale, baseScale, baseScale);

            var axisPosition = baseRenderer.bounds.size.y;
            windmillAxis.localPosition = new Vector3(0, axisPosition, 0);
            
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
    }
}
