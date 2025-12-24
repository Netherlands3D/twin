using System;
using UnityEngine;

namespace Netherlands3D
{
    public class RopeJoint : MonoBehaviour
    {
        [SerializeField] 
        private bool kinematicOnCollision = false;
        private Rigidbody rb;
        [SerializeField] private Vector3 offset = Vector3.zero;
        public bool attatchToCamera;
        private Gun gun;

        private void Awake()
        {
            rb =  GetComponent<Rigidbody>();
            gun = FindAnyObjectByType<Gun>();
        }

        private void OnCollisionEnter(Collision other)
        {
            if (kinematicOnCollision)
            {
                rb.isKinematic = true;
                GetComponent<SphereCollider>().radius = 0.1f;
            }
        }

        private void FixedUpdate()
        {
            if (attatchToCamera)
            {
                var rotatedOffset = gun.transform.rotation * offset;
                rb.MovePosition(gun.transform.position + rotatedOffset);
            }
        }
    }
}
