using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Yurowm.Extensions;
using UObject = UnityEngine.Object;

namespace Yurowm.Utilities {
    public static class UnityUtils {
        
        public static bool IsUnityThread() {
            try {
                bool _ = Application.isPlaying;
                return true;
            } catch {
                return false;
            }
        }

        public static bool MagnitudeIsGreaterThan(this Vector2 vector, float maxDistance) {
            if (maxDistance <= 0) return true;
            return !vector.MagnitudeIsLessThan(maxDistance);
        }
        
        public static bool MagnitudeIsGreaterThan(this Vector2 a, Vector2 b) {
            return !a.MagnitudeIsLessThan(b);
        }

        public static bool MagnitudeIsLessThan(this Vector2 vector, float maxDistance) {
            if (maxDistance < 0) return false;
            if (vector.x.Abs() > maxDistance || vector.y.Abs() > maxDistance)
                return false;
            if (vector.x * vector.x + vector.y * vector.y > maxDistance * maxDistance)
                return false;
            return true;
        }
        
        public static bool MagnitudeIsLessThan(this Vector2 a, Vector2 b) {
            if (a.x.Abs() > b.x.Abs() || a.y.Abs() > b.y.Abs())
                return false;
            if (a.x * a.x + a.y * a.y > b.x * b.x + b.y * b.y)
                return false;
            return true;
        }

        public static string ToText(this Enum e) {
            return Enum
                .GetValues(e.GetType())
                .Cast<Enum>()
                .Where(e.HasFlag)
                .Select(s => s.ToString())
                .Join("|");
        }
        
        public static bool IsNumericType(this Type type) {   
            switch (Type.GetTypeCode(type)) {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
        
        public static IEnumerable<T> ForEachValues<T>(params T[] values) {
            foreach (T value in values)
                yield return value;
        }

        public static IEnumerable<T> ForEachCollection<T>(params ICollection<T>[] values) {
            foreach (ICollection<T> collection in values)
                foreach (T value in collection)
                    yield return value;
        }
        
        public static void DebugRun(Action action) {
            action();
        }
        
        public static IEnumerator ExecuteAsync(Action action) {
            if (action == null) yield break;

            #if UNITY_WEBGL
            
            try {
                action.Invoke();
            } catch (Exception e) {
                Debug.LogException(e);
            }
            
            #else
            var complete = false;
            Exception exception = null;
            Action<object> task = o => {
                try {
                    action.Invoke();
                } catch (Exception e) {
                    exception = e;
                }
                complete = true;
            };
            
            ThreadPool.QueueUserWorkItem(task.Invoke);
            
            while (!complete) 
                yield return null;
            
            if (exception != null)
                Debug.LogException(exception);
            #endif
        }
        
        public static IEnumerator ExecuteAsync(this IEnumerable<Action> actions) {
            if (actions == null)
                yield break;
            var executions = actions.Select(ExecuteAsync).ToList();
            while (true) {
                var breaker = true;
                foreach (IEnumerator execution in executions) {
                    if (execution.MoveNext())
                        breaker = false;
                }
                if (breaker)
                    yield break;
                yield return null;
            }
        }

        static IEnumerable<Assembly> GetAllRelatedAssembies(Type contextType, bool wholeApplication) {
            var assembly = contextType.Assembly;
                
            if (wholeApplication) {
                var assemblyName = assembly.GetName().FullName;

                foreach (var a in AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a == assembly || a.GetReferencedAssemblies().Any(n => n.FullName.Equals(assemblyName)))) {
                    yield return a;
                }
            } else
                yield return assembly;
        }

        public static IEnumerable<KeyValuePair<MethodInfo, A>> GetAllMethodsWithAttribute<A>(BindingFlags flags = BindingFlags.Default) where A : Attribute {
            foreach (var assembly in GetAllRelatedAssembies(typeof(A), true)) {
                foreach (var type in assembly.GetTypes()) {
                    foreach (var method in type.GetMethods(flags)) {
                        var attribute = method.GetCustomAttribute<A>(true);
                        if (attribute != null)
                            yield return new KeyValuePair<MethodInfo, A>(method, attribute);
                    }
                }
            }
        }

        public static Type FindType(string name) {
            var result = Type.GetType(name);
            
            if (result != null)
                return result;
            
            result = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(name))
                .NotNull()
                .FirstOrDefault();
            
            return result;
        }

