using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Netherlands3D.Twin
{
    public class MusicRandomiser : MonoBehaviour
    {
        private AudioSource source;
        [SerializeField] private AudioClip[] clips;
        private List<int> indices;
        private int currentPlayIndex = 0;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            ShuffleAudioClips(clips);
        }

        private void Update()
        {
            if (source.isPlaying)
                return;

            currentPlayIndex++;
            currentPlayIndex %= clips.Length;
            source.clip = clips[currentPlayIndex];
            source.Play();
        }

        void ShuffleAudioClips(AudioClip[] audioClips)
        {
            int n = audioClips.Length;
            System.Random random = new System.Random();

            for (int i = n - 1; i > 0; i--)
            {
                int randIndex = random.Next(0, i + 1);

                // Swap audio clips[i] and audio clips[randIndex]
                (audioClips[i], audioClips[randIndex]) = (audioClips[randIndex], audioClips[i]);
            }
        }
    }
}