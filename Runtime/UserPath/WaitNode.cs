using System.Collections;
using Yurowm.Coroutines;
using Yurowm.Nodes;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Core {
    public class WaitNode : UserPathFilter {
        
        public readonly Port durationPort = new Port(2, "Duration (Float)", Port.Info.Input, Side.Left);

        public override IEnumerable GetPorts() {
            yield return base.GetPorts();
            yield return durationPort;
        }
        
        public float seconds = 5;

        public override IEnumerator Logic() {
            if (Pull(durationPort, out float pulledDuration))
                yield return new Wait(pulledDuration);
            else
                yield return new Wait(seconds);
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("seconds", seconds);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("seconds", ref seconds);
        }
    }
}