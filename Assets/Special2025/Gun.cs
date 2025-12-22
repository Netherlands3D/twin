using System.Collections.Generic;
using Netherlands3D.FirstPersonViewer;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D
{
    public enum ProjectileType
    {
        Cube,
        Sneeuwbal
    }
    
    public class Gun : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;

        public InputAction MoveAction { private set; get; }
        public InputAction SprintAction { private set; get; }
        public InputAction SpaceAction { private set; get; }
        public InputAction VerticalMoveAction { private set; get; }
        public InputAction LookInput { private set; get; }
        public InputAction ExitInput { private set; get; }
        public InputAction LeftClick { private set; get; }
        public InputAction ResetInput { private set; get; }

        public InputAction CycleNextModus { private set; get; }
        public InputAction CyclePreviousModus { private set; get; }
        
        public float ProjectileSpeed => projectileSpeed;
        public float Cooldown => cooldown;
        

        private Camera fpvCamera;
        private float projectileSpeed = 60;
        private float cooldown = 0.1f;
        private float cd;
        private bool isShooting = false;

        private int maxProjectiles = 10;
        
        [SerializeField] private ProjectileType projectileType = ProjectileType.Sneeuwbal;
        public List<GameObject> projectilePrefabs = new List<GameObject>();
        private string selectedPrefabName = "Cube";
        
        private Dictionary<string, List<Rigidbody>> projectilePool = new Dictionary<string, List<Rigidbody>>();
        private Dictionary<string, List<Rigidbody>> projectileActive = new Dictionary<string, List<Rigidbody>>();
        
        private void OnValidate()
        {
            SetProjectileType(projectileType);
        }

        public void SetProjectileType(ProjectileType type)
        {
            selectedPrefabName = type.ToString();
            projectileType = type;
        }

        
        private void Awake()
        {
            MoveAction = inputActionAsset.FindAction("Move");
            SprintAction = inputActionAsset.FindAction("Sprint");
            SpaceAction = inputActionAsset.FindAction("Space");
            VerticalMoveAction = inputActionAsset.FindAction("VerticalMove");
            LookInput = inputActionAsset.FindAction("Look");
            ExitInput = inputActionAsset.FindAction("Exit");
            LeftClick = inputActionAsset.FindAction("LClick");
            ResetInput = inputActionAsset.FindAction("Reset");
            CycleNextModus = inputActionAsset.FindAction("NavigateModusNext");
            CyclePreviousModus = inputActionAsset.FindAction("NavigateModusPrevious");

            LeftClick.performed += OnClickHandler;
            LeftClick.canceled += OnClickStopHandler;
            
            fpvCamera = FindObjectOfType<FirstPersonViewerCamera>().GetComponent<Camera>();
            
            cd = cooldown;
            
        }

        private void OnClickHandler(InputAction.CallbackContext context)
        {
            isShooting = true;
        }
        
        private void OnClickStopHandler(InputAction.CallbackContext context)
        {
            isShooting = false;
        }

        private void Update()
        {
            cd -= Time.deltaTime;
            if (isShooting)
            {
                if (cd < 0)
                {
                    cd = cooldown;
                    OnFire();
                }
            }

            foreach(KeyValuePair<string, List<Rigidbody>> proj in projectileActive)
                if (proj.Value.Count > maxProjectiles)
                {
                    Despawn(proj.Value[0]);
                }
        }

        private void OnFire()
        {
            Vector2 screenPosition =  Pointer.current.position.ReadValue();
            Vector3 pos = fpvCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, fpvCamera.nearClipPlane));

            Rigidbody rb = SpawnProjectile(pos, selectedPrefabName);
            rb?.AddForce(fpvCamera.transform.forward * projectileSpeed, ForceMode.Impulse);
        }

        private Rigidbody SpawnProjectile(Vector3 position, string type)
        {
            Rigidbody projectileRb;
            if (projectilePool.ContainsKey(type) && projectilePool[type].Count > 0)
            {
                projectileRb = projectilePool[type][0];
                projectilePool[type].RemoveAt(0);
            }
            else
            {
                GameObject prefab = projectilePrefabs.Find(p => p.name == type);
                if (prefab == null)
                {
                    Debug.LogError("selected prefab does not exist!");
                    return null; 
                }

                GameObject test = Instantiate(prefab);
                projectileRb = test.AddComponent<Rigidbody>();    
            }
            projectileRb.position = position;   
            if(!projectileActive.ContainsKey(type))
                projectileActive.Add(type, new List<Rigidbody>());
            
            projectileActive[type].Add(projectileRb);
            ResetRigidBody(projectileRb);
            return projectileRb;
        }

        private void Despawn(Rigidbody obj)
        {
            ResetRigidBody(obj);
            
            string type = obj.name;
            type = type.Replace("(Clone)", "");
            bool removed = projectileActive[type].Remove(obj); //should be present
            if(!removed)
                Debug.LogError("projectile was not in the active list");
            
            if(!projectilePool.ContainsKey(type))
                projectilePool.Add(type, new List<Rigidbody>());
            
            projectilePool[type].Add(obj);
        }

        private void ResetRigidBody(Rigidbody obj)
        {
            obj.linearVelocity = Vector3.zero;
            obj.angularVelocity = Vector3.zero;
            obj.Sleep();                 
            obj.ResetInertiaTensor();    
        }
    }
}
