using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Special2025.Firework
{
    public class FireworkBox : Firework
    {
        [SerializeField] private ParticleSystem fireworkTrailParticle;
        [SerializeField] private ParticleSystem fireworkParticle;
        [SerializeField] private GameObject trailParticle;

        [SerializeField] private List<FireworkBoxShot> fireworkShow;

        [SerializeField] private AudioSource shotAudioSource;
        [SerializeField] private List<AudioClip> explosionAudio;
        [SerializeField] private List<AudioClip> shotAudio;

        public override void StartFirework()
        {
            StartCoroutine(StartBox());
        }

        private IEnumerator StartBox()
        {
            for (int i = 0; i < fireworkShow.Count; i++)
            {
                FireworkBoxShot shot = fireworkShow[i];
                for (int j = 0; j < shot.shotCount; j++)
                {
                    shotAudioSource.PlayOneShot(shotAudio[Random.Range(0, shotAudio.Count)]);

                    for (int k = 0; k < shot.burstCount; k++)
                    {
                        if (shot.hasTrail) fireworkTrailParticle.Play(true);
                        else fireworkParticle.Play(true);
                        yield return new WaitForEndOfFrame();
                    }

                    yield return new WaitForSeconds(shot.delayBetweenShots);
                    fireworkTrailParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    fireworkParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
                yield return new WaitForSeconds(shot.endDelay);
            }
            fireworkTrailParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    [System.Serializable]
    public class FireworkBoxShot
    {
        public float delayBetweenShots;
        public bool hasTrail;
        public int shotCount;
        public int burstCount = 1;
        public float endDelay;
    }
}
