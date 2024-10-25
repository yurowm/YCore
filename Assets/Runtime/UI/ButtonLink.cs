using Yurowm.Extensions;

namespace Yurowm {
    public class ButtonLink : UIBehaviour {
        
        public Button button;
        
        public override void Initialize() {
            base.Initialize();
            
            if (this.SetupComponent(out Button button)) 
                button.onClick.SetSingleListner(OnClick);
        }
        
        void OnClick() {
            if (!button) return;
            
            if (!button.interactable) return;
            
            button.onClick?.Invoke();
        }
    }
}