        public static RectTransform rect(this Transform transform) {
            return transform as RectTransform;
        }
        
        public static Rect Lerp(Rect a, Rect b, float time) {
            return new Rect(Vector2.Lerp(a.position, b.position, time),Vector2.Lerp(a.size, b.size, time));
        }
        
        public static Rect MoveTowards(Rect a, Rect b, float delta) {
            float sizeDelta = Vector2.Distance(a.size, b.size) * delta / Vector2.Distance(a.position, b.position);
            return new Rect(
                Vector2.MoveTowards(a.position, b.position, delta),
                Vector2.MoveTowards(a.size, b.size, sizeDelta));
        }

        public static DeviceType GetDeviceType() {
            if (!Application.isMobilePlatform)
                return DeviceType.Desktop;
            
            #if UNITY_IOS
            bool deviceIsIpad = UnityEngine.iOS.Device.generation.ToString().Contains("iPad");
            if (deviceIsIpad)
                return DeviceType.Tablet;

            bool deviceIsIphone = UnityEngine.iOS.Device.generation.ToString().Contains("iPhone");
            if (deviceIsIphone)
                return DeviceType.Phone;
            #endif
         
            float diagonal = Mathf.Sqrt(Mathf.Pow(Screen.width, 2) + Mathf.Pow(Screen.height, 2));
            float diagonalInches = diagonal / Screen.dpi;
            
            float aspectRatio = Mathf.Max(Screen.width, Screen.height) / Mathf.Min(Screen.width, Screen.height);
            
            bool isTablet = diagonalInches > 6.5f && aspectRatio < 2f;

            return isTablet ? DeviceType.Tablet : DeviceType.Phone;
        }
        
        static DelayedAccess frameAccess = new DelayedAccess(1f / 30);
        public static bool GetFrameAccess() {
            return frameAccess.GetAccess();
        }
    }
    
    public enum DeviceType {
        Phone,
        Tablet,
        Desktop
    }

    #region Custom Yield Instructions
    public class WaitWithDelay : CustomYieldInstruction {
        float? lastTrue;
        float delay;
        Func<bool> predicate;

        public WaitWithDelay(Func<bool> predicate, float delay) {
            lastTrue = null;
            this.delay = delay;
            this.predicate = predicate;
        }

        public override bool keepWaiting {
            get {
                if (predicate()) {
                    if (!lastTrue.HasValue)
                        lastTrue = Time.time;
                    if (lastTrue.Value + delay < Time.time)
                        return false;
                } else
                    lastTrue = null;
                return true;
            }
        }
    }
    #endregion

    #region Int2 and Area
    [System.Serializable]
    public struct int2 {
        public static readonly int2 zero = new int2(0, 0);
        public static readonly int2 right = new int2(1, 0);
        public static readonly int2 up = new int2(0, 1);
        public static readonly int2 left = new int2(-1, 0);
        public static readonly int2 down = new int2(0, -1);
        public static readonly int2 one = new int2(1, 1);
        public static readonly int2 Null = new int2(0, 0, true);

        [SerializeField]
        int _x;
        public int X {
            get {
                if (isNull) throw new NullReferenceException();
                return _x;
            }
            set {
                if (isNull) throw new NullReferenceException();
                _x = value;
            }
        }
        
        [SerializeField]
        int _y;
        public int Y {
            get {
                if (isNull) throw new NullReferenceException();
                return _y;
            }
            set {
                if (isNull) throw new NullReferenceException();
                _y = value;
            }
        }
        readonly bool isNull;
        
        int2(int x, int y, bool isNull) {
            this.isNull = isNull;
            this._x = x;
            this._y = y;
        }

        public int2(int x = 0, int y = 0) : this (x, y, false) {}

        public int2(int2 coord) : this (coord._x, coord._y, coord.isNull) {}

        public static bool operator ==(int2 a, int2 b) {
            if (a.Equals(Null))
                return b.Equals(Null);
            return a.Equals(b);
        }

        public static bool operator !=(int2 a, int2 b) {
            if (a.Equals(Null))
                return !b.Equals(Null);
            return !a.Equals(b);
        }

        public static int2 operator *(int2 a, int b) {
            return new int2(a.X * b, a.Y * b);
        }

        public static int2 operator *(int b, int2 a) {
            return a * b;
        }

