using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Sounds;
using AudioSettings = Yurowm.Audio.AudioSettings;

namespace Yurowm {
    public class ContentSound : BaseBehaviour, IClipComponent {
        public List<Sound> clips = new();
        static Dictionary<string, float> limiter = new();
        const float limiterDelay = .1f;
        
        Dictionary<string, string> _clips = new();
        
        bool isInitialized;
        
        void Initialize() {
            _clips = clips.ToDictionary(x => x.name, x => x.clip);
            
            isInitialized = true;
        }

        public bool HasClip(string soundName) {
            if (soundName.IsNullOrEmpty()) return false;
                
            if (!isInitialized) Initialize();
            
            var clip = _clips.Get(soundName);

            return !clip.IsNullOrEmpty();
        }
        
        /// For Animation events
        public void PlaySound(string soundName) {
            Play(soundName);
        }
        
        public void Play(string soundName) {
            if (soundName.IsNullOrEmpty()) return;
                
            if (!isInitialized) Initialize();

            var clip = _clips.Get(soundName);
            
            if (!clip.IsNullOrEmpty())
                Shot(clip, gameObject);
        }
        
        public static void Shot(string clip, GameObject gameObject) {
            if (GetAccessFor(clip))
                SoundBase.storage.GetItemByID<Sounds.SoundEffect>(clip)?.Play(gameObject);
        }
           
        AudioSettings settings;

        static bool GetAccessFor(string soundName) {
            limiter.TryAdd(soundName, 0);
            
            if (limiter[soundName] + limiterDelay <= Time.unscaledTime) {
                limiter[soundName] = Time.unscaledTime;
                return true;
            }
            
            return false;
        }
        
        public struct Parameter {
            public readonly string name;
            public readonly float value;

            public Parameter(string name, float value) {
                this.name = name;
                this.value = value;
            }
        }
        
        [System.Serializable]
        public class Sound {
            public string name;
            public string clip;

            public Sound(string _name) {
                name = _name;
            }

            public static bool operator ==(Sound a, Sound b) {
                return Equals(a, b);
            }

            public static bool operator !=(Sound a, Sound b) {
                return !Equals(a, b);
            }

            public override bool Equals(object obj) {
                if (obj is Sound sound)
                    return Equals(this, sound);
                return false;
            }
            
            static bool Equals(Sound a, Sound b) {
                return a?.name == b?.name;
            }
        }
    }
}