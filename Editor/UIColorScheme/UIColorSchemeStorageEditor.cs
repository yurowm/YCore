using Yurowm.Dashboard;
using Yurowm.Serialization;

namespace Yurowm.Colors {
    [DashboardGroup("UI")]
    [DashboardTab("UI Colors", null)]
    public class UIColorSchemeStorageEditor : StorageEditor<UIColorScheme> {
        
        public override string GetItemName(UIColorScheme item) {
            return item.ID;
        }

        public override Storage<UIColorScheme> OpenStorage() {
            return UIColorScheme.storage;
        }
    }
}