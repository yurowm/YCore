using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Linq;
using System;
using System.Collections.Generic;
using Yurowm.Extensions;
using Yurowm.Profiling;
using Yurowm.Utilities;

namespace Yurowm.Controls {
    public class TouchControl {
        
        public Camera controlCamera;
    
        bool isMobilePlatform;
    
        Func<bool> isBegan;
        Func<bool> isEnded;
        Func<bool> isPress;
        Func<bool> isMulitouch;
        Func<Vector2> getWorldPoint;
        Func<Vector2> getPoint;
        Func<Vector2> getDeltaPoint;
        
        public Action<TouchStory> onTouchBegin = delegate { };
        public Action<TouchStory> onTouch = delegate { };
        public Action onNonTouch = delegate { };
        public Action<TouchStory> onTouchEnd = delegate { };
        public Action<TouchStory> onTouchClick = delegate { };

        public TouchControl() {
            isMobilePlatform = Application.isMobilePlatform;

            if (isMobilePlatform) {
                isBegan = () => Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
                isEnded = () => Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
                isPress = () => Input.touchCount > 0;
                isMulitouch = () => Input.touchCount >= 2;
                getPoint = () => Input.touchCount > 0 ? Input.GetTouch(0).position : Vector2.zero;
                getDeltaPoint = () => Input.touchCount > 0 ? Input.GetTouch(0).deltaPosition : Vector2.zero;
            } else {
                isBegan = () => Input.GetMouseButtonDown(0);
                isEnded = () => Input.GetMouseButtonUp(0);
                isPress = () => Input.GetMouseButton(0);
                isMulitouch = () => false;
                getPoint = () => Input.mousePosition;
                Vector2 lastPoint = Input.mousePosition;
                getDeltaPoint = () => {
                    if (isBegan()) lastPoint = Input.mousePosition;
                    var result = new Vector2(Input.mousePosition.x - lastPoint.x, Input.mousePosition.y - lastPoint.y);
                    lastPoint = Input.mousePosition;
                    return result;
                };
            }
            // getWorldPoint = () => controlCamera.ScreenPointToRay(getPoint()).origin;
            getWorldPoint = () => ScreenToWorldPosition(getPoint());
        }

        bool IsOverUI() {
            return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
        }

        bool IsOverUI(int pointerID) {
            return EventSystem.current && EventSystem.current.IsPointerOverGameObject(pointerID);
        }

        List<RaycastResult> list = new();

        bool IsOverUI(Vector2 screenPosition) {
            if (!EventSystem.current)
                return false;
            
            var data = new PointerEventData(EventSystem.current);
            data.position = screenPosition;

            list.Clear();
            EventSystem.current.RaycastAll(data, list);

            return !list.IsEmpty();
        }

        public IEnumerator ControlRoutine(ITouchControlProvider provider) {
            var touches = new List<TouchStory>();
            
            Func<bool> controlIsAvaliable = () =>
                provider != null && provider.IsAliveControls() && provider.IsControl()
                && Time.timeScale != 0 && controlCamera;

            while (provider != null && provider.IsAliveControls()) {
                yield return new WaitWithDelay(controlIsAvaliable, 0.2f);

                while (controlIsAvaliable()) {
                    using (YProfiler.Area("Controls")) {
                        if (Application.isMobilePlatform)
                            TouchControls(touches);
                        else {
                            DesktopControls(touches);
                            if (Input.mouseScrollDelta.y != 0) provider.ScrollControl(Input.mouseScrollDelta.y);
                            if (Input.GetKey(KeyCode.Minus)) provider.ScrollControl(-Time.deltaTime);
                            if (Input.GetKey(KeyCode.Equals)) provider.ScrollControl(Time.deltaTime);
                        }

                        if (touches.Count > 0)
                            provider.Control(touches);

                        #region Remove complete touches
                        if (touches.Count > 0) {
                            for (int i = 0; i < touches.Count; i++) {
                                if (touches[i].IsComplete) {
                                    touches.RemoveAt(i);
                                    i--;
                                }
                            }
                            if (touches.Count == 0) {
                                provider.OnReleaseControl();
                            }
                        }
                        #endregion
                    }
                    yield return null;
                }
            }
        }

