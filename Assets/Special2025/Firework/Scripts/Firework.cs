using UnityEngine;

namespace Netherlands3D.Special2025.Firework
{
    public abstract class Firework : MonoBehaviour
    {
        [SerializeField] private float fuseTime;
        private float fuseTimer;

        protected bool FuseEnded { private set; get; }
        
        protected virtual void OnEnable()
        {
            FuseEnded = false;
            fuseTimer = fuseTime;
        }


        protected virtual void Update()
        {
            fuseTimer = Mathf.Max(fuseTimer - Time.deltaTime, 0);

            if(fuseTimer == 0 && !FuseEnded)
            {
                FuseEnded = true;
                StartFirework();
            }
        }

        public abstract void StartFirework();
    }
}
