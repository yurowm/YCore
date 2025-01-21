using Yurowm.Dashboard;
using Yurowm.Serialization;

namespace Yurowm.Core {
    [DashboardGroup("Content")]
    [DashboardTab("User Path", "Footer")]
    public class UserPathStorageEditor : StorageEditor<UserPath> {
        public override string GetItemName(UserPath item) {
            return item.ID;
        }

        public override Storage<UserPath> OpenStorage() {
            return UserPath.storage;
        }
    }
}