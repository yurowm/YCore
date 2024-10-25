using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Yurowm.Extensions {
    public static class IEnumerableExtensions {
        public static IEnumerable<IEnumerable<E>> Portions<E>(this IEnumerable<E> enumerable, int portionSize) {
            if (enumerable == null)
                throw new NullReferenceException(nameof(enumerable));
            
            if (portionSize <= 0)
                throw new Exception("Portion size can not be 0 or negative");
            
            var enumerator = enumerable.GetEnumerator();

            IEnumerable<E> Portion() {
                var count = 0;

                while (true) {
                    yield return enumerator.Current;
                    
                    count++;
                    
                    if (count >= portionSize || !enumerator.MoveNext())
                        yield break;
                }
            }

            while (enumerator.MoveNext())
                yield return Portion();
        }
        
        public static float Multiply(this IEnumerable<float> enumerable) {
            if (enumerable == null)
                throw new NullReferenceException(nameof(enumerable));
            
            var result = 1f;

            foreach (var value in enumerable) 
                result *= value;
            
            return result;
        }
        
        public static int Multiply(this IEnumerable<int> enumerable) {
            if (enumerable == null)
                throw new NullReferenceException(nameof(enumerable));
            
            var result = 1;

            foreach (var value in enumerable) 
                result *= value;
            
            return result;
        }
        
        public static void ApplyIndex<E>(this IEnumerable<E> enumerable, Action<int, E> apply) {
            if (enumerable == null)
                throw new NullReferenceException(nameof(enumerable));
            
            if (apply == null)
                throw new NullReferenceException(nameof(apply));
            
            var index = 0;
            foreach (var e in enumerable) 
                apply(index++, e);
        }
        
        public static bool CastOne<T>(this IEnumerable enumerable, out T value) {
            if (enumerable != null)
                foreach (var element in enumerable) {
                    if (element is T t) {
                        value = t;
                        return true;
                    }
                }
            value = default;
            return false;
        }

        public static T GetNextByLoop<T>(this IList<T> collection, T current) {
            var index = collection.IndexOf(current) + 1;

            if (index >= collection.Count)
                index = 0;
            
            return collection.Get(index);
        }

        public static int MaxSafe<T>(this IEnumerable<T> collection, Func<T, int> selector, int defaultValue) {
            if (collection.Any())
                return collection.Max(selector);

            return defaultValue;
        }

        public static int MinSafe<T>(this IEnumerable<T> collection, Func<T, int> selector, int defaultValue) {
            if (collection.Any())
                return collection.Min(selector);

            return defaultValue;
        }
        
        public static T GetClamp<T>(this ICollection<T> collection, int index) {
            if (collection == null)
                throw new NullReferenceException("Collection is null");
            
            var count = collection.Count;
            
            if (count == 0)
                return default;
            
            if (index < 0) 
                index = 0;
            else if (index > count - 1)
                index = count - 1;

            return collection.ElementAt(index);
        }

        
    }
}