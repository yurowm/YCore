using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.Analytics {
    public static class Analytic {
        public static bool log = false;

        public static List<AnalyticIntegration> integrations = new();
        
        static IEnumerable<AnalyticIntegration> AllActive() {
            foreach (var integration in integrations)
                if (integration != null && integration.active)
                    yield return integration;
        }
        
        static IEnumerable<AnalyticIntegration> AllFullTracked() {
            foreach (var integration in integrations)
                if (integration != null && integration.active && integration.trackAll)
                    yield return integration;
        }

        #region Events
        
        public static void Event(string eventName) {
            if (!log) return;
            AllFullTracked().ForEach(x => x.Event(eventName));
        }

        public static void Event(string eventName, params Segment[] segments) {
            if (!log) return;
            segments = segments.Where(s => !s.IsNull).ToArray();
            AllFullTracked().ForEach(x => x.Event(eventName, segments));
        }

        public static void Event(string eventName, IEnumerable segmentCollection) {
            if (!log) return;
            
            var segments = segmentCollection
                .Collect<Segment>()
                .Where(s => !s.IsNull)
                .ToArray();
            
            AllFullTracked().ForEach(x => x.Event(eventName, segments));
        }
        
        public static AI Network<AI>() where AI : AnalyticIntegration {
            return AllActive().CastOne<AI>();
        }

        #endregion
    }
    
    public struct Segment {
        public readonly string ID;
        
        public object value;
        
        bool isNull;
        
        public bool IsNull => isNull;
        
        Segment(string ID, object value) {
            this.ID = ID;
            this.value = value;
            isNull = false;
        }
        
        public static Segment New(string ID, object value) {
            return value == null || ID.IsNullOrEmpty() ? Null() : new Segment(ID, value);
        }
        
        public static Segment New(string ID) => New(ID, string.Empty);

        public static Segment Null() {
            return new Segment(null, null) {
                isNull = true
            };
        }

        public override string ToString() {
            if (isNull) 
                return "null";
            return $"{ID}: {value}";
        }
    }
}


