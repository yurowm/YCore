using Yurowm.Extensions;
using Yurowm.Shapes;

namespace UnityEngine.UI {
    [AddComponentMenu("UI/Particle System UI")]
    [RequireComponent (typeof (ParticleSystem))]
    public class UIParticleSystem : ShapeUIBehaviour {
        
        public float scale = 1;
        public bool cropByRect = true;
        
        public bool scaleByVelocity = false;
        public float scaleByVelocityMultiplier = 1f;

        public override Texture mainTexture {
            get {
                if (overrideSprite != null) return overrideSprite.texture;
                
                if (material != null && material.mainTexture != null)
                    return material.mainTexture;

                return s_WhiteTexture;
            }
        }

        public bool clearOnEnable = true;

        ParticleSystem _particleSystem = null;
        new ParticleSystem particleSystem {
            get {
                if (!_particleSystem)
                    _particleSystem = GetComponent<ParticleSystem>();
                return _particleSystem;
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            if (clearOnEnable) particleSystem.Clear();
        }

        void Update() {
            if (particleSystem && particleSystem.IsAlive())
                Rebuild();
        }

        Sprite m_OverrideSprite;
        public Sprite overrideSprite {
            get => m_OverrideSprite ? m_OverrideSprite : sprite;
            set {
                if (m_OverrideSprite == value) return;
                m_OverrideSprite = value;
                Rebuild();
            }
        }


        [SerializeField]
        Sprite m_Sprite;
        public Sprite sprite {
            get => m_Sprite;
            set {
                if (m_Sprite == value) return;
                m_Sprite = value;
                Rebuild();
            }
        }

        static readonly Vector2[] quad = {
            new Vector2(-.5f, .5f),
            new Vector2(-.5f, -.5f),
            new Vector2(.5f, -.5f),
            new Vector2(.5f, .5f) 
        };

        static readonly Vector2[] uv = {
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1)
        };

        static readonly ushort[] triangles = {
            0, 1, 2,
            0, 2, 3
        };

        public override void FillMesh(MeshUIBuilder builder) {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[particleSystem.particleCount];
            particleSystem.GetParticles(particles);
            
            var rect = rectTransform.rect;
            
            bool drawSprite = overrideSprite != null;

            var vertices = drawSprite ? overrideSprite.vertices : quad;
            var uv = drawSprite ? overrideSprite.uv : UIParticleSystem.uv;
            var triangles = drawSprite ? overrideSprite.triangles : UIParticleSystem.triangles;
            
            foreach (var particle in particles) {
                var pColor = particle.GetCurrentColor(particleSystem) * color;
                
                int index = builder.currentVertCount;
                
                var particleSize = particle.GetCurrentSize(particleSystem) * scale;
                var particlePosition = particle.position.To2D() * scale;

                switch (particleSystem.main.simulationSpace) {
                    case ParticleSystemSimulationSpace.Local: break;
                    case ParticleSystemSimulationSpace.World: particlePosition = transform.InverseTransformPoint(particlePosition); break;
                }
                
                if (cropByRect && !rect.Contains(particlePosition)) continue;

                for (int i = 0; i < vertices.Length; i++) {
                    Vector2 vertex;
                    if (scaleByVelocity) {
                        var velocity = particle.velocity.To2D();
                        vertex = vertices[i]
                            .Scale(x: velocity.FastMagnitude() * scaleByVelocityMultiplier)
                            .Rotate(velocity.Angle())
                            * particleSize + particlePosition; 
                    } else
                        vertex = vertices[i].Rotate(particle.rotation) * particleSize + particlePosition;
                    
                    builder.AddVert(vertex, pColor, 
                        uv[i], default,
                        Vector3.back, new Vector4(1, 0, 0, -1));
                }

                for (int i = 0; i < triangles.Length; i += 3) 
                    builder.AddTriangle(
                        index + triangles[i],
                        index + triangles[i + 1],
                        index + triangles[i + 2]);
            }
        }
    }
}
