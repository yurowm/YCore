using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Colors {
    [RequireComponent(typeof (TrailRenderer))]
    public class TrailColorRepaint : RepaintColor {
        
        TrailRenderer trailRenderer;
        
        Color startColor;
        Color endColor;

        public override void SetColor(Color color) {
            if (!trailRenderer && !this.SetupComponent(out trailRenderer))
                return;
            
            if (rememberOriginalColor) {
                if (!remembered) {
                    startColor = trailRenderer.startColor;
                    endColor = trailRenderer.endColor;
                    remembered = true;
                }
            
                trailRenderer.startColor = TransformColor(startColor, color);
                trailRenderer.endColor = TransformColor(endColor, color);
            } else {
                trailRenderer.startColor = TransformColor(trailRenderer.startColor, color);
                trailRenderer.endColor = TransformColor(trailRenderer.endColor, color);
            }
        }

        public override Color GetColor() {
            return default;
        }
    }
}