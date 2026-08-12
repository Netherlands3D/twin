using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Netherlands3D.Masking
{
    public class MaskingDomeSpawner : MonoBehaviour
    {
        [Header("Placement actions")]
        [SerializeField] private InputActionReference clickPlacementAction;
        [SerializeField] private float maxCameraTravelToPlacement = 20.0f; 
        [SerializeField] private DisappearDome disappearEffect;
        [SerializeField] private float margin;

        [Header("Global shader settings")]
        [SerializeField] private string sphericalMaskFeatureKeyword = "_SPHERICAL_MASKING";
        [SerializeField] private string sphericalMaskPositionName = "_SphericalMaskPosition";
        [SerializeField] private string sphericalMaskRadiusName = "_SphericalMaskRadius";
        [SerializeField] private string sphericalMaskBitIndexName = "_SphericalMaskBitIndex";
    
        [SerializeField] private bool resetMaskOnDisable = true;
        private int positionPropertyID;
        private int radiusPropertyID;
        private int bitIndexPropertyID;
        private int maskingBitIndex = 22;
        
        public VisualDome DomeVisualisation => domeVisualisation;
        
        [SerializeField] private VisualDome domeVisualisation;
        [SerializeField] private Transform tempCanvas;
        
        public bool IsPointerOnDome => isPointerOnDome;
        private bool isPointerOnDome;

        private Camera mainCamera;
        private Vector3 cameraLookatPosition = Vector3.zero;

        private bool waitForInitialPlacement = false;

        private void Awake() {
            GetPropertyIDs();
            ApplyGlobalShaderVariables();
        }

        public void SetMaskingBitIndex(int bitIndex)
        {
            maskingBitIndex = bitIndex;
            //setting the bitIndex only needs to happen once, so it is done outside of the ApplyGlobalShaderVariables function.
            bitIndexPropertyID = Shader.PropertyToID(sphericalMaskBitIndexName);
            Shader.SetGlobalInt(bitIndexPropertyID, bitIndex);
        }

        public void SetDomeEnabled()
        {
            mainCamera = Camera.main;
            
            tempCanvas.gameObject.SetActive(true);
            domeVisualisation.gameObject.SetActive(true);
            
            Shader.EnableKeyword(sphericalMaskFeatureKeyword);

            clickPlacementAction.action.Enable();
            clickPlacementAction.action.started += StartTap;
            clickPlacementAction.action.performed += EndTap;

            StickToPointer();
            
            domeVisualisation.onHoveringChange.AddListener(OnPointerOnDome);
        }
        
        public void SetDomeDisabled()
        {
            tempCanvas.gameObject.SetActive(false);
            domeVisualisation.gameObject.SetActive(false);
            
            Shader.DisableKeyword(sphericalMaskFeatureKeyword);

            // Unsubscribe and disable the click action when the script is disabled
            clickPlacementAction.action.performed -= StartTap;
            clickPlacementAction.action.Disable();

            if(resetMaskOnDisable)
            {
                ResetGlobalShaderVariables();
            }
            
            domeVisualisation.onHoveringChange.RemoveListener(OnPointerOnDome);
        }

        /// <summary>
        /// Initial start will make dome follow pointer untill first click
        /// </summary>
        private void StickToPointer()
        {
            domeVisualisation.AnimateIn();
            waitForInitialPlacement = true;
        }

        private void OnPointerOnDome(bool onDome)
        {
            isPointerOnDome = onDome;
        }

        public void SpawnDisappearAnimation()
        {
            var newDisappearEffect = Instantiate(disappearEffect.gameObject,this.transform.parent);
            newDisappearEffect.GetComponent<DisappearDome>().DisappearFrom(domeVisualisation.transform.position, domeVisualisation.transform.localScale);
        }

        private void StartTap(InputAction.CallbackContext context)
        {
            cameraLookatPosition = LookPosition();
        }
        private void EndTap(InputAction.CallbackContext context)
        {
            var currentCameraLookatPosition = LookPosition();
            var distanceTraveled = Vector3.Distance(cameraLookatPosition, currentCameraLookatPosition);
            if(distanceTraveled < maxCameraTravelToPlacement)
            {
                PlaceDome();
            }
        }

        private Vector3 LookPosition()
        {
            // Calculate the pointer position in world space
            Ray ray = mainCamera.ScreenPointToRay(Vector3.one*0.5f);
            Plane plane = new Plane(Vector3.up, transform.position);
            plane.Raycast(ray, out float distance);
            Vector3 pointerWorldPosition = ray.GetPoint(distance);

            return pointerWorldPosition;
        }

        private void PlaceDome()
        {
            if(!EventSystem.current.IsPointerOverGameObject()){
                Vector2 pointerPosition = Pointer.current.position.ReadValue();

                if(!waitForInitialPlacement)
                    SpawnDisappearAnimation();

                domeVisualisation.MoveToScreenPoint(pointerPosition);
                domeVisualisation.AnimateIn();
            }

            waitForInitialPlacement = false;
        }      

        void Update()
        {
            if(waitForInitialPlacement)
            {
                domeVisualisation.MoveToScreenPoint(Pointer.current.position.ReadValue());
            }

            if (domeVisualisation.transform.hasChanged)
            {
                ApplyGlobalShaderVariables();
                domeVisualisation.transform.hasChanged = false;
            }
        }

        private void GetPropertyIDs(){
            positionPropertyID = Shader.PropertyToID(sphericalMaskPositionName);
            radiusPropertyID = Shader.PropertyToID(sphericalMaskRadiusName);
        }

        private void ApplyGlobalShaderVariables()
        {
            Shader.SetGlobalVector(positionPropertyID,domeVisualisation.transform.position);
            Shader.SetGlobalFloat(radiusPropertyID,(domeVisualisation.transform.localScale.x/2.0f) + margin);
        }

        private void ResetGlobalShaderVariables()
        {
            Shader.SetGlobalVector(positionPropertyID,Vector3.zero);
            Shader.SetGlobalFloat(radiusPropertyID,0.0f);
        }
    }
}
