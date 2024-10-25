using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Colors {
    [RequireComponent(typeof (IRepaintTargetBase))]
    public class RepaintTargetRepaint : RepaintColor {
        
        IRepaintTargetBase target;

        public override void SetColor(Color color) {
            if (target == null && !this.SetupComponent(out target))
                return;

            switch (target) {
                case IRepaintTarget t: t.Color = TransformColor(color); return;
                case IRepaint32Target t: t.Color = TransformColor(color); return;
            }
        }

        public override Color GetColor() {
            if (target == null && !this.SetupComponent(out target))
                return default;
            
            switch (target) {
                case IRepaintTarget t: return t.Color;
                case IRepaint32Target t: return t.Color;
            }
            
            return default;
        }
    }
    
    public interface IRepaintTargetBase {}
    
    public interface IRepaintTarget : IRepaintTargetBase {
        Color Color {get; set;}
    }
    
    public interface IRepaint32Target : IRepaintTargetBase {
        Color32 Color {get; set;}
    }
    
    
}