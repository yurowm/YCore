using UnityEngine;
using UnityEngine.UI;
using Yurowm.ContentManager;

namespace Yurowm.ComposedPages {

    [RequireComponent (typeof (LayoutElement))]
    public class ComposedElement : ContextedBehaviour, IReserved {
        public Page page;
        LayoutElement _layout = null;
        public LayoutElement layout {
            get {
                if (!_layout)
                    _layout = GetComponent<LayoutElement>();
                return _layout;
            }
        }

        public virtual void OnSetup() {

        }

        public virtual bool IsVisible() {
            return true;
        }

        public virtual void Rollout() { }
        public virtual void Prepare() { }
    }
}