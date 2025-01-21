using System;

namespace Yurowm {
    public interface ITimeSource {
        bool HasTime {get;}
        bool ValidTime {get;}
        DateTime Now {get;}
        DateTime UTCNow {get;}
        TimeSpan Zone {get;}
        
        public bool HasValidTime() => HasTime && ValidTime;
    }
    
    public class SystemTimeSource : ITimeSource {
        public bool HasTime => true;
        public bool ValidTime => true;
        public DateTime Now => DateTime.Now;
        public DateTime UTCNow => DateTime.UtcNow;
        public TimeSpan Zone => DateTime.Now - DateTime.UtcNow;
    }
}