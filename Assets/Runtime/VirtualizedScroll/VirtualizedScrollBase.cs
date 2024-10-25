using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Direction = UnityEngine.UI.Slider.Direction;

namespace Yurowm.UI {
    public abstract class VirtualizedScrollBase<C, I> : Behaviour, IDragHandler, IEndDragHandler, IBeginDragHandler, IUIRefresh where C : Component {

        public float friction = .5f;
        public float itemHeight = 50;
        public float spacing = 10;
        
        [Flags]
        public enum Options {
            AllowUserToScroll = 1 << 0,
            BreakPositionOnEnable = 1 << 1
        }
        
        public Direction orientation = Direction.TopToBottom;
        
        public Options options = Options.AllowUserToScroll;
        
        public RectOffset padding;

        float position = 0;

        protected Dictionary<int, C> childs = new Dictionary<int, C>();
        IList infos = null;

        public void ProvideSource(List<I> list) {
            infos = list;
            UpdateChilds(0);
        }
        
        public abstract IList GetList();

        Vector2 canvasScale = new Vector2(1, 1);
        
        RectTransform canvasRect;
        
        public override void Initialize() {
            base.Initialize();
            canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        }

        public abstract void UpdateItem(C item, I info);
        
        public abstract C EmitItem();

        public void Refresh() {
            infos = null;
            UpdateChilds(0);
            foreach (var child in childs)
                UpdateItem(child.Value, (I) infos[child.Key]);
        }

        void OnEnable() {
            if (options.HasFlag(Options.BreakPositionOnEnable)) {
                position = 0;
                velocity = 0;
            }
            Refresh();
        }

        void OnDisable() {
            infos = null;
        }

        #region Center
        
        public void CenterTo(I target) {
            if (!InitializeInfos()) return;
            
            var index = infos.IndexOf(target);
            if (index < 0) return;
            
            var targetPosition = GetStartPadding() 
                                 + index * (itemHeight + spacing) 
                                 + itemHeight / 2
                                 - GetViewSize() / 2;
            
            UpdateChilds(targetPosition - position);
        }
        
        public void CenterTo(I target, float duration) {
            if (centering != null) return;
            
            if (duration <= 0) {
                CenterTo(target);
                return;
            }
            
            if (!InitializeInfos()) return;
            
            var index = infos.IndexOf(target);
            
            if (index < 0) return;
            
            var targetPosition = GetStartPadding() 
                                 + index * (itemHeight + spacing) 
                                 + itemHeight / 2
                                 - GetViewSize() / 2;
            
            if (position == targetPosition) return;

            centering = Centering(targetPosition, duration);                    
            centering.Run();
        }
        
        IEnumerator centering = null;
        
        IEnumerator Centering(float targetPosition, float duration) {
            if (duration <= 0) {
                UpdateChilds(targetPosition - position);
                yield break;
            }
            
            var start = position;
            
            velocity = 0;
            
            for (var t = 0f; t < 1 && isActiveAndEnabled && !drag; t += Time.unscaledDeltaTime / duration) {
                UpdateChilds(YMath.Lerp(start, targetPosition, t.Ease(EasingFunctions.Easing.InOutCubic)) - position);
                yield return null;
            }

            centering = null;
        }
        
        #endregion
        
        C child;
        KeyValuePair<int, C> pair;
        
        public void UpdateChilds(float deltaScroll) {
            if (!InitializeInfos()) return;

            position += deltaScroll;
            var clampP = ClampPosition(position);
            
            if (clampP != position) 
                velocity += (clampP - position) * 10 * Time.unscaledDeltaTime;
            
            var start = position - GetStartPadding();
            var end = start + GetViewSize();

            int startIndex = ((start + spacing) / (itemHeight + spacing))
                .FloorToInt()
                .ClampMin(0);
            
            int endIndex = (end / (itemHeight + spacing))
                .CeilToInt()
                .ClampMax(infos.Count);
            
            foreach (var c in childs)
                if (c.Key < startIndex || c.Key >= endIndex)
                    c.Value.gameObject.SetActive(false);

            for (int index = startIndex; index < endIndex; index++) {
                child = childs.Get(index);
                if (!child) {
                    pair = childs.FirstOrDefault(p => !p.Value.gameObject.activeSelf);
                    if (pair.Value == null) {
                        child = EmitItem();
                        SetupItem(child.transform);
                    } else {
                        child = pair.Value;
                        childs.Remove(pair.Key);
                    }

                    childs.Add(index, child);
                    UpdateItem(child, (I) infos[index]);
                }   

                if (child) {
                    child.gameObject.SetActive(true);
                    SetChildPosition(index);
                }
            }
        }