        public static int2 operator +(int2 a, int2 b) {
            return new int2(a.X + b.X, a.Y + b.Y);
        }

        public static int2 operator -(int2 a, int2 b) {
            return new int2(a.X - b.X, a.Y - b.Y);
        }

        public static int2 operator +(int2 a, Side side) {
            return a + side.ToInt2();
        }

        public static int2 operator -(int2 a, Side side) {
            return a + side.ToInt2();
        }

        public bool IsItHit(int minX, int minY, int maxX, int maxY) {
            return X >= minX && X <= maxX && Y >= minY && Y <= maxY;
        }

        public int FourSideDistanceTo(int2 destination) {
            return (X - destination.X).Abs() + (Y - destination.Y).Abs();
        }
        
        public int FourSideDistance() {
            return X.Abs() + Y.Abs();
        }

        public int EightSideDistanceTo(int2 destination) {
            return YMath.Max(
                (X - destination.X).Abs(),
                (Y - destination.Y).Abs());
        }
        
        public int EightSideDistance() {
            return YMath.Max(X.Abs(), Y.Abs());
        }

        public int DistanceTo(int2 destination, DistanceType type) {
            switch (type) {
                case DistanceType.Four: return FourSideDistanceTo(destination);
                case DistanceType.Eight: return EightSideDistanceTo(destination);
            }
            return 0;
        }
        
        public enum DistanceType {
            Four = 0,
            Eight = 1
        }

        public override bool Equals(object obj) {
            if (!(obj is int2 coord))
                return false;
            return (isNull && coord.isNull) || (!isNull && !coord.isNull && X == coord.X && Y == coord.Y);
        }

        public override int GetHashCode() {
            if (isNull) return 0;
            
            int hash = 13;
            hash = (hash * 7) + _x;
            hash = (hash * 7) + _y;
            return hash;
        }

        public override string ToString() {
            return isNull ? nullText : $"({_x}, {_y})";
        }

        const string nullText = "null";
        static Regex parser = new Regex(@"\((?<x>-?\d+)\,\s*(?<y>-?\d+)\)");

        public static int2 Parse(string raw) {
            if (raw == nullText) return Null;
            Match match = parser.Match(raw);
            if (match.Success)
                return new int2(int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
            throw new FormatException("Can't to parse \"" + raw + "\" to int2 format. It must have next format: (int,int)");
        }

        public int2 XtoY() {
            return new int2(Y, X);
        }

        public static explicit operator Vector2(int2 coord) {
            return new Vector2(coord.X, coord.Y);
        }

        public Vector3 ToVector3(Asix3D plane, bool inverse = false) {
            switch (plane) {
                case Asix3D.XY: return inverse ? new Vector3(Y, X, 0) : new Vector3(X, Y, 0);
                case Asix3D.YZ: return inverse ? new Vector3(0, X, Y) : new Vector3(0, Y, X);
                case Asix3D.XZ: return inverse ? new Vector3(Y, 0, X) : new Vector3(X, 0, Y);
            }
            return Vector3.zero;
        }

        public readonly Vector2 ToVector2() {
            return new Vector2(X, Y);
        }

        #region Move

        public int2 Up(int s = 1) {
            if (isNull) return Null;
            return new int2(X, Y + s);
        }
        
        public int2 Down(int s = 1) {
            if (isNull) return Null;
            return new int2(X, Y - s);
        }
        
        public int2 Right(int s = 1) {
            if (isNull) return Null;
            return new int2(X + s, Y);
        }
        
        public int2 Left(int s = 1) {
            if (isNull) return Null;
            return new int2(X - s, Y);
        }
        
        public int2 MoveTo(int2 target) {
            if (target.isNull || isNull)
                return this;
            
            var delta = new int2 (
                (target.X - X).Sign(zero: true),
                (target.Y - Y).Sign(zero: true));
            
            return this + delta;
        }
        
        #endregion

        public Side ToSide() {
            var x = X.Sign(true);
            var y = Y.Sign(true);
            
            if (x == 0) {
                if (y == 0) return Side.Null; 
                if (y == 1) return Side.Top; 
                if (y == -1) return Side.Bottom; 
            }
            
            if (x == 1) {
                if (y == 0) return Side.Right; 
                if (y == 1) return Side.TopRight; 
                if (y == -1) return Side.BottomRight; 
            }
            
            if (x == -1) {
                if (y == 0) return Side.Left; 
                if (y == 1) return Side.TopLeft; 
                if (y == -1) return Side.BottomLeft; 
            }
            
            return Side.Null;
        }
    }

