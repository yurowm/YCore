using System;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.UI {
    public class ShowPageButton : MonoBehaviour {
        
        public enum Type {
            ByName = 0,
            Previous = 1,
            Default = 2
        }
        
        public Type type;
        [HideInInspector]
        public string pageID;
        
        public bool immediate = false;

        void Awake() {
            if (this.SetupComponent(out Button yB))
                yB.AddAction(Show);
            if (this.SetupComponent(out UnityEngine.UI.Button uB))
                uB.onClick.AddListener(Show);
        }

        public void Show() {
            switch (type) {
                case Type.Default: Page.GetDefault().Show(immediate); break;
                case Type.Previous: Page.Back(immediate); break;
                case Type.ByName: Page.Get(pageID).Show(immediate); break;
            }
        }
    }
}