        void DesktopControls(List<TouchStory> touches) {
            TouchStory touch;

            if (touches.Count == 0) {
                if (Input.GetMouseButtonDown(0) && !IsOverUI()) {
                    touch = new TouchStory();

                    touch.beginTime = Time.time;
                    touch.beginScreenPosition = Input.mousePosition;
                    touch.beginWorldPosition = ScreenToWorldPosition(touch.beginScreenPosition.Value);

                    touch.currentScreenPosition = touch.beginScreenPosition.Value;
                    touch.currentWorldPosition = touch.beginWorldPosition.Value;

                    touches.Add(touch);
                    
                    touch.IsOverUI = IsOverUI();
                    
                    onTouchBegin.Invoke(touch);
                } else {
                    onNonTouch.Invoke();
                    return;
                }
            } else
                touch = touches[0];

            Vector2 screenPosition = Input.mousePosition.To2D(Asix3D.XY);
            touch.deltaScreenPosition = screenPosition - touch.currentScreenPosition;
            touch.currentScreenPosition = screenPosition;

            Vector2 worldPosition = ScreenToWorldPosition(screenPosition);
            touch.deltaWorldPosition = worldPosition - touch.currentWorldPosition;
            touch.currentWorldPosition = worldPosition;
            
            touch.IsOverUI = IsOverUI();
            onTouch.Invoke(touch);
        
            if (Input.GetMouseButtonUp(0)) {
                touch.endTime = Time.time;
                touch.endScreenPosition = screenPosition;
                touch.endWorldPosition = worldPosition;
                touch.completeDeltaScreenPosition = touch.endScreenPosition - touch.beginScreenPosition;
                touch.completeDeltaWorldPosition = touch.endWorldPosition - touch.beginWorldPosition;
                touch.IsCanceled = !new Rect(0, 0, Screen.width, Screen.height).Contains(screenPosition);
                
                onTouchEnd.Invoke(touch);
                
                if (touch.DeltaTime < .3f && touch.deltaScreenPosition.MagnitudeIsLessThan(Mathf.Min(Screen.width, Screen.height) / 200))
                    onTouchClick.Invoke(touch);
            }
        }

        void TouchControls(List<TouchStory> touches) {
            if (Input.touchCount == 0) {
                onNonTouch.Invoke();
                return;
            }

            foreach (Touch unityTouch in Input.touches) {
                TouchStory touch;
                if (unityTouch.phase == TouchPhase.Began) {
                    if (IsOverUI(unityTouch.position))
                        continue;

                    touch = new TouchStory();
                    touch.fingerId = unityTouch.fingerId;
                    touch.beginTime = Time.time;

                    touch.beginScreenPosition = unityTouch.position;
                    touch.beginWorldPosition = ScreenToWorldPosition(touch.beginScreenPosition.Value);

                    touch.currentScreenPosition = touch.beginScreenPosition.Value;
                    touch.currentWorldPosition = touch.beginWorldPosition.Value;
                    
                    touch.IsOverUI = touch.IsOverUI || IsOverUI(unityTouch.position);
                    
                    touches.Add(touch);

                    onTouchBegin.Invoke(touch);
                } else
                    touch = touches.FirstOrDefault(t => t.fingerId == unityTouch.fingerId);

                if (touch != null) {
                    Vector2 screenPosition = unityTouch.position;
                    touch.deltaScreenPosition = screenPosition - touch.currentScreenPosition;
                    touch.currentScreenPosition = screenPosition;

                    Vector2 worldPosition = ScreenToWorldPosition(screenPosition);
                    touch.deltaWorldPosition = worldPosition - touch.currentWorldPosition;
                    touch.currentWorldPosition = worldPosition;

                    touch.IsOverUI = touch.IsOverUI || IsOverUI(screenPosition);
                    
                    onTouch.Invoke(touch);

                    if (unityTouch.phase == TouchPhase.Canceled || unityTouch.phase == TouchPhase.Ended) {
                        touch.endTime = Time.time;
                        touch.endScreenPosition = screenPosition;
                        touch.endWorldPosition = worldPosition;
                        touch.completeDeltaScreenPosition = touch.endScreenPosition - touch.beginScreenPosition;
                        touch.completeDeltaWorldPosition = touch.endWorldPosition - touch.beginWorldPosition;
                        touch.IsCanceled = unityTouch.phase == TouchPhase.Canceled;
                        
                        onTouchEnd.Invoke(touch);
                        
                        if (touch.DeltaTime < .3f && touch.deltaScreenPosition.MagnitudeIsLessThan(Mathf.Min(Screen.width, Screen.height) / 200))
                            onTouchClick.Invoke(touch);
                    }
                }
            }

            #if DEVELOPMENT_BUILD
            DebugTools.DebugPanel.Log("Touches", "Input", touches
                .Select(t => $"{t.fingerId}.{(t.IsOverUI ? "X" : "")} ({t.currentScreenPosition})").Join(";\n"));
            #endif
        }

