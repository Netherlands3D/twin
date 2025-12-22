using System;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public GameObject SplatVisual => splatVisual;
        
        [SerializeField] private GameObject splatVisual;

        [SerializeField] private float breakForce = 100f;
        public Rigidbody rb;
        public float Cooldown = 0.5f;
        public float Power = 60f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnCollisionEnter(Collision col)
        {
            if (col.contactCount == 0)
                return;
            
            var contact = col.GetContact(0);
            
            // print((col.impulse / Time.fixedDeltaTime).magnitude);
            if((col.impulse / Time.fixedDeltaTime).magnitude > breakForce)
                Destroy(gameObject);

            CreateSplat(contact.point, contact.normal);
        }

        private void CreateSplat(Vector3 position, Vector3 normal)
        {
            var rot =  Quaternion.LookRotation(-normal, Vector3.up);
            Instantiate(splatVisual, position + (0.5f*normal), rot);
        }
    }
}
