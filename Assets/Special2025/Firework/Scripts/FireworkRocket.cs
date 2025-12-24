using UnityEngine;

namespace Netherlands3D.Special2025.Firework
{
    [RequireComponent(typeof(Rigidbody))]
    public class FireworkRocket : Firework
    {
        private Rigidbody rb;

        [SerializeField] private AudioSource audioSource;

        [SerializeField] private Vector2 timeBeforeExploding;
        //[SerializeField] private float timeBeforeExploding;
        private float explosionTimer;

        [SerializeField] private ParticleSystem rocketLaunchParticle;
        [SerializeField] private ParticleSystem explosionParticlePrefab;

        public float launchForce = 5f;
        public float accelerationTime = 2f;
        private float timer;

        private Projectile projectile;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            projectile = GetComponent<Projectile>();

        }

        protected override void OnEnable()
        {
            base.OnEnable();
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            audioSource.Stop();
            rocketLaunchParticle.Stop();
            explosionTimer = Random.Range(timeBeforeExploding.x, timeBeforeExploding.y);
        }

        public override void StartFirework()
        {
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            audioSource.Play();
            rocketLaunchParticle.Play();
        }

        protected override void Update()
        {
            base.Update();

            if (!FuseEnded) return;

            Vector3 randomUp = (transform.up + new Vector3(Random.Range(-0.1f, 0.1f), 0, Random.Range(-0.1f, 0.1f)) + Vector3.up * .05f).normalized;

            if (timer < accelerationTime)
            {
                float t = timer / accelerationTime; 
                rb.AddForce(randomUp * launchForce * t, ForceMode.Force);
                timer += Time.deltaTime;
            }

            explosionTimer = Mathf.Max(explosionTimer - Time.deltaTime, 0);
            if (explosionTimer == 0) Explode();
        }

        private void Explode()
        {
            Instantiate(explosionParticlePrefab, transform.position, Quaternion.LookRotation(transform.up));
            projectile.Despawn();
        }
    }
}
