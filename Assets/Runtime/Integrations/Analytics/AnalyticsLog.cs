using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Analytics {
    public class AnalyticsLog : AnalyticIntegration {
        public override string GetName() {
            return "Analytics Log";
        }

        public override void Event(string eventName) {
            Debug.Log($"event: {eventName}");
        }

        public override void Event(string eventName, params Segment[] segments) {
            Debug.Log($"event: {eventName}{segments.Select(s => $"\n{s.ID}: {s.value}").Join()}");
        }
    }
}