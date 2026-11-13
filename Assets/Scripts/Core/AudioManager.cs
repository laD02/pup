using System.Collections.Generic;
using UnityEngine;

namespace PufferLaunch.Core
{
    /// <summary>Music + pooled SFX. Lives across scenes; volumes come from the save file.</summary>
    public class AudioManager : MonoBehaviour
    {
        const int SfxVoices = 10;

        static AudioManager instance;
        AudioSource music;
        readonly List<AudioSource> voices = new List<AudioSource>();
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        int nextVoice;

        /// <summary>Mute or volume changed somewhere (pause menu, settings, a HUD button): sound icons redraw from state.</summary>
        public static event System.Action Changed;

        public static void NotifyChanged() => Changed?.Invoke();

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("Audio");
                    DontDestroyOnLoad(go);
                    instance = go.AddComponent<AudioManager>();
                    instance.Init();
                }
                return instance;
            }
        }

        void Init()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            for (int i = 0; i < SfxVoices; i++)
            {
                var v = gameObject.AddComponent<AudioSource>();
                v.playOnAwake = false;
                voices.Add(v);
            }
            ApplyVolumes();
        }

        public void ApplyVolumes()
        {
            var d = SaveSystem.Data;
            music.volume = d.muted ? 0f : d.musicVolume;
            foreach (var v in voices) v.volume = d.muted ? 0f : d.sfxVolume;
        }

        public void PlayMusic(string name)
        {
            var clip = Clip(name);
            if (clip == null || music.clip == clip) return;
            music.clip = clip;
            music.Play();
        }

        public static void Sfx(string name, float pitchJitter = 0.06f, float volume = 1f)
        {
            var self = Instance;
            var clip = self.Clip(name);
            if (clip == null) return;
            // A free voice first: re-pitching a voice that is still playing bends the earlier sound too.
            AudioSource v = null;
            for (int i = 0; i < self.voices.Count && v == null; i++)
            {
                var candidate = self.voices[(self.nextVoice + i) % self.voices.Count];
                if (!candidate.isPlaying) v = candidate;
            }
            if (v == null) v = self.voices[self.nextVoice];
            self.nextVoice = (self.voices.IndexOf(v) + 1) % self.voices.Count;
            v.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            v.PlayOneShot(clip, volume);
        }

        AudioClip Clip(string name)
        {
            if (!clips.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("Audio/" + name);
                if (clip == null) Debug.LogWarning($"[PufferLaunch] Missing audio clip Resources/Audio/{name}");
                clips[name] = clip;
            }
            return clip;
        }
    }
}
