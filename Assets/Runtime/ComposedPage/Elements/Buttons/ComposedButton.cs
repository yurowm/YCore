using System;
using TMPro;
using Yurowm.Extensions;

namespace Yurowm.ComposedPages {
    public class ComposedButton : ComposedElement {
        public Button button;
        
        TMP_Text label;
        
        public override void Initialize() {
            base.Initialize();
            button.onClick.SetSingleListner(OnClick);
        }

        public Action onClick;
        
        void OnClick() {
            onClick?.Invoke();
        }

        public void SetLabel(string labelText) { 
            if (label || this.SetupChildComponent(out label)) 
                label.text = labelText;
        }

        public void SetInteractable(bool value) { 
            button.interactable = value;
        }

        #region IReserved
        
        public override void Rollout() {
            button.interactable = true;
            SetLabel("");
            SetInteractable(true);
            onClick = null;
        }

        #endregion
    }
}
