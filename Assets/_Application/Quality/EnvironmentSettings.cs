using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Quality
{
    public class EnvironmentSettings : MonoBehaviour
    {
        public static EnvironmentProfile ActiveEnvironmentProfile;
        public static Light sun;

        [SerializeField]
        private int skyIndexMobile = 1;

        [SerializeField]
        private int skyIndexDesktop = 0;

        [SerializeField]
        private EnvironmentProfile[] selectableProfiles;

        public EnvironmentProfile[] SelectableProfiles { get => selectableProfiles; private set => selectableProfiles = value; }

        [SerializeField]
        private Material proceduralSkyMaterial;
        [SerializeField]
        private Material texturedSkyMaterial;

        [SerializeField]
        private Light directionalLightSun;

        [SerializeField]
        private float sunUpAmount = 1.0f;
        [SerializeField]
        private float sunDownAmount = -1.0f;

        [SerializeField]
        private Material intensityMaterialTrees;

        private static bool visualsUpdateRequired = false;

        private static bool useSkyboxForReflections = true;

        [SerializeField]
        private MeshRenderer sunGraphic;
        [SerializeField]
        private MeshRenderer sunHaloGraphic;

        public static EnvironmentSettings Instance;

        private Coroutine loadingTextureProgress;

        private void Awake()
        {
            Instance = this;

            //Work on copies of our EnvironmentSettings profiles
            for (int i = 0; i < selectableProfiles.Length; i++)
            {
                selectableProfiles[i] = Instantiate(selectableProfiles[i]);
            }

            if (directionalLightSun)
            {
                sun = directionalLightSun;
            }
            else
            {
                sun = FindObjectOfType<Light>();
            }
        }

        public void ApplyEnvironment(bool mobile = false)
        {
            //Load up our environment based on platform (mobile should be lightweight)
            ApplyEnvironmentProfile((mobile) ? skyIndexMobile : skyIndexDesktop);
            UpdateSunBasedVisuals();
        }

        public static void SetSunAngle(Vector3 angles)
        {
            sun.transform.localRotation = Quaternion.Euler(angles);
            visualsUpdateRequired = true;
        }

        private void Update()
        {
            if (visualsUpdateRequired)
                UpdateSunBasedVisuals();
        }

        public void ApplyEnvironmentProfile(int profileIndex)
        {
            var profile = selectableProfiles[profileIndex];

            if (loadingTextureProgress != null) StopCoroutine(loadingTextureProgress);
            loadingTextureProgress = StartCoroutine(LoadEnvironmentProfile(profile));

            ApplyReflectionSettings();
        }
        private IEnumerator LoadEnvironmentProfile(EnvironmentProfile environmentProfile)
        {
            //Always destroy our prevous loaded skybox texture
            if(ActiveEnvironmentProfile && ActiveEnvironmentProfile.loadedTexture != null)
            {
                Destroy(ActiveEnvironmentProfile.loadedTexture);
            }

            //Select our new profile
            ActiveEnvironmentProfile = environmentProfile;

            if (ActiveEnvironmentProfile.isTexturedSky && ActiveEnvironmentProfile.texturePath != "")
            {
                var skyboxTextureUrl = "";
                var splitTarget = Application.absoluteURL.LastIndexOf('/');
                if (splitTarget != -1)
                {
                    var relativeDirectory = Application.absoluteURL.Substring(0, splitTarget);
                    skyboxTextureUrl = $"{relativeDirectory}/{ActiveEnvironmentProfile.texturePath}";
                }
#if UNITY_EDITOR
                skyboxTextureUrl = $"file:///{Application.dataPath}/WebGLTemplates/Netherlands3D/{ActiveEnvironmentProfile.texturePath}";
#endif

                Debug.Log("Loading skybox texture:" + skyboxTextureUrl);
                using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(skyboxTextureUrl,true))
                {
                    yield return uwr.SendWebRequest();
                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(uwr.error);
                    }
                    else
                    {
                        // Get downloaded asset bundle
                        ActiveEnvironmentProfile.loadedTexture = (Texture2D)DownloadHandlerTexture.GetContent(uwr);
                        texturedSkyMaterial.SetTexture("_Tex", ActiveEnvironmentProfile.loadedTexture);
                        //texturedSkyMaterial.SetColor();
                        RenderSettings.skybox = texturedSkyMaterial;

                        SetReflections(useSkyboxForReflections);
                    }
                }
            }
            else
            {
                SetReflections(useSkyboxForReflections);
                RenderSettings.skybox = proceduralSkyMaterial;
            }

            //Set the proper graphic for the representation of the Sun
            sunGraphic.enabled = ActiveEnvironmentProfile.sunTexture;
            sunGraphic.material.SetTexture("_MainTexture", ActiveEnvironmentProfile.sunTexture);

            sunHaloGraphic.enabled = ActiveEnvironmentProfile.haloTexture;
            sunHaloGraphic.material.SetTexture("_MainTexture", ActiveEnvironmentProfile.haloTexture);

            UpdateSunBasedVisuals();
        }

        public static void SetReflections(bool realtimeReflectionsAreOn = false)
        {
            useSkyboxForReflections = realtimeReflectionsAreOn;

            if (!ActiveEnvironmentProfile) return;
            ApplyReflectionSettings();
        }

        private static void ApplyReflectionSettings()
        {
            if (!useSkyboxForReflections)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            }
            else
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            }
        }
    
        public void UpdateSunBasedVisuals()
        {
            //Reduce sun strength when we go down the horizon
            sun.intensity = Mathf.InverseLerp(sunDownAmount, sunUpAmount, Vector3.Dot(sun.transform.forward,Vector3.up));

            //Apply sunlight to tree darkness (who use a very simple unlit shader)
            intensityMaterialTrees.SetFloat("_Light", Mathf.Max(sun.intensity, 0.3f));

            //Change the fog and ambient color based on this intensity
            RenderSettings.fogColor = Color.Lerp(
                ActiveEnvironmentProfile.fogColorNight,
                ActiveEnvironmentProfile.fogColorDay,
                sun.intensity
            );

            //Sky colors
            RenderSettings.ambientSkyColor = Color.Lerp(
                ActiveEnvironmentProfile.skyColorsNight[0],
                ActiveEnvironmentProfile.skyColorsDay[0],
                sun.intensity
            );
            RenderSettings.ambientEquatorColor = Color.Lerp(
                ActiveEnvironmentProfile.skyColorsNight[1],
                ActiveEnvironmentProfile.skyColorsDay[1],
                sun.intensity
            );
            RenderSettings.ambientGroundColor = Color.Lerp(
                ActiveEnvironmentProfile.skyColorsNight[2],
                ActiveEnvironmentProfile.skyColorsDay[2],
                sun.intensity
            );

            if(ActiveEnvironmentProfile.isTexturedSky)
                RenderSettings.skybox.SetFloat("_Exposure", sun.intensity);

            var sunHorizon = Mathf.Clamp(Mathf.InverseLerp(0.6f, 0.7f, sun.intensity),0.0f,1.0f);

            if(ActiveEnvironmentProfile.haloTexture)
                sunHaloGraphic.material.SetColor("_BaseColor", Color.Lerp(Color.black, ActiveEnvironmentProfile.sunHaloTextureTintColor * sunHorizon, sun.intensity));

            visualsUpdateRequired = false;
        }
    }
}
