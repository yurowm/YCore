using System.Collections;
using Cysharp.Threading.Tasks;
using Yurowm.Coroutines;
using Yurowm.Nodes;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Core {
    public class WaitNode : UserPathFilter {
        
        public readonly Port durationPort = new(2, "Duration (Float)", Port.Info.Input, Side.Left);

        public override IEnumerable GetPorts() {
            yield return base.GetPorts();
            yield return durationPort;
        }
        
        public float seconds = 5;

        public override async UniTask Logic() {
            if (Pull(durationPort, out float pulledDuration))
                await UniTask.WaitForSeconds(pulledDuration);
            else
                await UniTask.WaitForSeconds(seconds);
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