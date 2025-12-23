using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Special2025.Firework
{
    public class FireworkBox : Firework
    {
        [SerializeField] private ParticleSystem fireworkTrailParticle;
        [SerializeField] private ParticleSystem fireworkParticle;
        [SerializeField] private ParticleSystem fireworkBallParticle;
        [SerializeField] private GameObject trailParticle;

        [SerializeField] private List<FireworkBoxShot> fireworkShow;

        [SerializeField] private AudioSource explosioSource;
        [SerializeField] private List<AudioClip> explosionAudio;
        [SerializeField] private AudioSource shotAudioSource;
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
                    for (int k = 0; k < shot.burstCount; k++)
                    {
                        shotAudioSource.PlayOneShot(shotAudio[Random.Range(0, shotAudio.Count)]);

                        ParticleSystem ps = null;
                        switch (shot.fireworKType)
                        {
                            case FireworkType.Regular:
                                ps = fireworkParticle;
                                break;
                            case FireworkType.Trail:
                                ps = fireworkTrailParticle;
                                break;
                            case FireworkType.Ball:
                                ps = fireworkBallParticle;
                                break;
                        }

                        ps.Play(true);

                        if (shot.fireworKType != FireworkType.Ball)
                        {
                            float lifeTime;
                            if (ps.main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants) lifeTime = Random.Range(ps.main.startLifetime.constantMin, ps.main.startLifetime.constantMax);
                            else lifeTime = ps.main.startLifetime.constant;

                            StartCoroutine(PlayAudioAfterLifetime(lifeTime - .1f));
                        }

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

        private IEnumerator PlayAudioAfterLifetime(float duration)
        {
            yield return new WaitForSeconds(duration);

            explosioSource.PlayOneShot(explosionAudio[Random.Range(0, shotAudio.Count)]);
        }
    }

    public enum FireworkType { Regular, Trail, Ball }

    [System.Serializable]
    public class FireworkBoxShot
    {
        public float delayBetweenShots;
        public FireworkType fireworKType;
        public int shotCount;
        public int burstCount = 1;
        public float endDelay;
    }
}
