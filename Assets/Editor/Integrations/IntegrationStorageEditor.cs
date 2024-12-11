using System.Linq;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.Icons;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Integrations {
    [DashboardGroup("Content")]
    [DashboardTab("Integrations", "Integrations")]
    public class IntegrationStorageEditor : StorageEditor<Integration> {
        
        static Texture2D statusIcon;
        
        static readonly Color activeColor = new Color(.5f, 1f, .5f);
        static readonly Color issueColor = new Color(1f, 0.19f, 0.23f);
        static readonly Color inactiveColor = new Color(0.07f, 0.14f, 0.07f);

        int noSDKTag;
        int wrongPlatformTag;
        
        public override bool Initialize() {
            noSDKTag = tags.New("!SDK", issueColor);
            wrongPlatformTag = tags.New("!Platform", issueColor);
            return base.Initialize();
        }

        public override void OnGUI() {
            if (statusIcon == null) statusIcon = EditorIcons.GetIcon("Dot");
            base.OnGUI();
        }
        
        protected override bool FilterNewItem(Integration item, out string reason) {
            if (!base.FilterNewItem(item, out reason))
                return false;
            
            if (storage.items.Any(i => i.GetType() == item.GetType())) {
                reason = "The storage already contains an integration of this type";
                return false;
            }
            
            return true;
        }

        protected override void Sort() {}
        
        bool HasProblem(Integration integration) {
            if (integration.HasIssues())
                return true;
            
            return !PlatformExpression.Evaluate(integration.platformExpression);
        }
        
        protected override Rect DrawItem(Rect rect, Integration item) {
            Color color;
            
            if (!item.active)
                color = inactiveColor;
            else if (HasProblem(item))
                color = issueColor;
            else
                color = activeColor;
            
            rect = ItemIconDrawer.Draw(rect, statusIcon, color);
            
            rect = base.DrawItem(rect, item);
            
            var type = item.GetIntegrationType();
            
            if (!type.IsNullOrEmpty())
                foreach (var t in type.Split(','))
                    rect = ItemIconDrawer.DrawTag(rect, t.Trim(), Color.white, ItemIconDrawer.Side.Right);
            
            return rect;
        }

        protected override void UpdateTags(Integration item) {
            base.UpdateTags(item);
            var issues = item.GetIssues();
            tags.Set(item, noSDKTag, issues.HasFlag(Integration.Issue.SDK));
            tags.Set(item, wrongPlatformTag, issues.HasFlag(Integration.Issue.Platform));
        }

        public override string GetItemName(Integration item) {
            if (!item.name.IsNullOrEmpty())
                return $"{item.GetName()} ({item.name.Italic()})";
            return item.GetName();
        }

        public override Storage<Integration> OpenStorage() {
            return Integration.storage;
        }
    }

}