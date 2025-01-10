using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;
using AudioSettings = Yurowm.Audio.AudioSettings;

namespace Yurowm.Sounds {
    public class SoundShot : Sound {
        public string clipName;
        
        DelayedAccess throttling = new(1f / 20f);
        
        public override void Play(params object[] args) {
            base.Play(args);
            
            var sounds = GameSettings.Instance.GetModule<AudioSettings>().SFX > 0;
            
            if (!sounds) return;

            if (clipName.IsNullOrEmpty() || !throttling.GetAccess()) 
                return;
            
            var clip = SoundController.GetClip(clipName);
            
            SoundController.PlayEffect(clip);
        }
        
        public void PlayForce() {
            if (clipName.IsNullOrEmpty()) 
                return;
            
            var clip = SoundController.GetClip(clipName);
            
            Debug.Log(clipName);
            
            SoundController.PlayEffect(clip, 1);
        }

        public override IEnumerable<string> GetAllPath() {
            yield break;
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("clipName", clipName);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("clipName", ref clipName);
        }
    }
}