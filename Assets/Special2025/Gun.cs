using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Netherlands3D.SelectionTools;
using System;
using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer;
using UnityEngine.Events;

namespace Netherlands3D
{
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
        private float cooldown = 0.5f;
        private float cd;

        private int maxProjectiles = 10;
        
        public List<GameObject> projectilePrefabs = new List<GameObject>();
        private string selectedPrefab = "Cube";
        
        private Dictionary<string, List<Rigidbody>> projectilePool = new Dictionary<string, List<Rigidbody>>();
        private Dictionary<string, List<Rigidbody>> projectileActive = new Dictionary<string, List<Rigidbody>>();
        
        
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
            
            fpvCamera = FindObjectOfType<FirstPersonViewerCamera>().GetComponent<Camera>();
            
            cd = cooldown;
            
        }

        private void OnClickHandler(InputAction.CallbackContext context)
        {
            if (cd > 0) return;
            
            cd = cooldown;
            
            Vector2 screenPosition =  Pointer.current.position.ReadValue();
            Vector3 pos = fpvCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, fpvCamera.nearClipPlane));

            Rigidbody rb = SpawnProjectile(pos, selectedPrefab);
            rb?.AddForce(fpvCamera.transform.forward * projectileSpeed, ForceMode.Impulse);
            
        }

        private void Update()
        {
            cd -= Time.deltaTime;
            
            foreach(KeyValuePair<string, List<Rigidbody>> proj in projectileActive)
                if (proj.Value.Count > maxProjectiles)
                {
                    Despawn(proj.Value[0]);
                }
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
            return projectileRb;
        }

        private void Despawn(Rigidbody obj)
        {
            obj.linearVelocity = Vector3.zero;
            obj.angularVelocity = Vector3.zero;
            obj.Sleep();                 
            obj.ResetInertiaTensor();    
            
            string type = obj.name;
            type = type.Replace("(Clone)", "");
            bool removed = projectileActive[type].Remove(obj); //should be present
            if(!removed)
                Debug.LogError("projectile was not in the active list");
            
            if(!projectilePool.ContainsKey(type))
                projectilePool.Add(type, new List<Rigidbody>());
            
            projectilePool[type].Add(obj);
        }
    }
}