    [System.Serializable]
    public struct area {
        public static readonly area Null = new area(new int2(), new int2(), true);
        readonly bool isNull;

        public int left => position.X;

        public int down => position.Y;

        public int right => position.X + size.X - 1;

        public int up => position.Y + size.Y - 1;

        public int width => size.X;

        public int height => size.Y;

        public int2 position;
        public int2 size;

        area(int2 position, int2 size, bool isNull) {
            this.isNull = isNull || size == int2.Null || position == int2.Null;
            this.position = isNull ? int2.Null : position;
            this.size = isNull ? int2.Null : size;
        }
        
        public area(IEnumerable<int2> points) : this (int2.zero, int2.zero, false) {
            isNull = true;
         
            int left = int.MaxValue;
            int right = int.MinValue;
            int down = int.MaxValue;
            int top = int.MinValue;

            foreach (var point in points) {
                isNull = false;
                left = Mathf.Min(left, point.X);
                right = Mathf.Max(right, point.X);
                down = Mathf.Min(down, point.Y);
                top = Mathf.Max(top, point.Y);
            }
            
            if (!isNull) {
                position = new int2(left, down);
                size = new int2(right - left + 1, top - down + 1);
            }
        }
        
        public area(int2 position, int2 size) : this (position, size, false) {}

        public area(int2 position = new int2()) : this(position, int2.one) {}

        public static bool operator ==(area a, area b) {
            if (a.Equals(Null))
                return b.Equals(Null);
            return a.Equals(b);
        }

        public static bool operator !=(area a, area b) {
            if (a.Equals(Null))
                return !b.Equals(Null);
            return !a.Equals(b);
        }

        public bool Contains(int2 point) {
            return Contains(point.X, point.Y);
        }

        public bool Contains(int x, int y) {
            return left <= x &&
                right >= x &&
                down <= y &&
                up >= y;
        }

        public bool Contains(area subarea) {
            return left <= subarea.left &&
                right >= subarea.right &&
                down <= subarea.down &&
                up >= subarea.up;
        }

        public bool IsItIntersect(area subarea) {
            return (Mathf.Max(left, subarea.left) <= Mathf.Min(right, subarea.right)) &&
                (Mathf.Max(down, subarea.down) <= Mathf.Min(up, subarea.up));
        }

        public override bool Equals(object obj) {
            if (!(obj is area a))
                return false;
            return (isNull && a.isNull) || (!isNull && !a.isNull && position == a.position && size == a.size);
        }

        public override int GetHashCode() {
            if (isNull) return 0;
            int hash = 13;
            hash = (hash * 7) + position.GetHashCode();
            hash = (hash * 7) + size.GetHashCode();
            return hash;
        }

        const string nullText = "null";
        public override string ToString() {
            if (isNull)
                return nullText;
            else
                return "position:" + position.ToString() + " size:" + size.ToString();
            
            return isNull ? nullText : $"position:";
        }

        public area GetClone() {
            return (area) MemberwiseClone();
        }

        public IEnumerable<int2> GetPoints() {
            for (int x = left; x <= right; x++)
                for (int y = down; y <= up; y++)
                    yield return new int2(x, y);
        }
    }
    #endregion

    [Serializable]
    public struct RectOffsetFloat {
        public static readonly RectOffsetFloat Zero = new();

        [SerializeField]
        float m_Left;
        [SerializeField]
        float m_Right;
        [SerializeField]
        float m_Top;
        [SerializeField]
        float m_Bottom;
        
        public float Left {
            get => m_Left;
            set => m_Left = value;
        }

        public float Right {
            get => m_Right;
            set => m_Right = value;
        }
        
        public float Top {
            get => m_Top;
            set => m_Top = value;
        }
        
        public float Bottom {
            get => m_Bottom;
            set => m_Bottom = value;
        }
        
        public RectOffsetFloat(float left, float right, float top, float bottom) {
            m_Left = left;
            m_Right = right;
            m_Top = top;
            m_Bottom = bottom;
        }
        
        public float Vertical => Top + Bottom;
        public float Horizontal => Left + Right;

        public Rect Add(Rect rect) {
            rect.xMin += Left;
            rect.yMin += Bottom;
            rect.xMax -= Right;
            rect.yMax -= Top;
            return rect;
        }

