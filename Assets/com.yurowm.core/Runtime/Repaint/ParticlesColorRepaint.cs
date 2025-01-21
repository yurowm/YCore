using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Colors {
    [RequireComponent(typeof (ParticleSystem))]
    public class ParticlesColorRepaint : RepaintColor {
        
        new ParticleSystem particleSystem;

        public override void SetColor(Color color) {
            if (!particleSystem && !this.SetupComponent(out particleSystem))
                return;
            
            var module = particleSystem.main;
            module.startColor = TransformColor(color);
        }

        public override Color GetColor() {
            if (!particleSystem && !this.SetupComponent(out particleSystem))
                return default;
            
            return particleSystem.main.startColor.color;
        }
    }
}