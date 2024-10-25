using UnityEngine.UI;
using UnityEngine;

namespace Yurowm.ComposedPages {
    [RequireComponent(typeof(LayoutElement))]
    public class ComposedFlexibleSpace : ComposedElement {

        LayoutElement layout;

        public override void OnSetup() {
            base.OnSetup();
            layout = GetComponent<LayoutElement>();
        }

        public void SetWeight(float weight) {
            layout.flexibleHeight = weight;
        }
    }
}