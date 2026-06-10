using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Quality
{
    public class WaterReflectionCamera : MonoBehaviour
    {
        private const int MINIMUM_DEPTH_BUFFER_FORMAT = 16; //In the render graph API, the output Render Texture must have a depth buffer, this is the minimum value to keep the render texture light weight.
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

        private void Start()
        {
            followCamera = App.Cameras.ActiveCamera;
            App.Cameras.OnSwitchCamera.AddListener(SetCamera);

            if (!renderTexture)
                CreateNewRenderTexture();
        }

        public void SetCamera(Camera camera)
        {
            followCamera = camera;
        }

        private void OnEnable()
        {
            exampleFeatureKeyword = GlobalKeyword.Create(waterReflectionsFeatureKeyword);
            Shader.EnableKeyword(waterReflectionsFeatureKeyword);

            if (!camera)
                camera = GetComponent<Camera>();

            
        }

        private void OnDisable()
        {
            Shader.DisableKeyword(waterReflectionsFeatureKeyword);

            camera.targetTexture = null;
            Destroy(renderTexture);
        }

        private void CreateNewRenderTexture()
        {
            renderTexture = new RenderTexture(Mathf.RoundToInt(followCamera.pixelWidth * ScaleMultiplier), Mathf.RoundToInt(followCamera.pixelHeight * ScaleMultiplier), MINIMUM_DEPTH_BUFFER_FORMAT);
            screenWidthOnInit = followCamera.pixelWidth;
            screenHeightOnInit = followCamera.pixelHeight;
            camera.targetTexture = renderTexture;
            Shader.SetGlobalTexture("_ReflectionCameraTexture", renderTexture);
        }

        void LateUpdate()
        {
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
        
        private void OnDestroy()
        {
            App.Cameras.OnSwitchCamera.RemoveListener(SetCamera);
        }
    }
}
