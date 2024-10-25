using System.Collections.Generic;

namespace Yurowm {
    public static class Availability {
        public static IEnumerable<T> AvailableOnly<T>(this IEnumerable<T> source) {
            foreach (var element in source)
                if (element.CheckAvailability())
                    yield return element;
        }
        
        public static bool CheckAvailability(this object obj) {
            return obj is not IAvailability a || a.AvailabilityFilter();
        }
    }
    
    public interface IAvailability {
        public bool AvailabilityFilter();
    }
}