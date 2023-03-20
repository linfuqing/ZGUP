using System.Collections.Generic;
using UnityEngine;

namespace ZG
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleAudioTrigger : MonoBehaviour
    {
        public AudioClip clip;
        public AudioSource style;
        private ParticleSystem __particleSystem;
        private List<ParticleSystem.Particle> __particles;

        private void OnParticleTrigger()
        {
            if (__particleSystem == null)
                __particleSystem = GetComponent<ParticleSystem>();

            if (__particles == null)
                __particles = new List<ParticleSystem.Particle>();
            else
                __particles.Clear();

            int count = __particleSystem.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, __particles);
            float length = clip == null ? 0 : clip.length;
            if (style == null)
            {
                for (int i = 0; i < count; ++i)
                    AudioSource.PlayClipAtPoint(clip, __particles[i].position);
            }
            else
            {
                AudioSource audioSource;
                Transform transform;
                GameObject gameObject;
                for (int i = 0; i < count; ++i)
                {
                    audioSource = Instantiate(style);
                    if (audioSource == null)
                        continue;

                    audioSource.clip = clip;
                    
                    transform = audioSource.transform;
                    if (transform != null)
                        transform.position = __particles[i].position;

                    gameObject = audioSource.gameObject;
                    gameObject.SetActive(true);

                    audioSource.Play();

                    Destroy(gameObject, length);
                }
            }
        }
    }
}