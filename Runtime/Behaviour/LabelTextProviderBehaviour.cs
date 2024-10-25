using TMPro;
using UnityEngine.UI;
using Yurowm.Extensions;
using Yurowm.UI;

namespace Yurowm {
    public abstract class LabelTextProviderBehaviour : Behaviour, IUIRefresh {
        
        Text label;
        TMP_Text tmpLabel;

        bool isDirty = true;

        protected void SetDirty() => isDirty = true;
        
        public void SetText(string text) {
            if (label || this.SetupComponent(out label))
                label.text = text;
            if (tmpLabel || this.SetupComponent(out tmpLabel))
                tmpLabel.text = text;
            isDirty = false;
        }
        
        protected virtual void OnEnable() {
            Refresh();
        }
        
        public abstract string GetText();

        void Update() {
            if (isDirty) Refresh();
        }

        #region IUIRefresh
        bool IUIRefresh.visible => gameObject.activeInHierarchy;
        
        public void Refresh() {
            SetText(GetText());    
        }
        #endregion
    }
}