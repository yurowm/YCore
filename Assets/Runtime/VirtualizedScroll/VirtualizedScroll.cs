using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Direction = UnityEngine.UI.Slider.Direction;

namespace Yurowm.UI {
    public class VirtualizedScroll : Behaviour, IInitializePotentialDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler, IPointerDownHandler, IUIRefresh {
        
        public float friction = 1f;
        public float maxSpeed = 300f;
        public float clampOffset = 100f;
        public float edgeBouncing = 30f;
        protected virtual bool buildOnActive => isActiveAndEnabled;
        
        public string lockUIKey;
        
        IPrefabProvider _prefabProvider;
        
        public IPrefabProvider prefabProvider {
            get {
                if (_prefabProvider == null)
                    _prefabProvider = new AssetManagerPrefabProvider();
                return _prefabProvider;
            }
            set => _prefabProvider = value;
        }
        
        [Flags]
        public enum Options {
            AllowUserToScroll = 1 << 0,
            BreakPositionOnEnable = 1 << 1
        }
        
        public Direction orientation = Direction.TopToBottom;
        
        public Options options = Options.AllowUserToScroll;
        
        public float spacing = 10;
        public RectOffset padding;

        float position = 0;
        
        protected float totalSize = 0;
        
        public List<ItemInfo> infos = new List<ItemInfo>();
        
        bool clamping => friction <= 0;

        void OnValidate() {
            RefreshDimensions();
        }
        
        void OnRectTransformDimensionsChange() {
            Build();
        }
        
        protected virtual void OnEnable() {
            if (options.HasFlag(Options.BreakPositionOnEnable)) {
                position = 0;
                velocity = 0;
            }
            RefreshDimensions();
        }

        protected void RefreshDimensions() {
            if (!infos.IsEmpty()) {
                var position = 0f;
                infos.ForEach(i => {
                        i.position = position;
                        i.size = VectorToValue(i.GetBodyPrefab().size);
                        position += i.size + spacing;
                    });
                totalSize = position - spacing;
            }
            
            position = ClampPosition(position);
            Build();
        }

        public void SetList<I>(IEnumerable<I> list) where I : IVirtualizedScrollItem {
            infos.ForEach(i => i.Hide());
            
            if (list == null) {
                infos.Clear();
            } else
                infos.Reuse(list
                    .Select(i => {
                        var info = new ItemInfo();
                        info.source = i;
                        info.list = this;
                        return info;
                    }));

            RefreshDimensions(); 
        }
        
        public IVirtualizedScrollItem GetValue(int index) {
            return infos.Get(index)?.source;
        }

        Vector2 canvasScale = new(1, 1);
        
        RectTransform canvasRect;
        
        static int count = 0;
        
        public override void Initialize() {
            base.Initialize();
            canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            
            DebugPanel.Log("List Initialize", "UI", count++);
        }
        
        void Move(float delta) {
            position += delta;
            
            var clampPosition = ClampPosition(position);
            
            if (clamping)
                position = clampPosition;
            else { 
                if (position != clampPosition) {
                    velocity += SmoothClamping(clampPosition - position) * edgeBouncing * Time.unscaledDeltaTime;
                }
            }

            if (delta != 0) 
                Build();
        }
        
        protected float ClampPosition(float position, float offset = 0) {
            return position.Clamp(-offset, totalSize - GetViewSize() + GetPaddingSize() + offset);
        }
        
        protected virtual float SmoothClamping(float offset) {
            return (-0.5f + 1f / (1f + Mathf.Exp(-offset * 2f / clampOffset))) * clampOffset * 2;
        }
        
        void Build() {
            if (!buildOnActive) 
                return;
            
            var position = this.position;
            var clampPosition = ClampPosition(position);
            if (clampPosition != position) 
                position = clampPosition + SmoothClamping(position - clampPosition);

            var startPadding = GetStartPadding();
            var itemRange = new FloatRange();
            var viewRange = new FloatRange(
                position,
                position + GetViewSize());
            
            foreach (var info in infos) {
                itemRange.Set(
                    info.position + startPadding,
                    info.position + startPadding + info.size);
                if (viewRange.Overlaps(itemRange)) {
                    Align(info.Show().rectTransform, itemRange, position);
                } else 
                    info.Hide();
            }
            
            OnBuild(position);
        }

        protected virtual void OnBuild(float position) {}
        
        public VirtualizedScrollItemBody GetBodyIfVisible(IVirtualizedScrollItem item) {
            return infos.FirstOrDefault(i => i.source == item)?.body;
        }
        public VirtualizedScrollItemBody GetBodyIfVisible<VS>(Func<VS, bool> filter) {
            if (filter == null)
                return null;
            
            return infos.FirstOrDefault(i => i.source is VS vs && filter.Invoke(vs))?.body;
        }
        
        public VirtualizedScrollItemBody GetBodyIfVisibleByIndex(int index) {
            return infos.Get(index)?.body;
        }
        
