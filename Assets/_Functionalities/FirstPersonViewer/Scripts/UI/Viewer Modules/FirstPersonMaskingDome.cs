using Netherlands3D.Events;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Components
{
    public class FirstPersonMaskingDome : MonoBehaviour
    {
        [Header("Placement actions")]
        [SerializeField] private float maxCameraTravelToPlacement = 20.0f;
        [SerializeField] private float margin;

        [Header("Global shader settings")]
        [SerializeField] private string sphericalMaskFeatureKeyword = "_SPHERICAL_MASKING";
        [SerializeField] private string sphericalMaskPositionName = "_SphericalMaskPosition";
        [SerializeField] private string sphericalMaskRadiusName = "_SphericalMaskRadius";
        [SerializeField] private bool resetMaskOnDisable = true;
        private int positionPropertyID;
        private int radiusPropertyID;

        [Header("Event")]
        [SerializeField] private BoolEvent enableDome;

        private void Start()
        {
            gameObject.SetActive(false);
            enableDome.AddListenerStarted(EnableObject);

            GetPropertyIDs();
            ApplyGlobalShaderVariables();
        }

        private void OnEnable()
        {
            Shader.EnableKeyword(sphericalMaskFeatureKeyword);
        }

        private void OnDisable()
        {
            Shader.DisableKeyword(sphericalMaskFeatureKeyword);

            if (resetMaskOnDisable)
            {
                ResetGlobalShaderVariables();
            }
        }

        private void OnDestroy()
        {
            enableDome.RemoveListenerStarted(EnableObject);
        }

        void Update()
        {
            if (transform.hasChanged)
            {
                ApplyGlobalShaderVariables();
                transform.hasChanged = false;
            }
        }

        private void EnableObject(bool enable)
        {
            gameObject.SetActive(enable);
            ApplyGlobalShaderVariables();
        }

        private void GetPropertyIDs()
        {
            positionPropertyID = Shader.PropertyToID(sphericalMaskPositionName);
            radiusPropertyID = Shader.PropertyToID(sphericalMaskRadiusName);
        }

        private void ApplyGlobalShaderVariables()
        {
            Shader.SetGlobalVector(positionPropertyID, transform.position);
            Shader.SetGlobalFloat(radiusPropertyID, (transform.localScale.x / 2.0f) + margin);
        }

        private void ResetGlobalShaderVariables()
        {
            Shader.SetGlobalVector(positionPropertyID, Vector3.zero);
            Shader.SetGlobalFloat(radiusPropertyID, 0.0f);
        }

    }
}
