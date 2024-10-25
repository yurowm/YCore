using System.Collections;
using System.Collections.Generic;
using Yurowm.Utilities;

namespace Yurowm.Nodes {
    public class CableNode : BasicNode {
        
        public readonly Port inputPort = new(0, "Input", Port.Info.Input, Side.Left);
        public readonly Port outputPort = new(1, "Output", Port.Info.Output, Side.Right);
        
        public override IEnumerable GetPorts() {
            yield return inputPort;
            yield return outputPort;
        }
        
        #if UNITY_EDITOR
        public override float width => 75;
        #endif

        public override void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {
            if (targetPort == inputPort) 
                PushWithArgs(outputPort, args);
        }

        public override IEnumerable<object> OnPortPulled(Port sourcePort, Port targetPort) {
            if (targetPort == outputPort)
                foreach (var arg in Pull(inputPort))
                    yield return arg;
        }
    }
}