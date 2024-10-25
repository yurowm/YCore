using System;
using UnityEngine;
using Yurowm.ContentManager;

namespace Yurowm.UI {
    public class VirtualizedScrollItemBody : ContextedBehaviour, IReserved {
        public Vector2 size = new Vector2(300, 100);

        public virtual void Rollout() {}
        public virtual void Prepare() {}
    }
    
    public class VirtualizeScrollItem: IVirtualizedScrollItem {
        readonly string prefabName;
        Action<VirtualizedScrollItemBody> setup;

        public VirtualizeScrollItem(string prefabName, Action<VirtualizedScrollItemBody> setup = null) {
            this.prefabName = prefabName;
            this.setup = setup;
        }
        
        public void SetupBody(VirtualizedScrollItemBody body) {
            setup?.Invoke(body);
        }

        public string GetBodyPrefabName() => prefabName;
    }
}