        void Align(RectTransform rectTransform, FloatRange itemRange, float position) {
            if (rectTransform.parent != transform) {
                rectTransform.SetParent(transform);
                rectTransform.Reset();
            }

            position *= GetDirectionSign();

            switch (orientation) {
                case Direction.TopToBottom: {
                    rectTransform.anchorMin = Vector2.up;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMax = new Vector2( 
                        -padding.right, 
                        position - itemRange.min);
                    rectTransform.offsetMin = new Vector2( 
                        padding.left, 
                        position - itemRange.max);
                    break;
                }
                case Direction.BottomToTop: {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.right;
                    rectTransform.offsetMax = new Vector2( 
                        -padding.right, 
                        position + itemRange.max);
                    rectTransform.offsetMin = new Vector2( 
                        padding.left, 
                        position + itemRange.min);
                    break;
                }
                case Direction.RightToLeft: {
                    rectTransform.anchorMin = Vector2.right;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.offsetMax = new Vector2( 
                        position - itemRange.min,
                        -padding.top);
                    rectTransform.offsetMin = new Vector2( 
                        position - itemRange.max,
                        padding.bottom);
                    break;
                }
                case Direction.LeftToRight: {
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.up;
                    rectTransform.offsetMax = new Vector2( 
                        position + itemRange.max,
                        -padding.top);
                    rectTransform.offsetMin = new Vector2( 
                        position + itemRange.min,
                        padding.bottom);
                    break;
                }
                
            }
        }

        #region Orientation
        
        float GetDelta(PointerEventData eventData) {
            float delta = GetDirectionSign();
            switch (orientation) {
                case Direction.BottomToTop:
                case Direction.TopToBottom: delta *= eventData.delta.y * canvasScale.y; break;
                case Direction.LeftToRight:
                case Direction.RightToLeft: delta *= eventData.delta.x * canvasScale.x; break;
                default: return 0;
            }
            return delta;
        }
        
        float GetDirectionSign() {
            switch (orientation) {
                case Direction.TopToBottom:
                case Direction.RightToLeft: return 1;
                case Direction.BottomToTop:
                case Direction.LeftToRight: return -1;
            }
            return 0f;
        }
        
        float GetStartPadding() {
            switch (orientation) {
                case Direction.TopToBottom: return padding.top;
                case Direction.BottomToTop: return padding.bottom;
                case Direction.LeftToRight: return padding.left;
                case Direction.RightToLeft: return padding.right;
            }
            return 0f;
        }
        
        protected float GetViewSize() {
            switch (orientation) {
                case Direction.BottomToTop:
                case Direction.TopToBottom: return rectTransform.rect.height;
                case Direction.LeftToRight:
                case Direction.RightToLeft: return rectTransform.rect.width;
            }
            return 0f;
        }
        
        float GetPaddingSize() {
            switch (orientation) {
                case Direction.BottomToTop:
                case Direction.TopToBottom: return padding.vertical;
                case Direction.LeftToRight:
                case Direction.RightToLeft: return padding.horizontal;
            }
            return 0f;
        }
        
        float VectorToValue(Vector2 vector) {
            switch (orientation) {
                case Direction.BottomToTop:
                case Direction.TopToBottom: return vector.y;
                case Direction.LeftToRight:
                case Direction.RightToLeft: return vector.x;
            }
            return 0f;
        }
        
        #endregion
        
        public float GetCurrentPosition() => position;
        
        public bool GetCenterPosition<VS>(Func<VS, bool> filter, out float position) {
            position = -1;
            
            if (filter == null)
                return false;
            
            var info = infos.FirstOrDefault(i => i.source is VS vs && filter.Invoke(vs));
            
            if (info == null)
                return false;
            
            position = info.position 
                       + info.size / 2 
                       - GetViewSize() / 2;
            
            position = ClampPosition(position);
            
            return true;
        }
        
        public bool GetCenterPosition(IVirtualizedScrollItem item, out float position) {
            position = -1;
            
            var info = infos.FirstOrDefault(i => i.source == item);
            
            if (info == null)
                return false;
            
            position = info.position 
                       + info.size / 2 
                       - GetViewSize() / 2;
            
            position = ClampPosition(position);

            return true;
        }

        #region Center

        public void CenterTo<SL>(Func<SL, bool> targetFilter, float duration = 0) where SL : IVirtualizedScrollItem {
            if (infos.IsEmpty()) 
                return;
            
            var info = infos
                .FirstOrDefault(i => i.source is SL sl && targetFilter.Invoke(sl));
            
            if (info == null)
                return;
            
            velocity = 0;
            
            var targetPosition = ClampPosition(info.position 
                                                + info.size / 2 
                                                - GetViewSize() / 2); 
            
            CenterTo(targetPosition, duration);
        }
        
        public void CenterTo(IVirtualizedScrollItem target, float duration = 0) {
            CenterTo<IVirtualizedScrollItem>(i => i == target, duration);    
        }
        
