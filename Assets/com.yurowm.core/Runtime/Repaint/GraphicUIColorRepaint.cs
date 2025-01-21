using UnityEngine;
using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.Colors {
    [RequireComponent(typeof (Graphic))]
    public class GraphicUIColorRepaint : RepaintColor {
        
        Graphic graphic;

        public override void SetColor(Color color) {
            if (!graphic && !this.SetupComponent(out graphic))
                return;
            
            graphic.color = TransformColor(color);
        }

        public override Color GetColor() {
            if (!graphic && !this.SetupComponent(out graphic))
                return default;
            
            return graphic.color;
        }
    }
}