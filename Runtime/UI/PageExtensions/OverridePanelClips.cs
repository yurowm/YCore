using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.UI {
    public class OverridePanelClips: PageExtension {
        public int panelLinkID;
        
        public string showClip;
        public string hideClip;
        
        public override void OnShow(Page page) {
            base.OnShow(page);
            
            var panel = Page.GetPanel(panelLinkID);
        
            if (!panel) return;
            
            panel.SetVisible(false, true);
            
            if (!showClip.IsNullOrEmpty()) panel.overrideShowClip = showClip;
            if (!hideClip.IsNullOrEmpty()) panel.overrideHideClip = hideClip;
        }

        public override void OnHide(Page page) {
            base.OnHide(page);
            
            var panel = Page.GetPanel(panelLinkID);
        
            if (!panel) return;
            
            if (!hideClip.IsNullOrEmpty() && page.GetMode(panel) != Page.PanelInfo.Mode.Disable) {
                panel.PlayClip(hideClip);
                panel.overrideShowClip = null;
                panel.overrideHideClip = null;
            }
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("panelName", panelLinkID);
            writer.Write("showClip", showClip);
            writer.Write("hideClip", hideClip);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("panelName", ref panelLinkID);
            reader.Read("showClip", ref showClip);
            reader.Read("hideClip", ref hideClip);
        }
    }
}