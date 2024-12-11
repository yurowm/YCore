using System.Collections;
using Cysharp.Threading.Tasks;
using Yurowm.Integrations;
using Yurowm.Serialization;

namespace Yurowm.Analytics {
    public abstract class AnalyticIntegration : Integration {
        
        public bool trackAll = true;
        
        protected override async UniTask Initialize() {
            Analytic.integrations.Add(this);
            Analytic.log = true;
        }

        public abstract void Event(string eventName);

        public virtual void Event(string eventName, params Segment[] segments) {
            Event(eventName);
        }

        public override string GetIntegrationType() => $"Analytics,{base.GetIntegrationType()}";
        
        #region ISerializable

        public override void Serialize(IWriter writer) {
            writer.Write("trackAll", trackAll);
            base.Serialize(writer);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("trackAll", ref trackAll);
            base.Deserialize(reader);
        }

        #endregion
    }
}