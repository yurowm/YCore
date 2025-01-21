using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Shapes;

namespace Yurowm.Effects {
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSpriteApply : MonoBehaviour {
        
        public Sprite sprite;
        public bool textureProperty = true;
        
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        
        [ContextMenu("Apply")]
        void Apply() {
            if (sprite && this.SetupComponent(out ParticleSystemRenderer renderer)) {
                renderer.enabled = true;
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = MeshUtils.GenerateMeshFromSprite(sprite);
                if (textureProperty) {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetTexture(MainTex, sprite.texture);
                    renderer.SetPropertyBlock(block);
                }
            }
        }
        
        void Awake() {
            Apply();
        }
    }
}