        public Vector2 ScreenToWorldPosition(Vector2 screenPosition) {
            var point = controlCamera.ScreenToWorldPoint(screenPosition);
            if (controlCamera.transform.parent)
                point -= controlCamera.transform.parent.position;
            return point;
        }
    }

    public interface ITouchControlProvider {
        void Control(List<TouchStory> touches);
        void OnReleaseControl();
        bool IsAliveControls();
        bool IsControl();
        void ScrollControl(float scroll);
    }

    public class Clickables {
        HashSet<ICastable> all = new ();
        
        Transform transform;

        public void Set(ICastable item) {
            all.Add(item);
        }

        public void Remove(ICastable item) {
            all.Remove(item);
        }

        public void Vacuum() {
            all.RemoveWhere(c => c == null);
        }

        public int controlMode = 0;

        public void Click(Vector2 position) {
            position = GetPoint(position);
            
            var target = all.CastIfPossible<IClickable>()
                .Where(c => c.IsAvaliableForClick(controlMode) 
                                        && (c.position - position).MagnitudeIsLessThan(c.clickableRadius))
                .GetMin(c => Vector2.Distance(c.position, position));
            target?.OnClick(controlMode);
        }

        public C Cast<C>(Vector2 position, float threashold) where C : ICastable {
            return Find<C>(GetPoint(position), threashold / transform.localScale.x);
        }
        
        public C Find<C>(Vector2 point, float threashold) where C : ICastable {
            var target = all.Where(c => c is C && c.IsAvaliableForClick(controlMode) 
                                               && (c.position - point).MagnitudeIsLessThan(threashold))
                .GetMin(c => Vector2.Distance(c.position, point));
            return (C) target;
        }

        public void SetCoordinateSystem(Transform transform) {
            this.transform = transform;
        }

        public Vector2 GetPoint(Vector2 worldPoint) {
            if (transform) 
                return transform.InverseTransformPoint(worldPoint);
            
            return worldPoint;
        }

        public Vector2 GetWorldPoint(Vector2 point) {
            if (transform) 
                return transform.TransformPoint(point);
            
            return point;
        }
    }

    public interface ICastable {
        Vector2 position { get; }
        bool IsAvaliableForClick(int mode);
    }

    public interface IClickable : ICastable {
        float clickableRadius {get;}
        void OnClick(int mode);
    }

    public class TransformClickable : IClickable {
        public readonly Transform transform;
    
        public Func<int, bool> isAvaliableForClick = null;
        public Action<int> onClick = null;
    
        public TransformClickable(Transform transform, Action<int> onClick = null) {
            this.transform = transform;
            this.onClick = onClick;
        }

        public Vector2 position => transform.position;

        public float clickableRadius => 1;

        public void OnClick(int mode) {
            onClick?.Invoke(mode);
        }

        public bool IsAvaliableForClick(int mode) {
            return isAvaliableForClick?.Invoke(mode) ?? true;
        }
    }

    public class TouchStory {
        public int fingerId = 0;

        public float? beginTime;
        public float? endTime;
        
        public bool IsCanceled = false;
        public bool IsOverUI = false;
        
        public bool IsComplete => beginTime.HasValue && endTime.HasValue;

        public float DeltaTime => IsComplete ? endTime.Value - beginTime.Value : float.PositiveInfinity;
        public float TimeSinceStart => Time.time - beginTime.Value;

        public bool IsBegan => beginTime.HasValue && beginTime.Value == Time.time;
        
        public bool IsChanged => !deltaScreenPosition.IsEmpty();

        public Vector2? beginScreenPosition;
        public Vector2 currentScreenPosition;
        public Vector2? endScreenPosition;
        public Vector2 deltaScreenPosition;
        public Vector2? completeDeltaScreenPosition;

        public Vector2? beginWorldPosition;
        public Vector2 currentWorldPosition;
        public Vector2? endWorldPosition;
        public Vector2 deltaWorldPosition;
        public Vector2? completeDeltaWorldPosition;
    }
}