        public static RectOffsetFloat Delta(Rect rectA, Rect rectB) {
            return new RectOffsetFloat {
                m_Left = rectB.xMin - rectA.xMin,
                m_Right = rectA.xMax - rectB.xMax,
                m_Bottom = rectB.yMin - rectA.yMin,
                m_Top = rectA.yMax - rectB.yMax,
            };
        }

        public override string ToString() {
            return $@"(Left:{Left}, Right:{Right}, Top:{Top}, Bottom:{Bottom})";
        }
    }

    #region Bounds Detectors
    
    public struct BoundDetector {
        float min;
        float max;
        bool initialized;
        
        public void Clear() {
            min = float.MaxValue;
            max = float.MinValue;
            initialized = true;
        }
        
        public void Set(float point) {
            if (!initialized) Clear();

            if (min > point) min = point; 
            if (max < point) max = point; 
        }
        
        public FloatRange GetBound() {
            return new FloatRange(min, max);
        }
    }
    
    public struct BoundDetector2D {
        Vector2 min;
        Vector2 max;
        bool initialized;

        public void Clear() {
            min = new Vector2(float.MaxValue, float.MaxValue);
            max = new Vector2(float.MinValue, float.MinValue);
            initialized = true;
        }
        
        public void Set(Vector2 point) {
            if (!initialized) Clear();
            
            if (min.x > point.x) min.x = point.x; 
            if (min.y > point.y) min.y = point.y; 
            if (max.x < point.x) max.x = point.x; 
            if (max.y < point.y) max.y = point.y; 
        }
        
        public void Set(Rect rect) {
            if (!initialized) Clear();
            
            if (min.x > rect.xMin) min.x = rect.xMin; 
            if (min.y > rect.yMin) min.y = rect.yMin; 
            if (max.x < rect.xMax) max.x = rect.xMax; 
            if (max.y < rect.yMax) max.y = rect.yMax; 
        }
        
        public Rect GetBound() {
            return new Rect(min, max - min);
        }
        
        public static Rect GetBound(IEnumerable<Vector2> points) {
            var detector = new BoundDetector2D();
            foreach (var point in points) 
                detector.Set(point);
            return detector.GetBound();
        }
    }

    public struct IntBoundDetector2D {
        int2 min;
        int2 max;
        bool initialized;

        public void Clear() {
            min = new int2(int.MaxValue, int.MaxValue);
            max = new int2(int.MinValue, int.MinValue);
            initialized = true;
        }
        
        public void Set(int2 point) {
            if (!initialized) Clear();
            
            if (min.X > point.X) min.X = point.X; 
            if (min.Y > point.Y) min.Y = point.Y; 
            if (max.X < point.X) max.X = point.X; 
            if (max.Y < point.Y) max.Y = point.Y; 
        }
        
        public void Set(area rect) {
            if (!initialized) Clear();
            
            if (min.X > rect.left) min.X = rect.left; 
            if (min.Y > rect.down) min.Y = rect.down; 
            if (max.X < rect.right) max.X = rect.right; 
            if (max.Y < rect.up) max.Y = rect.up; 
        }
        
        public area GetBound() {
            if (initialized)
                return new area(min, max - min + int2.one);
            else
                return new area(int2.zero, int2.zero);
        }
        
        public static area GetBound(IEnumerable<int2> points) {
            var detector = new IntBoundDetector2D();
            foreach (var point in points) 
                detector.Set(point);
            return detector.GetBound();
        }
    }
    
    #endregion
    
    #region Events
    public class Event<T> : UnityEvent<T> {}
    public class Event<T1, T2> : UnityEvent<T1, T2> {}
    public class Event<T1, T2, T3> : UnityEvent<T1, T2, T3> {}
    public class Event<T1, T2, T3, T4> : UnityEvent<T1, T2, T3, T4> {}
    #endregion

    public static class Comparing {
        
        [Flags]
        public enum Operator {
            Less = 1 << 0,
            Equal = 1 << 1,
            Greater = 1 << 2
        }
        
        public static bool Compare(int value, int constant, Operator o) {
            if (value > constant)
                return o.HasFlag(Operator.Greater);
            
            if (value < constant)
                return o.HasFlag(Operator.Less);
            
            return o.HasFlag(Operator.Equal);
        }
    }
    
