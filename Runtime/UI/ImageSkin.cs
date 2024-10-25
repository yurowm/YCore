using System;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;
using Yurowm.Extensions;
using Yurowm.Shapes;

namespace Yurowm.UI {
    public class ImageSkin: ShapeUIBehaviour {
        public Transform target;
        Transform targetCache;
        
        SpriteSkin skin;
        SpriteRenderer spriteRenderer;
        
        Sprite sprite;

        public override Texture mainTexture => sprite?.texture;

        bool Validate() {
            if (!target) return false;
            
            if (target != targetCache) {
                skin = null;
                spriteRenderer = null;
                targetCache = target;
            }
            
            if (!spriteRenderer && !target.SetupComponent(out spriteRenderer))
                return false;
            
            sprite = spriteRenderer.sprite;
            
            if (!sprite) return false;
                
            if (!skin) 
                target.SetupComponent(out skin);
            
            return true;
        }

        int stateHash = 0;
        
        int GetStateHash() {
            var result = targetCache.localToWorldMatrix.GetHashCode();
            
            result = HashCode.Combine(result, spriteRenderer.color);
            
            if (skin && skin.HasCurrentDeformedVertices())
                foreach (var boneTransform in skin.boneTransforms)
                    result = HashCode.Combine(result, boneTransform.localToWorldMatrix);
            
            return result;
        }
        
        void Update() {
            if (Validate() && stateHash != GetStateHash()) Rebuild();
        }

        public override void FillMesh(MeshUIBuilder builder) {
            if (!Validate()) return;
            
            Vector2[] vertices = sprite.vertices.ToArray();
            
            if (skin && skin.HasCurrentDeformedVertices()) {
                vertices = skin.GetDeformedVertexPositionData().Select(v => v.To2D()).ToArray();
                stateHash = GetStateHash(); 
            } else
                vertices = sprite.vertices.ToArray();
            
            for (var i = 0; i < vertices.Length; i++) {
                vertices[i] = transform.InverseTransformPoint(targetCache.TransformPoint(vertices[i]));
            }
            
            // MeshUtils.TransformVertices(rectTransform, MeshUtils.Scaling.AsIs, RectOffsetFloat.Zero, scale, vertices);
            
            for (var v = 0; v < sprite.vertices.Length; v ++)
                builder.AddVert(vertices[v], color * spriteRenderer.color, sprite.uv[v]);
            
            for (var t = 0; t < sprite.triangles.Length; t += 3)
                builder.AddTriangle(
                    sprite.triangles[t],
                    sprite.triangles[t + 1],
                    sprite.triangles[t + 2]);
        }
    }
}