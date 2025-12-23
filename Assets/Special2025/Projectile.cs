using System;
using UnityEngine;

namespace Netherlands3D
{
    public class Projectile : MonoBehaviour
    {
        public bool IsAlive => isAlive;
        
        public GameObject SplatVisual => splatVisual;
        
        public bool IsSticking => isSticking;
        
        [SerializeField] private GameObject splatVisual;
        [SerializeField] private GameObject deathVisual;

        [SerializeField] private float breakForce = 1000;
        [SerializeField] private GameObject thumbnailPrefab;
        
        public Rigidbody rb;
        public float Cooldown = 0.5f;
        public float Power = 60f;
        public bool IsSticky = false;

        private bool isSticking = false;

        private Gun gun;
        private bool isAlive = false;

        private ParticleSystem ps;

        private void Awake()
        {
            if(!rb)
                rb = GetComponent<Rigidbody>();
           
        }

        private void Start()
        {
            
        }

        public void SetGun(Gun gun)
        {
            this.gun = gun;
        }

        public void SetAlive(bool isAlive)
        {
            this.isAlive = isAlive;
            if(gameObject.activeInHierarchy != isAlive)
                gameObject.SetActive(isAlive);
        }

        private void OnCollisionEnter(Collision col)
        {
            if (col.contactCount == 0 || !isAlive || isSticking)
                return;
            
            var contact = col.GetContact(0);
            if (IsSticky)
            {
                //lets not stick to another sticking object or loads of recursive nonsense will happen
                Projectile projectile = col.gameObject.GetComponent<Projectile>();
                if (projectile != null && !projectile.isSticking)
                {
                    Attach(col.gameObject.transform);
                    return;
                }
                else if (projectile == null)
                {
                    Attach(col.gameObject.transform);
                    return;
                }
            }
            
            float energy = 0.5f * rb.mass * col.relativeVelocity.sqrMagnitude; //(0.5f * mass 1 * rel 60 * 60 ≈ 1800
            if (energy > breakForce)
            {
                CreateDeathEffect(contact.point);
                gun.Despawn(this);
            }

            
            CreateSplat(contact.point, contact.normal);
        }

        private void CreateSplat(Vector3 position, Vector3 normal)
        {
            if(splatVisual == null) return;
            
            var rot =  Quaternion.LookRotation(-normal, Vector3.up);
            Instantiate(splatVisual, position + (0.5f*normal), rot);
        }

        private void CreateDeathEffect(Vector3 position)
        {
            if(deathVisual == null) return;
            
            if (ps == null)
            {
                GameObject psg = Instantiate(deathVisual, transform.position, transform.rotation);
                ps = psg.GetComponent<ParticleSystem>();
            }
            ps.gameObject.transform.position = position;
            ps.Play();
        }

        public void Attach(Transform target)
        {
            if(isSticking) return;
            
            isSticking = true;
            transform.parent = target;
            rb.isKinematic = true;
            Destroy(rb);
            rb = null;
        }

        public void Reset()
        {
            if(rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            
            isSticking = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            //obj.Sleep();                 
            rb.ResetInertiaTensor();    
        }
    }
}