        public void CenterTo(float position, float duration = 0) {
            if (duration <= 0) {
                this.position = position;  
                Build();
            } else {
                centering = Centering(position, duration);                    
                centering.Run();
            }
        }
        
        IEnumerator centering = null;
        
        IEnumerator Centering(float targetPosition, float duration) {
            if (duration <= 0)
                yield break;

            targetPosition = ClampPosition(targetPosition);
            
            var start = position;
            
            velocity = 0;
            
            for (var t = 0f; t < 1 && isActiveAndEnabled && !drag; t += Time.unscaledDeltaTime / duration) {
                position = YMath.Lerp(start, targetPosition, t.Ease(EasingFunctions.Easing.InOutCubic));
                position = ClampPosition(position);
                Build();
                yield return null;
            }

            centering = null;
        }

        #endregion

        #region Dragging
        
        bool drag = false;
        float velocity = 0;
        

        public void OnInitializePotentialDrag(PointerEventData eventData) {
            OnPointerDown(eventData);
        }
        
        public void OnPointerDown(PointerEventData eventData) {
            if (options.HasFlag(Options.AllowUserToScroll) && GetViewSize() < totalSize + GetPaddingSize() && InputLock.GetAccess(lockUIKey)) {
                canvasScale = canvasRect.rect.size.Scale(1f / Screen.width, 1f / Screen.height);
                drag = true;
            }
        }
        
        public void OnDrag(PointerEventData eventData) {
            if (drag) 
                Move(GetDelta(eventData));
        }
        
        public void OnEndDrag(PointerEventData eventData) {
            OnPointerUp(eventData);    
        }

        public void OnPointerUp(PointerEventData eventData) {
            if (!drag) return;
            
            drag = false;
            
            velocity = GetDelta(eventData) / Time.unscaledDeltaTime;

            Inertia().Run();
        }
        
        IEnumerator Inertia() {
         
            var bounced = false;
            float bouncedPosition = 0;
            
            bool Allowed() => isActiveAndEnabled && !drag;
            
            while (Allowed()) {
                
                Move(velocity * Time.unscaledDeltaTime);
                
                var clampPosition = ClampPosition(position);
                
                if (velocity.Abs() > maxSpeed) 
                    velocity = maxSpeed * velocity.Sign();
                
                if (clampPosition == position) {
                    if (bounced) {
                        velocity = 0;
                        break;
                    }
                    
                    velocity *= (1f - friction * Time.unscaledDeltaTime).Clamp01();
                    if (velocity.Abs() < 1) {
                        velocity = 0;
                        break;
                    }
                } else {
                    bounced = true;
                    bouncedPosition = clampPosition;
                    var offset = position - clampPosition;
                    var smooth = SmoothClamping(offset);
                    
                    if (offset.Abs() > clampOffset && (offset - smooth).Abs() / clampOffset > .1f)
                        velocity = 0;

                    if (velocity.Sign() != offset.Sign())
                        break;

                    velocity -= smooth * Time.unscaledDeltaTime * edgeBouncing;
                }
                
                yield return null;
            }

            if (!bounced) yield break;
            
            var startPosition = position;
            
            for (var t = 0f; t < 1f && Allowed(); t += Time.unscaledDeltaTime * 2) {
                position = YMath.Lerp(startPosition, bouncedPosition, t.Ease(EasingFunctions.Easing.OutCubic));
                Build();
            
                yield return null;
            }
            
            if (Allowed()) {
                position = bouncedPosition;
                Build();
            }
        }
        
        #endregion

        public class ItemInfo {
            public float position;
            public float size;
            public IVirtualizedScrollItem source;
            public VirtualizedScroll list;
            VirtualizedScrollItemBody prefab;
            public VirtualizedScrollItemBody body;
            
            bool visible = false;

            public void Hide() {
                if (!visible) return; 
                
                body.Kill();
                body = null;
                visible = false;
            }
        
            public VirtualizedScrollItemBody Show() {
                if (!visible) {
                    body = list.prefabProvider.Emit(GetBodyPrefab());
                    source.SetupBody(body);
                    visible = true;
                }
            
                return body;
            }
            
            public VirtualizedScrollItemBody GetBodyPrefab() {
                if (!prefab)
                    prefab = list.prefabProvider.GetPrefab(source.GetBodyPrefabName());
                return prefab;
            }

            public void Refresh() {
                if (visible)
                    source.SetupBody(body);
            }
        }

        public void Refresh() {
            infos?.ForEach(i => i.Refresh());
        }

        public void RefreshByIndex(int index) {
            infos?.Get(index)?.Refresh();
        }
    }

    public interface IPrefabProvider {
        VirtualizedScrollItemBody GetPrefab(string name);
        
        VirtualizedScrollItemBody Emit(VirtualizedScrollItemBody item);
        
        void Remove(VirtualizedScrollItemBody item);
    }
    
    public interface IVirtualizedScrollItem {
        void SetupBody(VirtualizedScrollItemBody body);
        string GetBodyPrefabName();
    }
}
