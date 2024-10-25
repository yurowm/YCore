using TMPro;

namespace Yurowm.ComposedPages {
    public class ComposedTitle : ComposedElement {

        public TextMeshProUGUI titleLabel;
        public Button back;

        public bool closeButton {
            get => back != null && back.gameObject.activeSelf;
            set {
                if (back)
                    back.gameObject.SetActive(value);
            }
        }

        public override void OnSetup() {
            base.OnSetup();
            back?.onClick.AddListener(page.Close);
        }

        public void SetTitle(string text) {
            titleLabel.text = text;
        }
        
        #region IReserved

        public override void Rollout() {
            base.Rollout();
            closeButton = true;
            SetTitle("");
        }

        #endregion
    }
}