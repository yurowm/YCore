using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Nodes {
    public class AndBoleanNode : BasicNode {

        public readonly Port valuesPort = new Port(0, "Values (Booleans)", Port.Info.Input, Side.Top);
        public readonly Port resultPort = new Port(1, "Result (Boolean)", Port.Info.Output, Side.Bottom);

        public override string GetTitle() {
            return "And";
        }

        public override IEnumerable GetPorts() {
            yield return valuesPort;
            yield return resultPort;
        }

        public override IEnumerable<object> OnPortPulled(Port sourcePort, Port targetPort) {
            if (targetPort == resultPort) {
                yield return PullAll(valuesPort)
                    .CastIfPossible<bool>()
                    .All(v => v);
            }   
        }

        public override void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {
            if (targetPort == valuesPort) 
                Push(resultPort, args.CastIfPossible<bool>().All(v => v));
        }
    }
    
    public class OrBoleanNode : BasicNode {

        public readonly Port valuesPort = new Port(0, "Values (Booleans)", Port.Info.Input, Side.Top);
        public readonly Port resultPort = new Port(1, "Result (Boolean)", Port.Info.Output, Side.Bottom);

        public override string GetTitle() {
            return "Or";
        }

        public override IEnumerable GetPorts() {
            yield return valuesPort;
            yield return resultPort;
        }

        public override IEnumerable<object> OnPortPulled(Port sourcePort, Port targetPort) {
            if (targetPort == resultPort) {
                yield return PullAll(valuesPort)
                    .CastIfPossible<bool>()
                    .Any(v => v);
            }   
        }

        public override void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {
            if (targetPort == valuesPort) 
                Push(resultPort, args.CastIfPossible<bool>().Any(v => v));
        }
    }
}