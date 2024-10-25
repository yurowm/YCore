namespace Yurowm.UI {
    public class LayoutPresetActivator : LayoutPreset {
        public Layout layout;

        public override void OnScreenResize() {
            gameObject.SetActive(GetCurrentLayout().HasFlag(layout));
        }
    }
}