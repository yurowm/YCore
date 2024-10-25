using System.Collections;
using Yurowm.Utilities;

namespace Yurowm.Nodes {
    public class IfNode : BasicNode {

        public readonly Port inputPort = new Port(0, "Trigger", Port.Info.Input, Side.Top);
        public readonly Port conditionPort = new Port(1, "Condition (Boolean)", Port.Info.Input, Side.Top);
        public readonly Port truePort = new Port(2, "True", Port.Info.Output, Side.Bottom);
        public readonly Port falsePort = new Port(3, "False", Port.Info.Output, Side.Bottom);
        
        public override IEnumerable GetPorts() {
            yield return inputPort;
            yield return conditionPort;
            yield return truePort;
            yield return falsePort;
        }
        
        bool? condition;
        
        #if UNITY_EDITOR

        public override float width => 100;

        #endif
        
        public override void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {
            if (targetPort == inputPort) {
                if (Pull(conditionPort, out bool conditionValue))                
                    condition = conditionValue;
                
                if (condition.HasValue) 
                    Push(condition.Value ? truePort : falsePort);
            }
        }
    }
}