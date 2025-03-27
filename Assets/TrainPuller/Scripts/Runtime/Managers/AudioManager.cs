using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager instance;

        [Header("Cached References")] public AudioLibrary audioLibrary;

        [SerializeField]
        private Dictionary<string, AudioClip> audioClipDictionary = new Dictionary<string, AudioClip>();

        [SerializeField] private List<AudioSource> audioSources;

        private void Awake()
        {
            InitializeSingleton();
            DontDestroyOnLoad(gameObject);
            InitializeAudioLibrary();
        }

        private void InitializeSingleton()
        {
            if (!instance)
            {
                instance = this;
            }
        }

        private void InitializeAudioLibrary()
        {
            foreach (var audioData in audioLibrary.audioClips)
            {
                audioClipDictionary.TryAdd(audioData.clipName, audioData.clip);
            }
        }

        private AudioSource GetOrCreateAudioSource()
        {
            foreach (var source in audioSources.Where(source => !source.isPlaying))
            {
                return source;
            }

            var newSource = gameObject.AddComponent<AudioSource>();
            audioSources.Add(newSource);
            return newSource;
        }

        public void PlaySound(string clipName, bool oneShot = true, bool loop = false, float volume = 1f,
            float pitch = 1f)
        {
            if (audioClipDictionary.TryGetValue(clipName, out var clip))
            {
                var source = GetOrCreateAudioSource();
                source.volume = volume;
                source.pitch = pitch;

                if (oneShot)
                {
                    source.PlayOneShot(clip, pitch);
                    return;
                }

                source.loop = loop;
                source.clip = clip;
                source.Play();
            }
            else
            {
                Debug.LogWarning($"AudioClip '{clipName}' not found in AudioLibrary.");
            }
        }
    }
}