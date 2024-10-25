using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm.Extensions {
    public static class ExtensionsUnityEngine {
        #region Vector Extensions        
        public static Vector2 To2D(this Vector3 original, Asix3D plane = Asix3D.XY, bool inverse = false) {
            switch (plane) {
                case Asix3D.XY:
                    return inverse ? new Vector2(original.y, original.x) : new Vector2(original.x, original.y);
                case Asix3D.YZ:
                    return inverse ? new Vector2(original.z, original.y) : new Vector2(original.y, original.z);
                case Asix3D.XZ:
                    return inverse ? new Vector2(original.z, original.x) : new Vector2(original.x, original.z);
            }
            return Vector2.zero;
        }

        public static Vector3 To3D(this Vector2 original, float z = 0, Asix3D plane = Asix3D.XY, bool inverse = false) {
            switch (plane) {
                case Asix3D.XY:
                    return inverse ? new Vector3(original.y, original.x, z) : new Vector3(original.x, original.y, z);
                case Asix3D.YZ:
                    return inverse ? new Vector3(z, original.y, original.x) : new Vector3(z, original.x, original.y);
                case Asix3D.XZ:
                    return inverse ? new Vector3(original.y, z, original.x) : new Vector3(original.x, z, original.y);
            }
            return Vector3.zero;
        }

        public static Vector3 Scale(this Vector3 original, float x = 1f, float y = 1f, float z = 1f) {
            return new Vector3(original.x * x, original.y * y, original.z * z);
        }     
        
        public static Vector2 Scale(this Vector2 original, float x = 1f, float y = 1f) {
            return new Vector2(original.x * x, original.y * y);
        }

        public static Vector3 Multiply(this Vector3 original, Vector3 scale) {
            return new Vector3(original.x * scale.x, original.y * scale.y, original.z * scale.z);
        }

        public static Vector3 Divide(this Vector3 original, Vector3 scale) {
            return new Vector3(original.x / scale.x, original.y / scale.y, original.z / scale.z);
        }

        public static Vector2 WithMagnitude(this Vector2 original, float newMagnitude) {
            return original.FastNormalized() * newMagnitude;
        }

        public static Vector2 FastNormalized(this Vector2 original) {
            if (original.IsEmpty()) return default;
            
            float magnitute = FastMagnitude(original);
            
            original.x /= magnitute;
            original.y /= magnitute;
            
            return original;
        }

        public static Vector3 FastNormalized(this Vector3 original) {
            float magnitute = FastMagnitude(original);
            
            original.x /= magnitute;
            original.y /= magnitute;
            original.z /= magnitute;
            
            return original;
        }

        public static float FastMagnitude(this Vector2 original) {
            float magnitute = original.x * original.x + 
                              original.y * original.y;
            
            magnitute = magnitute.Sqrt();
            
            return magnitute;
        }

        public static float FastMagnitude(this Vector3 original) {
            float magnitute = original.x * original.x + 
                              original.y * original.y + 
                              original.z * original.z;
            
            magnitute = magnitute.Sqrt();
            
            return magnitute;
        }

        public static Vector2 Rotate(this Vector2 v, float degress) {
            if (degress == 0) return v;
            float radians = YMath.DegToRad(degress);
            return new Vector2(
                v.x * YMath.CosRad(radians) - v.y * YMath.SinRad(radians),
                v.x * YMath.SinRad(radians) + v.y * YMath.CosRad(radians));
        }

        public static float Angle(this Vector2 vector, bool negative = true) {
            float angle = Vector2.Angle(Vector2.right, vector);
            if (negative && vector.y < 0)
                angle = 360f - angle;
            return angle;
        }

        public static Vector2 Perpendicular(this Vector2 vector, bool cw = true) {
            float x = vector.x;
            
            vector.x = vector.y;
            vector.y = x;
                
            if (cw) 
                vector.y *= -1;
            else
                vector.x *= -1;
            
            return vector;
        }

        public static Vector2 Perpendicular(this Vector2 vector, Vector2 up) {
            vector = vector.Perpendicular();
            
            if (vector.Dot(up) < 0) {
                vector.x *= -1;
                vector.y *= -1;
            }
            
            return vector;
        }

        public static float Dot(this Vector2 lhs, Vector2 rhs) {
            return lhs.x * rhs.x + lhs.y * rhs.y;
        }
        
        public static bool IsEmpty(this Vector2 v) {
            return v.x == 0 && v.y == 0;
        }

        public static bool IsEmpty(this Vector3 v) {
            return v.x == 0 && v.y == 0 && v.z == 0;
        }
        
        public static Vector2 ChangeMagnitude(this Vector2 original, Func<float, float> perform) {
            var magnitude = original.FastMagnitude();
            magnitude = perform(magnitude);
            return original.WithMagnitude(magnitude);
        }
        
        public static Vector2 ClampMagnitude(this Vector2 original, float min = 0, float max = float.MaxValue) {
            if (max < min) 
                return original.ClampMagnitude(max, min);
            
            if (min > 0 && original.MagnitudeIsLessThan(min))
                return original.WithMagnitude(min);
            
            if (max < float.MaxValue && original.MagnitudeIsGreaterThan(max))
                return original.WithMagnitude(max);
            
            return original;
        }
        
        public static Vector2 ClampMagnitude(this Vector2 original, FloatRange range) {
            return original.WithMagnitude(range.Clamp(original.FastMagnitude()));
        }
        
        public static Vector2 ChangeX(this Vector2 original, float x) {
            original.x = x;
            return original;
        }
        
        public static Vector2 ChangeY(this Vector2 original, float y) {
            original.y = y;
            return original;
        }
        
        public static Vector3 ChangeX(this Vector3 original, float x) {
            original.x = x;
            return original;
        }
        
        public static Vector3 ChangeY(this Vector3 original, float y) {
            original.y = y;
            return original;
        }
        
        public static Vector3 ChangeZ(this Vector3 original, float z) {
            original.z = z;
            return original;
        }
        
        #endregion

        #region Transform Extensions
        public static void DestroyChilds(this Transform transform, bool immediate = false) {
            for (int i = 0; i < transform.childCount; i++)
                if (immediate)
                    Object.DestroyImmediate(transform.GetChild(i).gameObject);
                else
                    Object.Destroy(transform.GetChild(i).gameObject);
        }

        public static IEnumerable<Transform> AllChild(this Transform transform, bool deep = true) {
            for (int i = 0; i < transform.childCount; i++) {
                yield return transform.GetChild(i);
                if (deep)
                    foreach (Transform child in transform.GetChild(i).AllChild(true))
                        yield return child;
            }
        }

        public static Transform GetChildByPath(this Transform transform, string path) {
            Transform result = transform;
            foreach (string name in path.Split('\\', '/')) {
                if (result == null)
                    return null;
                if (name.IsNullOrEmpty())
                    continue;
                result = result.AllChild(false).FirstOrDefault(c => c.name == name);
            }
            if (result == transform)
                result = null;
            return result;
        }

        public static Transform GetChildByName(this Transform transform, string name) {
            Transform result;
            foreach (Transform child in transform) {
                if (child.name == name) return child;
                result = child.GetChildByName(name);
                if (result) return result;
            }
            return null;
        }

        public static IEnumerable<Transform> AndAllChild(this Transform transform, bool deep = true) {
            yield return transform;
            foreach (Transform child in transform.AllChild(deep))
                yield return child;
        }

        public static void Reset(this Transform transform) {
            if (transform) {
                transform.localRotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;
            }
        }

        public static void SetRelPosition(this Transform transform, Vector2 relPosition) {
            transform.SetRelPosition(relPosition.x, relPosition.y);
        }

        public static void SetRelPosition(this Transform transform, float relX, float relY) {
            if (transform.parent is RectTransform) {
                var rect = (transform.parent as RectTransform).rect;
                transform.localPosition = new Vector3(rect.xMin + rect.width * relX,
                    rect.yMin + rect.height * relY, 0);
            }
        }
        #endregion

        #region Rect Transform Extensions

        static Vector3[] corners = new Vector3[4];
        /// <summary>
        /// Возвращает размер элемента в соответствием с его реальными мировыми координатами
        /// </summary>
        public static Rect GetWorldRect(this RectTransform rectTransform) {
            rectTransform.GetWorldCorners(corners);
            rectTransform.GetWorldCorners(corners);

            return new Rect(corners[0].To2D(), (corners[2] - corners[0]).To2D());
        }
        
        /// <summary>
        /// Возвращает размер элемента в относительных координатах (т.е. от 0.0 до 1.0)
        /// </summary>
        public static Rect GetScreenRect(this RectTransform rectTransform) {
            Rect worldRect = GetWorldRect(rectTransform);

            var corners = new[] {
                new Vector2(worldRect.xMin, worldRect.yMin),
                new Vector2(worldRect.xMax, worldRect.yMax)
            };
            
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            
            if (!canvas) return default;
                
            var canvasRect = canvas.transform.rect();
            var pivot = canvasRect.pivot;
            for (int i = 0; i < corners.Length; i++) {
                var point = corners[i];
                point = canvas.transform.worldToLocalMatrix.MultiplyPoint(point);
                point /= canvasRect.rect.size;
                point += pivot;
                corners[i] = point;
            }

            return new Rect(corners[0], corners[1] - corners[0]);
        }   
        
        /// <summary>
        /// Возвращает мировые размеры элемента с учетом его проекции на камеру
        /// </summary>
        public static Rect GetCameraRect(this RectTransform rectTransform, Camera camera) {
            Vector2 scale = camera.orthographicSize * 2f * new Vector2(camera.aspect, 1f);
            
            var result = GetScreenRect(rectTransform);

            result.size *= scale;
            
            result.position = camera.transform.position.To2D()
                              + (result.position - Vector2.one / 2) * scale;
            
            return result;
        }
        
        /// <summary>
        /// Возвращает размер, который элемент занимает на экране, в относительных координатах (т.е. от 0.0 до 1.0)
        /// </summary>
        public static Rect GetViewportRect(this RectTransform rectTransform, Camera camera) {
            var result = rectTransform.GetWorldRect();
        
            var minPoint = result.min;
            var maxPoint = result.max;
            
            minPoint = camera.WorldToViewportPoint(minPoint);
            maxPoint = camera.WorldToViewportPoint(maxPoint);

            result.position = minPoint;
            result.size = maxPoint - minPoint;
            
            return result;
        }
        
        public static Rect GetRootRect(this RectTransform rectTransform) {
            if (!rectTransform) 
                throw new NullReferenceException(nameof(rectTransform));
            
            if (!rectTransform.SetupParentComponent(out Canvas canvas))
                return default;
            
            if (!canvas.SetupParentComponent(out RectTransform canvasRT))
                return default;
            
            var rect = rectTransform.GetWorldRect();
            var canvasRect = canvasRT.GetWorldRect();
            
            var result = rect;
            result.x = Mathf.InverseLerp(canvasRect.xMin, canvasRect.xMax, rect.x);
            result.y = Mathf.InverseLerp(canvasRect.yMin, canvasRect.yMax, rect.y);
            result.xMax = Mathf.InverseLerp(canvasRect.xMin, canvasRect.xMax, rect.xMax);
            result.yMax = Mathf.InverseLerp(canvasRect.yMin, canvasRect.yMax, rect.yMax);
            
            return result;
        }
        
        public static void SetRootRect(this RectTransform rectTransform, Rect rootRect) {
            if (!rectTransform) 
                throw new NullReferenceException(nameof(rectTransform));
            
            if (!rectTransform.SetupParentComponent(out Canvas canvas))
                return;
            
            if (!canvas.SetupParentComponent(out RectTransform root))
                return;
            
            var canvasRect = root.GetWorldRect();
            
            var rect = rootRect;
            rect.x = Mathf.Lerp(canvasRect.xMin, canvasRect.xMax, rootRect.x);
            rect.y = Mathf.Lerp(canvasRect.yMin, canvasRect.yMax, rootRect.y);
            rect.xMax = Mathf.Lerp(canvasRect.xMin, canvasRect.xMax, rootRect.xMax);
            rect.yMax = Mathf.Lerp(canvasRect.yMin, canvasRect.yMax, rootRect.yMax);
            
            rectTransform.pivot = Vector2.zero;
            rectTransform.position = rect.position.To3D(rectTransform.position.z);            
            rectTransform.anchorMin = new Vector2(.5f, .5f);           
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.sizeDelta = root.rect.size * rootRect.size;
        }
        
        
        public static void Maximize(this RectTransform rectTransform) {
            rectTransform.Reset();
            
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        
        public static void ApplyValues(this RectTransform rectTransform, RectTransform source) {
            if (!rectTransform || !source)
                return;
            
            rectTransform.anchorMin = source.anchorMin;
            rectTransform.anchorMax = source.anchorMax;
            rectTransform.offsetMin = source.offsetMin;
            rectTransform.offsetMax = source.offsetMax;
            rectTransform.pivot = source.pivot;
            rectTransform.localScale = source.localScale;
            rectTransform.localRotation = source.localRotation;
        }
        
        
        public static void RebuildLayout(this RectTransform rectTransform) {
            if (!rectTransform) return;
            
            foreach (Transform child in rectTransform) {
                if (child is not RectTransform rtChild)
                    continue;
                
                LayoutRebuilder.ForceRebuildLayoutImmediate(rtChild);
                rtChild.RebuildLayout();
            }
        }
        
        #endregion
        
        #region Component Extensions

        public static void Destroy(this Object o) {
            if (o == null) return;

            switch (o) {
                case Transform t: Object.Destroy(t.gameObject); return;
                default: Object.Destroy(o); break;
            }
        }
        
        public static bool SetupComponent<C>(this Component anyComponent, out C component) where C : class {
            if (anyComponent == null) {
                component = null;
                return false;
            }
            return SetupComponent(anyComponent?.gameObject, out component);
        }
        
        public static bool SetupComponent<C>(this GameObject gameObject, out C component) where C : class {
            if (gameObject == null) {
                component = null;
                return false;
            }
            return gameObject.TryGetComponent(out component);
        }
        
        public static bool SetupChildComponent<C>(this Component anyComponent, out C component) where C : Component {
            return SetupChildComponent(anyComponent?.gameObject, out component);
        }
        
        public static bool SetupChildComponent<C>(this GameObject gameObject, out C component) where C : Component {
            if (gameObject == null) {
                component = null;
                return false;
            }
            component = gameObject.GetComponentInChildren<C>(true);
            return component != null;
        }
        
        public static bool SetupParentComponent<C>(this Component anyComponent, out C component) where C : Component {
            return SetupParentComponent(anyComponent?.gameObject, out component);
        }
        
        public static bool SetupParentComponent<C>(this GameObject gameObject, out C component) where C : Component {
            if (gameObject == null) {
                component = null;
                return false;
            }
            component = gameObject.GetComponentInParent<C>(true);
            return component != null;
        }
        
        public static C GetOrAddComponent<C>(this Component component) where C : Component {
            if (component == null) return null;
            return GetOrAddComponent<C>(component.gameObject);
        }
        
        public static C GetOrAddComponent<C>(this GameObject gameObject) where C : Component {
            if (gameObject == null) return null;
            
            C result = gameObject.GetComponent<C>();
            if (result != null) return result;
            
            return gameObject.AddComponent<C>();
        }
        
        public static C Clone<C>(this C component) where C : Component {
            if (!component)
                return null;
            
            return component.gameObject.Clone().GetComponent<C>();
        }
        
        public static GameObject Clone(this GameObject gameObject) {
            if (!gameObject)
                return null;
            
            var result = Object.Instantiate(gameObject);
            result.name = gameObject.name;
            result.transform.SetParent(gameObject.transform.parent);
            
            return result;
        }

        #endregion
        
        #region Animation Extensions
        public static void Break(this Animation animation) {
            string clip = animation.clip.name;
            animation.enabled = false;
            animation.Play(clip);
            animation[clip].enabled = false;
        }

        public static void Sample(this Animation animation, float time) {
            string clip = animation.clip.name;
            animation[clip].enabled = true;
            animation[clip].time = time * animation[clip].length;
            animation.Sample();
            animation[clip].enabled = false;
        }
        #endregion

        #region Rect
        public static Rect Resize(this Rect rect, Vector2 size) {
            return Resize(rect, size.x, size.y);
        }
        
        public static Rect Resize(this Rect rect, float width, float height) {
            rect.x -= (width - rect.width) / 2;
            rect.y -= (height - rect.height) / 2;
            rect.width = width;
            rect.height = height;
            return rect;
        }
        
        public static Rect Resize(this Rect rect, float size) {
            return Resize(rect, size, size);
        }
        
        public static Rect GrowSize(this Rect rect, Vector2 size) {
            return GrowSize(rect, size.x, size.y);
        }
        
        public static Rect GrowSize(this Rect rect, float deltaWidth, float deltaHeight) {
            rect.x -= deltaWidth / 2;
            rect.y -= deltaHeight / 2;
            rect.width += deltaWidth;
            rect.height += deltaHeight;
            return rect;
        }
        
        public static Rect GrowSize(this Rect rect, float offset) {
            return GrowSize(rect, offset, offset);
        }
        
        public static Rect Lerp(Rect a, Rect b, float time) {
            Rect result = default;
            result.position = Vector2.Lerp(a.position, b.position, time);
            result.size = Vector2.Lerp(a.size, b.size, time);
            return result;
        }
        
        public static Vector2 LowerLeft(this Rect rect) => new Vector2(rect.xMin, rect.yMin);
        
        public static Vector2 LowerRight(this Rect rect) => new Vector2(rect.xMax, rect.yMin);
        
        public static Vector2 UpperRight(this Rect rect) => new Vector2(rect.xMax, rect.yMax);
        
        public static Vector2 UpperLeft(this Rect rect) => new Vector2(rect.xMin, rect.yMax);

        #endregion

        #region Actions

        public static void SetSingleListner(this UnityEvent unityEvent, Action call) {
            if (unityEvent == null)
                throw new NullReferenceException(nameof(unityEvent));
            
            unityEvent.RemoveAllListeners();
            if (call != null)
                unityEvent.AddListener(call.Invoke);
        }

        #endregion

        #region Profiling
        
        public static IDisposable ProfileSample(string name) {
            if (name.IsNullOrEmpty())
                return null;
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            return new ProfileSampleS(name);
            #else
            return null;
            #endif
        } 

        struct ProfileSampleS: IDisposable {
            public ProfileSampleS(string name) {
                Profiler.BeginSample(name);
            }

            public void Dispose() {
                Profiler.EndSample();
            }
        }

        #endregion
        
        #region Coroutines
        
        public static IEnumerator WaitParticles(this GameObject gameObject, bool waitLoopsToo = true) {
            if (!gameObject) yield break;
            
            var particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();

            while (particleSystems
                   .Where(ps => ps != null && (waitLoopsToo || !ps.main.loop))
                   .Any(ps => ps.particleCount > 0))
                yield return null;
        }
        
        #endregion
        
        public static bool AddIfNew<T>(this List<T> list, T element) {
            if (list.Contains(element))
                return false;
            list.Add(element);
            return true;
        }
        
        public static void Resize<T>(this List<T> list, int size) {
            size = size.ClampMin(0);
            
            if (list.Count == size)
                return;

            if (list.Count > size) 
                list.RemoveRange(size, list.Count - size);
            
            if (list.Count < size) {
                var last = list.LastOrDefault();
                list.AddRange(Enumerator.For(list.Count, size - 1, 1).Select(_ => last));
            }
        }
        
        public static string ToSpaceString(this int numder) {
            return numder
                .ToString("#,#")
                .Replace(',' , ' ');
        }
        
        public static bool IsNan(this float v) {
            return float.IsNaN(v) || float.IsNaN(v);
        }
        
        public static bool IsNan(this Vector2 v) {
            return float.IsNaN(v.x) || float.IsNaN(v.y);
        }
        
        public static bool IsNan(this Vector3 v) {
            return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z);
        }
    }
}