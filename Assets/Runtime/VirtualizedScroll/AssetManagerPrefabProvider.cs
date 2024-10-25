using Yurowm.ContentManager;

namespace Yurowm.UI {
    public class AssetManagerPrefabProvider : IPrefabProvider {
        public VirtualizedScrollItemBody GetPrefab(string name) {
            return AssetManager.GetPrefab<VirtualizedScrollItemBody>(name);
        }

        public VirtualizedScrollItemBody Emit(VirtualizedScrollItemBody item) {
            return AssetManager.Emit(item);
        }

        public void Remove(VirtualizedScrollItemBody item) {
            item?.Kill();
        }
    }
}