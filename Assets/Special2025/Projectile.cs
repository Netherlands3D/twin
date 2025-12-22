using System;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public bool IsAlive => isAlive;
        
        public GameObject SplatVisual => splatVisual;
        
        [SerializeField] private GameObject splatVisual;

        [SerializeField] private float breakForce = 100f;
        public Rigidbody rb;
        public float Cooldown = 0.5f;
        public float Power = 60f;

        private Gun gun;
        private bool isAlive = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void SetGun(Gun gun)
        {
            this.gun = gun;
        }

        public void SetAlive(bool isAlive)
        {
            this.isAlive = isAlive;
        }

        private void OnCollisionEnter(Collision col)
        {
            if (col.contactCount == 0 || !isAlive)
                return;
            
            var contact = col.GetContact(0);
            
            // print((col.impulse / Time.fixedDeltaTime).magnitude);
            if ((col.impulse / Time.fixedDeltaTime).magnitude > breakForce)
                gun.Despawn(this);

            CreateSplat(contact.point, contact.normal);
        }

        private void CreateSplat(Vector3 position, Vector3 normal)
        {
            var rot =  Quaternion.LookRotation(-normal, Vector3.up);
            Instantiate(splatVisual, position + (0.5f*normal), rot);
        }
    }
}
