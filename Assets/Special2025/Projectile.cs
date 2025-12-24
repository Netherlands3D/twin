using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D
{
    public class Projectile : MonoBehaviour
    {
        public bool IsAlive => isAlive;
        
        public GameObject SplatVisual => splatVisual;
        public GameObject ThumbnailVisual => thumbnailPrefab;
        
        public bool IsSticking => isSticking;
        
        [SerializeField] private GameObject splatVisual;
        [SerializeField] private GameObject deathVisual;

        [SerializeField] private float breakForce = 1000;
        public Rigidbody[] rb;
        public int activeRbIndex = 0;
        [SerializeField] private GameObject thumbnailPrefab;
        
        public float Cooldown = 0.5f;
        public float Power = 60f;
        public bool IsSticky = false;
        public bool ContinuousSplat = false;

        private bool isSticking = false;

        private Gun gun;
        private bool isAlive = false;

        private ParticleSystem ps;
        
        private static Dictionary<string, List<GameObject>> splatPool = new Dictionary<string, List<GameObject>>();
        private static int maxSplats = 1000;

        private void Awake()
        {
            GetDefaultRigidBody();
        }

        private void GetDefaultRigidBody()
        {
            if (rb == null || rb.Length == 0)
            {
                rb = new Rigidbody[1];
               
            }
            if(rb[0] == null)
                rb[0] = gameObject.GetComponent<Rigidbody>();

            if(rb[0] == null)
                rb[0] = gameObject.AddComponent<Rigidbody>();
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

            float energy = 0.5f * rb[activeRbIndex].mass * col.relativeVelocity.sqrMagnitude; //(0.5f * mass 1 * rel 60 * 60 ≈ 1800
            if (energy > breakForce)
            {
                CreateDeathEffect(contact.point);
                gun.Despawn(this);
            }

            if(!ContinuousSplat)
                CreateSplat(contact.point, contact.normal, col.relativeVelocity.sqrMagnitude);
        }

        private void OnCollisionStay(Collision col)
        {
            if (col.contactCount == 0 || !isAlive || isSticking)
                return;
            
            if(col.relativeVelocity.sqrMagnitude < 10) return;
            if(!ContinuousSplat) return;

            if (UnityEngine.Random.value > 0.1f) return;

            var contact = col.GetContact(0);
            CreateSplat(contact.point, contact.normal, col.relativeVelocity.sqrMagnitude);
        }

        private void CreateSplat(Vector3 position, Vector3 normal, float size)
        {
            if(splatVisual == null) return;
            
            if(!splatPool.ContainsKey(splatVisual.name))
                splatPool.Add(splatVisual.name, new List<GameObject>());
            
            var rot =  Quaternion.LookRotation(-normal, Vector3.up);

            GameObject splat;
            if (splatPool[splatVisual.name].Count > maxSplats)
            {
                splat = splatPool[splatVisual.name][0];
                splatPool[splatVisual.name].RemoveAt(0);
            }
            else
            {
                splat = Instantiate(splatVisual, position, rot);   
            }
            splatPool[splatVisual.name].Add(splat);
            splat.transform.position = position + 0.5f * normal;
            splat.transform.rotation = rot;
            DecalProjector proj = splat.GetComponent<DecalProjector>();
            Vector3 clampedSize = Vector3.one * Mathf.Clamp(size * 0.01f, 0.5f, 3f);
            clampedSize.z = 0.1f;
            proj.size = clampedSize;
            //splat.SetActive(true);
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
            if (isSticking) return;

            isSticking = true;
            transform.parent = target;
            rb[activeRbIndex].isKinematic = true;
            Destroy(rb[activeRbIndex]);
            rb[activeRbIndex] = null;
        }

        public void Reset()
        {
            GetDefaultRigidBody();

            isSticking = false;
            rb[activeRbIndex].linearVelocity = Vector3.zero;
            rb[activeRbIndex].angularVelocity = Vector3.zero;
            rb[activeRbIndex].isKinematic = false;
            //obj.Sleep();                 
            rb[activeRbIndex].ResetInertiaTensor();
        }

        public void Despawn() => gun.Despawn(this);
    }
}