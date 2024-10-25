using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.Shapes {
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class ShapeUIBehaviour : MaskableGraphic {
        
        protected MeshUIBuilder builder;
        
        #if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();

            if (!enabled) return;
            
            if (this is IOnAnimateHandler)
                AnimateProperty.Update(this);

            Rebuild();
        }
        #endif

        public void Rebuild() {
            SetDirty();
            SetAllDirty();
        }

        protected override void OnEnable() {
            base.OnEnable();
            SetDirty();
        }

        #region Cache

        public virtual void SetDirty() {
            isDirty = true;    
        }

        bool isDirty = true;
        
        bool rectTransformCached = false;
        Rect rectTransformRect;

        void CacheRectTransform() {
            rectTransformCached = true;
            rectTransformRect = rectTransform.rect;
        }
        
        bool IsRectTransformChanged() {
            return !rectTransformCached || rectTransformRect != rectTransform.rect;
        }
        
        #endregion
        
        
        protected override void OnPopulateMesh(VertexHelper vh) {
            if (isDirty) {
                if (builder == null)
                    builder = new MeshUIBuilder();
                else
                    builder.Clear();
                
                builder.canvas = canvas;
                
                FillMesh(builder);
                
                CacheRectTransform();
                isDirty = false;
            }

            vh.Clear();
            builder.Build(vh);
        }
        
        protected override void OnRectTransformDimensionsChange() {
            if (IsRectTransformChanged())
                SetDirty();
            base.OnRectTransformDimensionsChange();
        }

        public virtual void Clear() {
            builder?.Clear();
            SetDirty();
            SetAllDirty();
        }
        
        public abstract void FillMesh(MeshUIBuilder builder);
    }
}