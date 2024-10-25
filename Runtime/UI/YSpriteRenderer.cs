using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Yurowm.Colors;
using Yurowm.Extensions;
using Yurowm.Shapes;

namespace Yurowm.UI {
    [RequireComponent(typeof(RectTransform))]
    [ExecuteInEditMode]
    public class YSpriteRenderer : Shape2DBehaviour, IRepaintTarget {

        
        [ContextMenu("Clear Property Block")]
        void ClearPropertyBlock() {
            texturePropertyBlock ??= new MaterialPropertyBlock();

            renderer.GetPropertyBlock(texturePropertyBlock);
            texturePropertyBlock.Clear();
            renderer.SetPropertyBlock(texturePropertyBlock);

            SetDirty();
        }

        
        [SerializeField]
        Sprite _sprite;
        public Sprite sprite {
            get => _sprite;
            set {
                if (_sprite == value) return;
                _sprite = value;
                SetDirty();
            }
        }
        
        public Color color = Color.white;
        
        public Color Color {
            get => color;
            set {
                color = value;
                SetDirty();
            }
        }

        public bool preserveAspect;
        
        public MeshUtils.TransformMode transformMode;
        
        static readonly int MainTex = Shader.PropertyToID("_MainTex");

        void OnRectTransformDimensionsChange() {
            SetDirty();
        }
        
        MaterialPropertyBlock texturePropertyBlock;

        public override void FillMesh(MeshBuilder builder) {
            if (transform is RectTransform rectTransform && sprite) {
                texturePropertyBlock ??= new MaterialPropertyBlock();

                renderer.GetPropertyBlock(texturePropertyBlock);
                if (sprite.texture)
                    texturePropertyBlock.SetTexture(MainTex, sprite.texture);
                // else
                //     texturePropertyBlock.SetTexture(MainTex, null);
                renderer.SetPropertyBlock(texturePropertyBlock);
                    
                var masterRect = rectTransform.rect;
                var rect = masterRect;
                if (preserveAspect) {
                    var spriteSize = sprite.bounds.size.To2D();
                    var spriteAspect = spriteSize.x / spriteSize.y;
                    var aspect = rect.width / rect.height;
                
                    if (aspect > spriteAspect)
                        rect.width = rect.height * spriteAspect;
                    else
                        rect.height = rect.width / spriteAspect;
                    
                    rect.position = -rectTransform.pivot * rect.size;
                }

                var triangles = sprite.triangles;
                var uv = sprite.uv;
                var bounds = sprite.bounds;
                
                var vertices = sprite.vertices
                    .Select(v => TransfromVertex(NormalizeVertex(MeshUtils.GetTransformVertex(transformMode, v), bounds), rect))
                    .ToArray();
                
                Vector2[] uv2 = null;
                if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
                    uv2 = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord2).ToArray();
                
                Color32[] vcolor = null;
                if (sprite.HasVertexAttribute(VertexAttribute.Color))
                    vcolor = sprite.GetVertexAttribute<Color32>(VertexAttribute.Color).ToArray();
                
                for (int i = 0; i < vertices.Length; i++) {
                
                    var c = color;
                    if (vcolor != null)
                        c *= vcolor[i];
                
                    builder.AddVert(vertices[i], c, 
                        uv[i], uv2?[i] ?? default, 
                        Vector3.back,
                        new Vector4(1.0f, 0.0f, 0.0f, -1.0f));
                }
                for (int i = 0; i < triangles.Length; i += 3) 
                    builder.AddTriangle(triangles[i], triangles[i + 1], triangles[i + 2]);
            }
        }
        
        Vector2 NormalizeVertex(Vector2 vertex, Bounds bounds) {
            return new Vector2(
                Mathf.InverseLerp(bounds.min.x, bounds.max.x, vertex.x), 
                Mathf.InverseLerp(bounds.min.y, bounds.max.y, vertex.y));
        }
        
        Vector2 TransfromVertex(Vector2 normalizedVertex, Rect rect) {
            return new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedVertex.x), 
                Mathf.Lerp(rect.yMin, rect.yMax,normalizedVertex.y));
        }
    }
}