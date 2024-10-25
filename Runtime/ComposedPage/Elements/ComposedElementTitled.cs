using TMPro;

namespace Yurowm.ComposedPages {

    public class ComposedElementTitled : ComposedElement {
        
        public TMP_Text title;
        
        public void SetTitle(string title) {
            if (this.title)
                this.title.text = title;
        }

        public override void Rollout() {
            base.Rollout();
            SetTitle("");
        }
    }
}