    public struct DisposeAction: IDisposable {
        Action action;
        
        public DisposeAction(Action action) {
            this.action = action;
        }

        public void Dispose() {
            action?.Invoke();
            action = null;
        }
    }
    
    [Serializable]
    public class SortingLayerAndOrder {
        public int layerID = 0;
        public int order = 0;
        public SortingLayerAndOrder(int layerID, int order) {
            this.layerID = layerID;
            this.order = order;
        }
        
        public static SortingLayerAndOrder Get(SortingGroup sortingGroup) {
            return new SortingLayerAndOrder(sortingGroup.sortingLayerID, sortingGroup.sortingOrder);
        }
        
        public static SortingLayerAndOrder Get(Renderer renderer) {
            return new SortingLayerAndOrder(renderer.sortingLayerID, renderer.sortingOrder);
        }
        
        public void Apply(SortingGroup sortingGroup) {
            sortingGroup.sortingLayerID = layerID;
            sortingGroup.sortingOrder = order;
        }
        
        public void Apply(Renderer renderer) {
            renderer.sortingLayerID = layerID;
            renderer.sortingOrder = order;
        }
    }

    public abstract class MonoBehaviourAssistant<T> : MonoBehaviour where T : MonoBehaviour {
        public static T Instance {get; private set;}

        public MonoBehaviourAssistant() : base() {
            Instance = this as T;
        }
    }

    public abstract class SingletonScriptableObject<T> : ScriptableObject where T : SingletonScriptableObject<T> {
        static T _instance = null;
        public static T Instance {
            get {
                if (_instance == null) {
                    Type type = typeof(T);
                    var o = Resources.Load<T>(type.FullName);
                    if (o == null) {
                        _instance = type.Emit<T>();
                        Debug.LogError(type.Name + " resource is not found. The new empty temporary instance was created!");
                    } else
                        _instance = o;
                }
                if (!_instance.IsInitialized && (Utils.IsMainThread() || !Application.isPlaying ))
                    _instance.Initialize();
                return _instance;
            }
        }

        [NonSerialized]
        bool initialized = false;
        public bool IsInitialized => initialized;

        public virtual void Initialize() {
            initialized = true;
        }
    }

    public abstract class SingletonObject<T> where T : SingletonObject<T> {
        static T _instance = null;
        public static T Instance {
            get {
                if (_instance == null)
                    _instance = Activator.CreateInstance<T>();
                if (!_instance.IsInitialized() && Utils.IsMainThread() && Application.isPlaying)
                    _instance.Initialize();
                return _instance;
            }
        }

        public bool IsInitialized() {
            return isInitialized;
        }

        bool isInitialized = false;
        public virtual void Initialize() {
            isInitialized = true;
        }
    }
    
    public class History<T> {
        List<T> memory = new List<T>();
        int index = -1;
        
        readonly int size;
        
        public bool IsEmpty => memory.IsEmpty();
        public bool HasNext => index < memory.Count - 1;
        public bool HasPrevious => index > 0;
        
        public History(int size) {
            this.size = size.ClampMin(2);
        }
        
        public T Current {
            get {
                if (index < 0)
                    return default;
                    
                return memory[index];
            }
        }
        
        public void Next(T element) {
            if (Equals(Current, element))
                return;
            
            while (HasNext) 
                memory.RemoveAt(memory.Count - 1);

            memory.Add(element);

            while (memory.Count > size)
                memory.RemoveAt(0);
            
            index = memory.Count - 1;
        }
        
        public T Back() {
            if (!HasPrevious) 
                return default;
            index --;
            return memory[index];
        }
        
        public void Clear() {
            memory.Clear();
            index = -1;
        }
        
        public T Forward() {
            if (!HasNext) 
                return default;
            index ++;
            return memory[index];
        }

        public IEnumerable<T> GetAll() {
            foreach (var element in memory) {
                yield return element;
            }
        }

        public T JumpTo(T element) {
            var i = memory.IndexOf(element);
            if (i < 0)
                return default;
            
            index = i;
            return element;
        }
        
        public T JumpTo(Predicate<T> predicate) {
            var i = memory.FindIndex(predicate);
            if (i < 0)
                return default;
            
            index = i;
            return memory[index];
        }
        
        public T JumpToIndex(int index) {
            if (index < 0 || index >= memory.Count - 1)
                return default;
            this.index = index;
            return memory[index];
        }
    }
}