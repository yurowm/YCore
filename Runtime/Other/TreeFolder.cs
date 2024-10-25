using System;

namespace Yurowm.HierarchyLists {
    [Serializable]
    public class TreeFolder {
        public string path = "";
        public string name = "";
        public string fullPath {
            get => path.Length > 0 ? path + '/' + name : name;
            set {
                int sep = value.LastIndexOf('/');
                if (sep >= 0) {
                    path = value.Substring(0, sep);
                    name = value.Substring(sep + 1, value.Length - sep - 1);
                } else {
                    path = "";
                    name = value;
                }
            }
        }
        public override int GetHashCode() {
            return fullPath.GetHashCode();
        }
    }
}
