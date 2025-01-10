using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public static class SoundController {
        
        const string coreName = "YSounds";
        
        const HideFlags hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
        
        static void BuildCore() {
            core = new GameObject(coreName);
            core.transform.Reset();
            core.hideFlags = hideFlags;
            
            sfxSource = core.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            
            sfxSourceUnmute = core.AddComponent<AudioSource>();
            sfxSourceUnmute.loop = false;
            sfxSourceUnmute.volume = 1;
            sfxSourceUnmute.playOnAwake = false;
            sfxSourceUnmute.rolloffMode = AudioRolloffMode.Linear;
            
            SoundVolume = GameSettings.Instance?.GetModule<Yurowm.Audio.AudioSettings>().SFX ?? 1;

            if (Application.isPlaying) {
                var listener = new GameObject(nameof(AudioListener));
                listener.AddComponent<AudioListener>();
                listener.hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable;
            }
            
            specialSources.Clear();
            clips.Clear();    
        }

        static GameObject core;
        static AudioSource sfxSource;
        static AudioSource sfxSourceUnmute;
        static AudioSource musicSource;
        static List<AudioSource> specialSources = new List<AudioSource>();

        static float _SoundVolume = 1;
        public static float SoundVolume {
            get => _SoundVolume;
            set {
                _SoundVolume = value.Clamp01();
                if (sfxSource)
                    sfxSource.volume = _SoundVolume * 1f;
            }
        }
        
        public static float MusicVolumeMultiplier = 1f;
        
        static float _MusicVolume = 1;
        public static float MusicVolume {
            get => _MusicVolume;
            set {
                _MusicVolume = value.Clamp01();
                if (musicSource)
                    musicSource.volume = _MusicVolume * MusicVolumeMultiplier;
            }
        }
        
        public static void PlayEffect(AudioClip clip, float? volume = null) {
            
            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                PlayEffectSpecialSource(clip, out _);
                return;
            }
            #endif
            
            if (clip && sfxSource) {
                if (volume.HasValue)
                    sfxSourceUnmute.PlayOneShot(clip, volume.Value);
                else
                    sfxSource.PlayOneShot(clip);
            }
        }
        
        public static void PlayEffectSpecialSource(AudioClip clip, out AudioSource source) {
            source = null;
            
            if (!clip) return;
            
            if (!core)
                BuildCore();

            #if UNITY_EDITOR
            if (!Application.isPlaying) {
                if (specialSources.Any(s => !s))
                    BuildCore();
                source = sfxSource;
                source.Stop();
            } else
            #endif
                source = specialSources.FirstOrDefault(s => !s.isPlaying);
            
            if (!source) {
                var go = new GameObject("SpecialSource");
                go.transform.SetParent(core.transform);
                go.transform.Reset();
                go.hideFlags = hideFlags;
                source = go.AddComponent<AudioSource>();
                source.rolloffMode = AudioRolloffMode.Linear;
                specialSources.Add(source);
            }
            
            source.clip = clip;
            source.volume = SoundVolume;
            source.pitch = 1f;
            source.loop = false;
            source.Play();
        }

        public static void PlayMusic(AudioClip clip) {
            if (clip == null)
                return;
            
            if (musicSource && musicSource.isPlaying && musicSource.clip == clip) {
                MusicVolume = MusicVolume;
                return;
            }

            PlayEffectSpecialSource(clip, out var source);
            
            StopMusic();
            
            musicSource = source;
            musicSource.loop = true;
            musicSource.volume = 0;
            
            Fade(musicSource, MusicVolume * MusicVolumeMultiplier).Forget();
        }

        static async UniTask Fade(AudioSource source, float volume) {
            if (!source) 
                return;
            
            var startVolume = source.volume;
            
            for (var t = 0f; t < 1f; t += Time.unscaledDeltaTime) {
                source.volume = YMath.Lerp(startVolume, volume, t);
                await UniTask.Yield();
            }
            source.volume = volume;
        }

        public static void StopMusic() {
            if (!musicSource) return;
            
            var source = musicSource;

            Fade(source, 0f)
                .ContinueWith(source.Stop)
                .Forget();
        }

        #region Clips

        static Dictionary<string, AudioClip> clips = new();
        
        public static AudioClip GetClip(string name) {
            return AssetManager.GetAsset<AudioClip>(name);
        }
        
        #endregion

        #region Extra
        
        public static Action<bool> onMuteChanged = delegate{};
        
        static HashSet<object> muters = new();

        public static void MuteWith(object obj, bool status) {
            if (status && muters.Add(obj))
                onMuteChanged?.Invoke(status);
            
            if (!status && muters.Remove(obj))
                onMuteChanged?.Invoke(status);
        }
        
        public static bool IsMute() => muters.Any();

        public const string RootFolderName = "Sounds";

        public static string GetHapticFullPath(string hapticPath) {
            if (hapticPath.IsNullOrEmpty())
                return null;
            
            return $"Sounds/{hapticPath}.ahap";
        } 

        #endregion
    }
}