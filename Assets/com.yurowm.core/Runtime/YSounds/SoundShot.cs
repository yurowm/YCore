using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;
using AudioSettings = Yurowm.Audio.AudioSettings;

namespace Yurowm.Sounds {
    /// <summary>
    ///  Legacy
    /// </summary>
    public class SoundShot : SoundEffect {
        public string clipName;
        
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