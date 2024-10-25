using Yurowm.Serialization;

namespace Yurowm.Sounds {
    public class Music : SoundBase {
        
        public string clipName;
        
        public override void Play(params object[] args) {
            var clip = SoundController.GetClip(clipName);
            if (clip)
                SoundController.PlayMusic(clip);
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