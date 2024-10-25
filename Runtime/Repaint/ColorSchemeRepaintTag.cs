using UnityEngine;
using Yurowm.Extensions;
using Yurowm.UI;

namespace Yurowm.Colors {
    [RequireComponent(typeof(RepaintColor))]
    [DisallowMultipleComponent]
    public class ColorSchemeRepaintTag : Behaviour, IUIRefresh {
        [HideInInspector]
        public string colorTag;
        
        public bool global = true;

        RepaintColor repaintColor;
        
        public void Refresh() {
            if (global)
                Refresh(UIColorScheme.current);
        }

        public void Refresh(UIColorScheme scheme) {
            if (scheme.GetColor(colorTag, out var color)) {
                if (!repaintColor && !this.SetupComponent(out repaintColor)) {
                    Destroy(this);
                    return;
                }
                repaintColor.SetColor(color);
            }
        }

        void OnEnable() {
            Refresh();
        }
    }
}