        bool InitializeInfos() {
            if (infos != null) return true;
            infos = GetList();
            if (infos == null) return false;
            return true;
        }

        void SetChildPosition(int index) {
            
            var rt = child.transform as RectTransform;
            
            if (!rt) return;
            
            var position = this.position * GetDirectionSign();

            switch (orientation) {
                case Direction.TopToBottom: {
                    rt.anchorMin = Vector2.up;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMax = new Vector2(
                        -padding.right,
                        position - padding.top - index * (itemHeight + spacing));
                    rt.offsetMin = new Vector2(
                        padding.left,
                        rt.offsetMax.y - itemHeight);
                } break;
                case Direction.BottomToTop: {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.right;
                    rt.offsetMax = new Vector2(
                        -padding.right,
                        position + padding.bottom + index * (itemHeight + spacing) + itemHeight);
                    rt.offsetMin = new Vector2(
                        padding.left,
                        rt.offsetMax.y - itemHeight);
                } break;
                case Direction.RightToLeft: {
                    rt.anchorMin = Vector2.right;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMax = new Vector2(
                        position - padding.right - index * (itemHeight + spacing),
                        -padding.top);
                    rt.offsetMin = new Vector2(
                        rt.offsetMax.x - itemHeight,
                        padding.bottom);
                } break;
                case Direction.LeftToRight: {
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.up;
                    rt.offsetMax = new Vector2(
                        position + padding.left + index * (itemHeight + spacing) + itemHeight,
                        -padding.top);
                    rt.offsetMin = new Vector2(
                        rt.offsetMax.x - itemHeight,
                        padding.bottom);
                } break;
            }
        }

        float ClampPosition(float position) {
            float positionMax = infos.Count * itemHeight
                                + (infos.Count - 1) * spacing
                                + GetPaddingSize()
                                - GetViewSize();

            positionMax = positionMax.ClampMin(0);
            
            return position.Clamp(0, positionMax);
        }
        
        protected void SetupItem(Transform child) {
            child.SetParent(transform);
            child.Reset();
        }

        bool drag = false;
        float velocity = 0;
        public void OnBeginDrag(PointerEventData eventData) {
            if (options.HasFlag(Options.AllowUserToScroll) && GetViewSize() < GetListSize()) {
                canvasScale = canvasRect.rect.size.Scale(1f / Screen.width, 1f / Screen.height);
                drag = true;
            }
        }
        
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
        
        float GetStartPadding() {
            switch (orientation) {
                case Direction.TopToBottom: return padding.top;
                case Direction.BottomToTop: return padding.bottom;
                case Direction.LeftToRight: return padding.left;
                case Direction.RightToLeft: return padding.right;
            }
            return 0f;
        }
        
        float GetViewSize() {
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
        
        float GetListSize() {
            return GetPaddingSize() + infos.Count * (itemHeight + spacing) - spacing;
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

        public void OnDrag(PointerEventData eventData) {
            if (!drag) return;

            UpdateChilds(GetDelta(eventData));
        }

        public void OnEndDrag(PointerEventData eventData) {
            if (!drag) return;
            
            drag = false;
            if (friction <= 0) return;
            velocity = GetDelta(eventData) / Time.unscaledDeltaTime;
            Inertia().Run();
        }

        IEnumerator Inertia() {
            
            while (isActiveAndEnabled && !drag) {

                UpdateChilds(velocity * Time.unscaledDeltaTime);
                
                if (ClampPosition(position) == position) {
                    velocity *= (1f - friction * Time.unscaledDeltaTime).Clamp01();
                    if (velocity.Abs() < 1) {
                        velocity = 0;
                        break;
                    }
                } 
                
                yield return null;
            }
        }
    }
}
