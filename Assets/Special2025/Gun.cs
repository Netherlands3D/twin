using System;
using System.Collections.Generic;
using Netherlands3D.FirstPersonViewer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

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
        private float charge = 0f;
        private float spawnStartDistance = 0.5f;

        private int maxProjectiles = 10;
        
        public List<GameObject> projectilePrefabs = new List<GameObject>();
        private int selectedPrefabIndex = 0;
        
        private Dictionary<string, List<Projectile>> projectilePool = new Dictionary<string, List<Projectile>>();
        private Dictionary<string, List<Projectile>> projectileActive = new Dictionary<string, List<Projectile>>();
        
        private ProjectileSelected projectileUI;

        private Quaternion lastRotation;
        
        // private void OnValidate()
        // {
        //     SetProjectileSelected(selectedPrefabIndex);
        // }
        
        //sticky objects
        //iets verder van camera af spawnen

        
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

        private void Start()
        {
            projectileUI = FindAnyObjectByType<ProjectileSelected>();
            projectileUI.next.onClick.AddListener(NextProjectile);
            projectileUI.previous.onClick.AddListener(PreviousProjectile);
            projectileUI.SetPowerEnabled(false);
            
            SetProjectileSelected(selectedPrefabIndex);
        }

        private void NextProjectile()
        {
            SetProjectileSelected(selectedPrefabIndex + 1);
        }

        private void PreviousProjectile()
        {
            SetProjectileSelected(selectedPrefabIndex - 1);
        }

        private void SetProjectileSelected(int index)
        {
            if(index < 0)
                index = projectilePrefabs.Count - 1;
            if(index >= projectilePrefabs.Count)
                index = 0;

            selectedPrefabIndex = index;
            projectileUI.SetImage(GetThumbnailPrefab(index));
            
            int count = projectilePrefabs.Count;
            int prev = (index - 1 + count) % count;
            int next = (index + 1) % count;
            projectileUI.SetImageForPrevious(GetThumbnailPrefab(prev));
            projectileUI.SetImageForNext(GetThumbnailPrefab(next));
        }

        private GameObject GetThumbnailPrefab(int index)
        {
            Projectile projectile = projectilePrefabs[index].GetComponent<Projectile>();
            if (projectile.ThumbnailVisual == null)
                return projectilePrefabs[index];
            return  projectile.ThumbnailVisual;
        }

        private void OnClickHandler(InputAction.CallbackContext context)
        {
            isShooting = true;
            projectileUI.SetPowerEnabled(true);
        }
        
        private void OnClickStopHandler(InputAction.CallbackContext context)
        {
            if (isShooting && !EventSystem.current.IsPointerOverGameObject())
            {
                if (cd < 0)
                {
                    OnFire();
                    cd = cooldown;
                }
            }
            isShooting = false;
            projectileUI.SetPowerEnabled(false);
        }

        private void Update()
        {
            cd -= Time.deltaTime;
            if (isShooting)
                charge += Time.deltaTime;
            else 
                charge = 0;
            charge = Mathf.Clamp01(charge);
            projectileUI.SetPower(charge);
            
            float deltaDegrees = Quaternion.Angle(lastRotation, transform.rotation);
            if(deltaDegrees > 0)
                charge = 0;
            lastRotation = transform.rotation;

            foreach(KeyValuePair<string, List<Projectile>> proj in projectileActive)
                if (proj.Value.Count > maxProjectiles)
                {
                    Despawn(proj.Value[0]);
                }
        }

        private void OnFire()
        {
            Vector2 screenPosition =  Pointer.current.position.ReadValue();
            Vector3 pos = fpvCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, fpvCamera.nearClipPlane));

            Projectile projectile = SpawnProjectile(pos + fpvCamera.transform.forward.normalized * spawnStartDistance, projectilePrefabs[selectedPrefabIndex].name);
            cooldown = projectile.Cooldown;
            projectileSpeed = projectile.Power * charge;
            
            projectile.rb.AddForce(fpvCamera.transform.forward * projectileSpeed, ForceMode.Impulse);
        }

        private Projectile SpawnProjectile(Vector3 position, string type)
        {
            Projectile projectile;
            if (projectilePool.ContainsKey(type) && projectilePool[type].Count > 0)
            {
                projectile = projectilePool[type][0];
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
                projectile = test.GetComponentInChildren<Projectile>();    
            }
            projectile.gameObject.transform.position = position;   
            projectile.gameObject.transform.rotation = Quaternion.identity;
            if(!projectileActive.ContainsKey(type))
                projectileActive.Add(type, new List<Projectile>());
            
            projectileActive[type].Add(projectile);
            projectile.Reset();
            projectile.SetGun(this);
            projectile.SetAlive(true);
            return projectile;
        }

        public void Despawn(Projectile obj)
        {
            if(!obj.IsAlive) return;
            
            obj.SetAlive(false);
            
            Projectile[] children = obj.gameObject.GetComponentsInChildren<Projectile>();
            foreach(Projectile child in children)
                Despawn(child);
            
            transform.parent = null; 
            obj.Reset();
            
            string type = obj.gameObject.name;
            type = type.Replace("(Clone)", "");
            bool removed = projectileActive[type].Remove(obj); //should be present
            if(!removed)
                Debug.LogError("projectile was not in the active list");
            
            if(!projectilePool.ContainsKey(type))
                projectilePool.Add(type, new List<Projectile>());
            
            projectilePool[type].Add(obj);
            obj.SetGun(null);
        }
    }
}
