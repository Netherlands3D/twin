using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Quality
{
    public class WaterReflectionCamera : MonoBehaviour
    {
        [SerializeField] private Material waterMaterial;

        private RenderTexture renderTexture;

        [SerializeField] private float scaleMultiplier = 0.1f;

        private new Camera camera;
        private Camera followCamera;

        private int screenWidthOnInit = 512;
        private int screenHeightOnInit = 512;
        private string waterReflectionsFeatureKeyword = "_REALTIME_PLANAR_REFLECTIONS";

        public float ScaleMultiplier
        {
            get => scaleMultiplier;
            set
            {
                scaleMultiplier = value;
                ScaleOrViewChanged();
            }
        }

        private GlobalKeyword exampleFeatureKeyword;

        private void OnEnable()
        {
            exampleFeatureKeyword = GlobalKeyword.Create(waterReflectionsFeatureKeyword);
            waterMaterial.EnableKeyword(waterReflectionsFeatureKeyword);

            if (!camera)
                camera = GetComponent<Camera>();

            if (!followCamera)
                followCamera = Camera.main;

            if (!renderTexture)
                CreateNewRenderTexture();
        }

        private void OnDisable()
        {
            waterMaterial.DisableKeyword(waterReflectionsFeatureKeyword);

            camera.targetTexture = null;
            Destroy(renderTexture);
        }

        private void CreateNewRenderTexture()
        {
            renderTexture = new RenderTexture(Mathf.RoundToInt(followCamera.pixelWidth * ScaleMultiplier), Mathf.RoundToInt(followCamera.pixelHeight * ScaleMultiplier), 0);

            screenWidthOnInit = followCamera.pixelWidth;
            screenHeightOnInit = followCamera.pixelHeight;
            camera.targetTexture = renderTexture;
            waterMaterial.SetTexture("_ReflectionCameraTexture", renderTexture);
        }

        void LateUpdate()
        {
            followCamera = Camera.main;

            camera.fieldOfView = followCamera.fieldOfView;

            if (Screen.width != followCamera.pixelHeight || screenHeightOnInit != followCamera.pixelHeight)
            {
                ScaleOrViewChanged();
            }

            camera.farClipPlane = followCamera.farClipPlane;
            camera.nearClipPlane = followCamera.nearClipPlane;

            this.transform.transform.SetPositionAndRotation(new Vector3(followCamera.transform.position.x, (followCamera.orthographic) ? followCamera.transform.position.y : -followCamera.transform.position.y, followCamera.transform.position.z), followCamera.transform.rotation);
            this.transform.transform.localEulerAngles = new Vector3(-followCamera.transform.localEulerAngles.x, followCamera.transform.localEulerAngles.y, followCamera.transform.localEulerAngles.z);
        }

        private void ScaleOrViewChanged()
        {
            if (!this.gameObject.activeInHierarchy) return;

            camera.targetTexture = null;

            Destroy(renderTexture);
            CreateNewRenderTexture();
        }
    }
}
