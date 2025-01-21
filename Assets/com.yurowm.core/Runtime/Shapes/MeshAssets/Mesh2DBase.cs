using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Yurowm.Colors;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Shapes {
    public abstract class Mesh2DBase : Shape2DBehaviour, IRepaintTarget, IMeshDataComponent, IMeshEffectTarget {

        MeshData md;

        public MeshData meshData {
            get {
                md = GetMeshData();
                return md;
            }
            set {
                if (md == value) 
                    return;
                md = value;
                SetMeshData(md);
                SetDirty();
            }
        }

        protected abstract MeshData GetMeshData();
        protected abstract void SetMeshData(MeshData meshData);

        [SerializeField]
        Color m_Color = Color.white;
        
        public Color Color {
            get => m_Color;
            set {
                if (m_Color == value) return;
                m_Color = value;
                SetDirty();
            }
        }

        public RectOffsetFloat borders;
        public float scale = 1f;
        
        public MeshUtils.TransformMode transformMode = 0;
        public MeshUtils.Scaling scalingMode = MeshUtils.Scaling.FitInside;
        public MeshAsset.Order.Options options = 0;
        public MeshBuilderBase.UVGenerator uvGenerator = 0;
        
        Vector2[] vertices = null;

        void OnRectTransformDimensionsChange() {
            SetDirty();
        }

        void OnDidApplyAnimationProperties() {
            SetDirty();
        }

        public override void FillMesh(MeshBuilder builder) {
            if (meshData == null || meshData.vertices.Length < 3) return;
            
            if (vertices == null || vertices.Length != meshData.vertices.Length)
                vertices = meshData
                    .GetTransformVertices(transformMode)
                    .ToArray();
            else {
                int i = 0;
                meshData
                    .GetTransformVertices(transformMode)
                    .ForEach(v => vertices[i++] = v);
            }

            if (transform is RectTransform rectTransform) {
                if (!MeshUtils.TransformVertices(rectTransform, scalingMode, borders, scale, vertices))
                    return;
            } else
                MeshUtils.TransformVertices(scale, vertices);
            
            var order = new MeshAsset.Order(builder) {
                color = m_Color,
                vertices = vertices,
                transformMode = transformMode,
                options = options,
                flip = transformMode.HasFlag(MeshUtils.TransformMode.FlipHorizontal) != 
                       transformMode.HasFlag(MeshUtils.TransformMode.FlipVertical)
            };
            
            foreach (var effect in effects) 
                if (effect.isVisible && effect.Order == MeshEffectOrder.Below)
                    effect.BuildMesh(meshData, order);
            
            meshData.BuildMesh(order);
            
            foreach (var effect in effects) 
                if (effect.isVisible && effect.Order == MeshEffectOrder.Above)
                    effect.BuildMesh(meshData, order);
            
            builder.GenerateUV(uvGenerator);
        }
        
        void OnDrawGizmosSelected() {
            if (scalingMode == MeshUtils.Scaling.Slice && transform is RectTransform rectTransform) {
                Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
                
                Rect rect = rectTransform.rect;

                float s = Mathf.Min(1,
                    rect.width / borders.Horizontal,
                    rect.height / borders.Vertical);

                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector2(rect.xMin + borders.Left * s, rect.yMin)),
                    transform.TransformPoint(new Vector2(rect.xMin + borders.Left * s, rect.yMax)));
                
                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector2(rect.xMax - borders.Right * s, rect.yMin)),
                    transform.TransformPoint(new Vector2(rect.xMax - borders.Right * s, rect.yMax)));
                
                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector2(rect.xMin, rect.yMin + borders.Bottom * s)),
                    transform.TransformPoint(new Vector2(rect.xMax, rect.yMin + borders.Bottom * s)));
                
                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector2(rect.xMin, rect.yMax - borders.Top * s)),
                    transform.TransformPoint(new Vector2(rect.xMax, rect.yMax - borders.Top * s)));
            }
        }

        #region IMeshEffectTarget

        List<IMeshEffect> effects = new List<IMeshEffect>();

        public void AddEffect(IMeshEffect meshEffect) {
            if (meshEffect != null && !effects.Contains(meshEffect))
                effects.Add(meshEffect);
        }

        public void RemoveEffect(IMeshEffect meshEffect) {
            if (meshEffect != null && effects.Contains(meshEffect)) {
                effects.Remove(meshEffect);
                SetDirty();   
            }
        }

        #endregion
    }
}
